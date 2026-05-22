using H2_Trainning.Enums;
using System.ComponentModel.DataAnnotations;

namespace H2_Trainning.Models
{
    public class Assignment
    {
        public int Id { get; set; }
        [Required]
        public string ClientId { get; set; }    // FK → AppUser
        public AppUser Client { get; set; }
        [Required]
        public int ProgramId { get; set; }      // FK → Program
        public Program Program { get; set; }
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
        [Required]
        public AssignmentStatus Status { get; set; } = AssignmentStatus.Assigned;
        public DateTime? LastChecked { get; set; }

        public ICollection<ExerciseLog> ExerciseLogs { get; set; } = new List<ExerciseLog>();
    }
    
}