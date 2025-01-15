using System.ComponentModel.DataAnnotations;

namespace mPass_server.Database.Models;

public class User
{
    public int Id { get; set; }

    public DateTime? LastLogin { get; set; }
    public required DateTime CreatedAt { get; set; }

    public bool Admin { get; set; }

    [MaxLength(128)] public string? Username { get; set; }
    [MaxLength(128)] public required string Email { get; set; }
    public bool EmailVerified { get; set; }

    [MaxLength(2048)] public required string Salt { get; set; }
    [MaxLength(2048)] public required string Verifier { get; set; }

    public List<ServicePassword>? Passwords { get; set; }
}