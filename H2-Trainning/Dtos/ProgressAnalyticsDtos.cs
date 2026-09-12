namespace H2_Trainning.Dtos
{
    public class ClientProgressAnalyticsDto
    {
        public ClientAnalyticsProfileDto Client { get; set; } = new();
        public int? AssignmentId { get; set; }
        public int? ProgramId { get; set; }
        public int Weeks { get; set; }
        public DateTime RangeStart { get; set; }
        public DateTime RangeEnd { get; set; }
        public DateTime? ProgramAssignedDate { get; set; }
        public DateTime? ProgramPeriodEnd { get; set; }
        public int ProgramDurationDays { get; set; }
        public bool IsHistoricalProgram { get; set; }
        public string? ProgramTitle { get; set; }
        public bool HasActiveProgram { get; set; }
        public ProgressSummaryDto Summary { get; set; } = new();
        public List<WeeklyProgressDto> WeeklyProgress { get; set; } = new();
        public List<SessionProgressDto> Sessions { get; set; } = new();
        public CheckInProgressDto CheckIns { get; set; } = new();
    }

    public class ClientProgramHistoryItemDto
    {
        public int AssignmentId { get; set; }
        public int ProgramId { get; set; }
        public string ProgramTitle { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int DurationDays { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? LastChecked { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class ClientAnalyticsProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Goal { get; set; }
    }

    public class ProgressSummaryDto
    {
        public int TotalExercises { get; set; }
        public int TrackedExercises { get; set; }
        public int ImprovingExercises { get; set; }
        public int StableExercises { get; set; }
        public int DecliningExercises { get; set; }
        public int InsufficientDataExercises { get; set; }
        public int LoggedDays { get; set; }
        public int TotalExerciseLogs { get; set; }
        public double? OverallChangePercent { get; set; }
        public string OverallTrend { get; set; } = "Needs data";
        public double DataCoveragePercent { get; set; }
    }

    public class WeeklyProgressDto
    {
        public int WeekNumber { get; set; }
        public string Label { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int LoggedExercises { get; set; }
        public int ActiveSessions { get; set; }
        public double? AverageProgressIndex { get; set; }
        public int CheckInCount { get; set; }
        public double? AverageWeight { get; set; }
        public double? AverageEnergy { get; set; }
        public double? AverageSleep { get; set; }
        public double? AverageMood { get; set; }
    }

    public class SessionProgressDto
    {
        public int DayNumber { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public int TotalExercises { get; set; }
        public int TrackedExercises { get; set; }
        public int ImprovingExercises { get; set; }
        public int StableExercises { get; set; }
        public int DecliningExercises { get; set; }
        public int InsufficientDataExercises { get; set; }
        public double? ProgressIndex { get; set; }
        public string Trend { get; set; } = "Needs data";
        public List<ExerciseProgressDto> Exercises { get; set; } = new();
    }

    public class ExerciseProgressDto
    {
        public int ExerciseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Sets { get; set; }
        public string Reps { get; set; } = string.Empty;
        public double? FirstWeight { get; set; }
        public double? LatestWeight { get; set; }
        public double? Change { get; set; }
        public double? ChangePercent { get; set; }
        public string Trend { get; set; } = "Needs data";
        public List<WeeklyExerciseProgressDto> WeeklyProgress { get; set; } = new();
    }

    public class WeeklyExerciseProgressDto
    {
        public int WeekNumber { get; set; }
        public double? Weight { get; set; }
    }

    public class CheckInProgressDto
    {
        public int Count { get; set; }
        public double? FirstWeight { get; set; }
        public double? LatestWeight { get; set; }
        public double? WeightChange { get; set; }
        public double? AverageSleep { get; set; }
        public double? AverageEnergy { get; set; }
        public double? AverageMood { get; set; }
    }
}
