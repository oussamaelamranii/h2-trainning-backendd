using System.ComponentModel.DataAnnotations;

namespace H2_Trainning.Models;

public class CoachMessage
{
    public int Id { get; set; }
    public string CoachId { get; set; } = string.Empty;
    public AppUser Coach { get; set; } = null!;
    public string ClientId { get; set; } = string.Empty;
    public AppUser Client { get; set; } = null!;
    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}

public class PostRead
{
    public string ClientId { get; set; } = string.Empty;
    public AppUser Client { get; set; } = null!;
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}

public class BodyWeightEntry
{
    public int Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public AppUser Client { get; set; } = null!;
    public decimal WeightKg { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
