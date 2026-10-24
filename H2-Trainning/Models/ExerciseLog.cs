using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace H2_Trainning.Models
{
    public class ExerciseLog
    {
        public int Id { get; set; }
        
        [Required]
        public int AssignmentId { get; set; }
        [JsonIgnore]
        public Assignment Assignment { get; set; }
        
        [Required]
        public int ExerciseId { get; set; }
        [JsonIgnore]
        public Exercise Exercise { get; set; }
        
        public bool IsCompleted { get; set; }
        
        [MaxLength(1000)]
        public string? ClientNotes { get; set; }

        public double? Weight { get; set; }

        public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    }
}
