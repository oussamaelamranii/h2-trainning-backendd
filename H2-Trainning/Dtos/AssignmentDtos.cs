using System.ComponentModel.DataAnnotations;

namespace H2_Trainning.Dtos
{
    public class AssignmentDto
    {
        public int Id { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public int ProgramId { get; set; }
        public string ProgramTitle { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastChecked { get; set; }
    }

    public class CreateAssignmentDto
    {
        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public int ProgramId { get; set; }
    }

    public class UpdateAssignmentStatusDto
    {
        [Required]
        public string Status { get; set; } = string.Empty; // "Assigned", "Viewed", "Completed"
    }
}
