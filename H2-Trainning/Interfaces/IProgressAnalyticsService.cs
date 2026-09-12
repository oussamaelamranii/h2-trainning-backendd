using H2_Trainning.Dtos;

namespace H2_Trainning.Interfaces
{
    public interface IProgressAnalyticsService
    {
        Task<ClientProgressAnalyticsDto?> GetClientProgressAsync(string coachId, string clientId, int weeks, int? assignmentId = null);
        Task<List<ClientProgramHistoryItemDto>?> GetClientProgramHistoryAsync(string coachId, string clientId);
    }
}
