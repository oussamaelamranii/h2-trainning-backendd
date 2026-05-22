using H2_Trainning.Dtos;
using H2_Trainning.Enums;
using H2_Trainning.Interfaces;
using H2_Trainning.Models;

namespace H2_Trainning.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly IAssignmentRepository _repo;

        public AssignmentService(IAssignmentRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<AssignmentDto>> GetByCoachIdAsync(string coachId)
        {
            var assignments = await _repo.GetByCoachIdAsync(coachId);
            return assignments.Select(MapToDto).ToList();
        }

        public async Task<List<AssignmentDto>> GetByClientIdAsync(string clientId)
        {
            var assignments = await _repo.GetByClientIdAsync(clientId);
            return assignments.Select(MapToDto).ToList();
        }

        public async Task<AssignmentDto> CreateAsync(string coachId, CreateAssignmentDto dto)
        {
            var assignment = new Assignment
            {
                ClientId = dto.ClientId,
                ProgramId = dto.ProgramId,
                AssignedDate = DateTime.UtcNow,
                Status = AssignmentStatus.Assigned
            };

            var created = await _repo.CreateAsync(assignment);

            // Re-fetch with includes
            var full = await _repo.GetByIdAsync(created.Id);
            return MapToDto(full!);
        }

        public async Task<AssignmentDto?> UpdateStatusAsync(int id, UpdateAssignmentStatusDto dto)
        {
            var assignment = await _repo.GetByIdAsync(id);
            if (assignment == null) return null;

            if (!Enum.TryParse<AssignmentStatus>(dto.Status, true, out var status))
                throw new Exception("Invalid status. Must be 'Assigned', 'Viewed', or 'Completed'.");

            assignment.Status = status;
            await _repo.UpdateAsync(assignment);
            return MapToDto(assignment);
        }

        public async Task<bool> UpdateExerciseLogAsync(int assignmentId, int exerciseId, UpdateExerciseLogDto dto)
        {
            var log = await _repo.GetExerciseLogAsync(assignmentId, exerciseId);
            if (log == null)
            {
                log = new ExerciseLog
                {
                    AssignmentId = assignmentId,
                    ExerciseId = exerciseId,
                    IsCompleted = dto.IsCompleted,
                    ClientNotes = dto.ClientNotes,
                    Weight = dto.Weight
                };
                await _repo.AddExerciseLogAsync(log);
            }
            else
            {
                log.IsCompleted = dto.IsCompleted;
                log.ClientNotes = dto.ClientNotes;
                log.Weight = dto.Weight;
                await _repo.UpdateExerciseLogAsync(log);
            }

            // Track weight history if a weight is provided
            if (dto.Weight.HasValue)
            {
                var assignment = await _repo.GetByIdAsync(assignmentId);
                if (assignment != null)
                {
                    await _repo.AddOrUpdateWeightHistoryAsync(assignment.ClientId, exerciseId, dto.Weight.Value, dto.ClientNotes, DateTime.UtcNow);
                }
            }

            return true;
        }

        public async Task<object> GetExerciseWeightHistoryAsync(string clientId, int exerciseId)
        {
            var history = await _repo.GetExerciseWeightHistoryAsync(clientId, exerciseId);
            return history.Select(h => new
            {
                Date = h.LoggedAt,
                Weight = h.Weight,
                Notes = h.Notes
            }).ToList();
        }

        private static AssignmentDto MapToDto(Assignment a) => new()
        {
            Id = a.Id,
            ClientId = a.ClientId,
            ClientName = a.Client?.FullName ?? "",
            ProgramId = a.ProgramId,
            ProgramTitle = a.Program?.Title ?? "",
            AssignedDate = a.AssignedDate,
            Status = a.Status.ToString(),
            LastChecked = a.LastChecked
        };
    }
}
