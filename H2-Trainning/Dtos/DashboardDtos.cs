namespace H2_Trainning.Dtos
{
    public class CoachDashboardDto
    {
        public int TotalClients { get; set; }
        public int ActiveAssignments { get; set; }
        public int ViewedAssignments { get; set; }
        public List<AssignmentDto> RecentAssignments { get; set; } = new();
    }

    public class ClientDashboardDto
    {
        public ProgramDto? CurrentProgram { get; set; }
        public string? AssignmentStatus { get; set; }
        public int? AssignmentId { get; set; }
        public DateTime? LastChecked { get; set; }
    }
}
