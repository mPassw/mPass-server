using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using mPass_server.Database;
using mPass_server.Middleware;
using mPass_server.Services;
using mPass_server.Utils;
using Scalar.AspNetCore;
using StackExchange.Redis;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// server side encryption
builder.Services.AddSingleton<EncryptionService>();

// swagger (scalar)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "mPass Server API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Auth",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
    };
    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    options.OperationFilter<AuthFilter>();

    var xmlFileName = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
});

// smtp
builder.Services.AddSingleton(new SmtpClient
{
    Host = builder.Configuration["Smtp:Host"] ?? throw new InvalidOperationException("SMTP Host is not set"),
    Port = int.Parse(builder.Configuration["Smtp:Port"] ?? throw new InvalidOperationException("SMTP Port is not set")),
    EnableSsl = bool.Parse(builder.Configuration["Smtp:EnableSsl"] ??
                           throw new InvalidOperationException("SMTP EnableSsl is not set")),

    Credentials = new NetworkCredential
    {
        UserName = builder.Configuration["Smtp:Username"] ??
                   throw new InvalidOperationException("SMTP Username is not set"),
        Password = builder.Configuration["Smtp:Password"] ??
                   throw new InvalidOperationException("SMTP Password is not set")
    }
});
builder.Services.AddSingleton<MailService>();

// redis
var redisConnectionString =
    $"{builder.Configuration["Redis:Host"]}:" +
    $"{builder.Configuration["Redis:Port"]}," +
    $"password={builder.Configuration["Redis:Password"]}";
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));

// postgresql
var connectionString = builder.Configuration["Postgres:ConnectionString"];
builder.Services.AddDbContext<DatabaseContext>(options => options.UseNpgsql(connectionString));

// jwt auth
builder.Services.AddSingleton<JwtService>();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            builder.Configuration["Auth:SecurityKey"] ??
            throw new InvalidOperationException("Auth Secret key is not set"))),
        ValidIssuer = builder.Configuration["Auth:Issuer"] ?? "mPass",
        ClockSkew = TimeSpan.Zero
    };
});

// cors
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders(["x-mpass-instance"]));
});

// rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("default", context =>
    {
        var jwtToken = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
        if (!string.IsNullOrEmpty(jwtToken))
        {
            return RateLimitPartition.GetTokenBucketLimiter(jwtToken, _ =>
                new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 8,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 8,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    TokensPerPeriod = 8,
                    AutoReplenishment = true
                });
        }

        return RateLimitPartition.GetTokenBucketLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ =>
                new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 4,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 4,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                    TokensPerPeriod = 4,
                    AutoReplenishment = true
                });
    });
});

var app = builder.Build();

// migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DatabaseContext>();
        context.Database.Migrate();
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});


if (builder.Configuration.GetValue<bool>("Server:UseSwagger"))
{
    app.UseSwagger(options => { options.RouteTemplate = "/openapi/{documentName}.json"; });
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("mPass Server API")
            .WithEndpointPrefix("/api-reference/{documentName}")
            .WithDownloadButton(false)
            .WithModels(false)
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(ScalarTarget.Node, ScalarClient.Fetch);
    });
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseMiddleware<ValidateSession>();

app.MapControllers()
    .RequireCors()
    .RequireRateLimiting("default");

app.Run();