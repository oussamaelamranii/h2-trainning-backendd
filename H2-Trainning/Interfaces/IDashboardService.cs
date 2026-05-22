using H2_Trainning.Dtos;

namespace H2_Trainning.Interfaces
{
    public interface IDashboardService
    {
        Task<CoachDashboardDto> GetCoachDashboardAsync(string coachId);
        Task<ClientDashboardDto> GetClientDashboardAsync(string clientId);
        Task UpdateLastCheckedAsync(string clientId);
    }
}
