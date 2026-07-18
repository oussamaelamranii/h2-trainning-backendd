using H2_Trainning.Dtos;

namespace H2_Trainning.Interfaces
{
    public interface IProgramService
    {
        Task<List<ProgramDto>> GetByCoachIdAsync(string coachId);
        Task<List<ProgramSummaryDto>> GetSummariesByCoachIdAsync(string coachId);
        Task<ProgramDto?> GetByIdAsync(int id);
        Task<ProgramDto> CreateAsync(string coachId, CreateProgramDto dto);
        Task<ProgramDto?> UpdateAsync(int id, string coachId, UpdateProgramDto dto);
        Task<bool> DeleteAsync(int id, string coachId);
        Task<ProgramDto?> DuplicateAsync(int id, string coachId);
    }
}
