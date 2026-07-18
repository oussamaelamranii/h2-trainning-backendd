using H2_Trainning.Models;

namespace H2_Trainning.Interfaces
{
    public interface ICheckInRepository
    {
        Task<List<CheckIn>> GetByClientIdAsync(string clientId);
        Task<CheckIn?> GetLatestByClientIdAsync(string clientId);
        Task<CheckIn> CreateAsync(CheckIn checkIn);
        Task<Dictionary<string, decimal?>> GetLatestWeightsForClientsAsync(List<string> clientIds);
    }
}
