using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mPass_server.Database.Models;

/// <summary>
/// Password for a website/service
/// </summary>
public class ServicePassword
{
    public int Id { get; set; }

    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [MaxLength(256)] public required string Title { get; set; }
    public List<string> Websites { get; set; } = [];
    [MaxLength(256)] public string? Login { get; set; }
    [MaxLength(256)] public string? Password { get; set; }
    [MaxLength(256)] public string? Note { get; set; }
    [MaxLength(512)] public required string Salt { get; set; }
    [MaxLength(1024)] public required string Nonce { get; set; }

    public int UserId { get; set; }

    [ForeignKey("UserId")] public User? User { get; set; }
}