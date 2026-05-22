using H2_Trainning.Dtos;

namespace H2_Trainning.Interfaces
{
    public interface IClientService
    {
        Task<List<UserDto>> GetClientsByCoachIdAsync(string coachId);
        Task<List<UserDto>> SearchClientsAsync(string coachId, string query);
        Task<UserDto> CreateClientAsync(string coachId, CreateClientDto dto);
        Task<UserDto?> GetByIdAsync(string id);
        Task<bool> DeleteClientAsync(string clientId, string coachId);
        Task<List<UserDto>> GetPendingClientsAsync();
        Task<UserDto> ApproveClientAsync(string clientId, string coachId);
        Task<bool> RejectClientAsync(string clientId);
        Task<UserDto> ResetClientPasswordAsync(string clientId, string coachId, string newPassword);
    }
}
