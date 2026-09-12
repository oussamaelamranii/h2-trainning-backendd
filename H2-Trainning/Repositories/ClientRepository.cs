using H2_Trainning.Data;
using H2_Trainning.Enums;
using H2_Trainning.Interfaces;
using H2_Trainning.Models;
using Microsoft.EntityFrameworkCore;

namespace H2_Trainning.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly ApplicationDbContext _context;

        public ClientRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AppUser>> GetClientsByCoachIdAsync(string coachId)
        {
            return await _context.Users
                .Where(u => u.Role == Role.Client && u.CoachId == coachId && u.IsApproved)
                .OrderBy(u => u.FullName)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<AppUser>> SearchClientsAsync(string coachId, string query)
        {
            return await _context.Users
                .Where(u => u.Role == Role.Client
                         && u.CoachId == coachId
                         && u.IsApproved
                         && u.FullName.Contains(query))
                .OrderBy(u => u.FullName)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<AppUser?> GetByIdAsync(string id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<bool> DeleteAsync(string clientId, string coachId)
        {
            var client = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == clientId && u.CoachId == coachId && u.Role == Role.Client);

            if (client == null) return false;

            // Remove related data manually (DB uses Restrict/NoAction cascade)
            var assignmentIds = await _context.Assignments
                .Where(a => a.ClientId == clientId)
                .Select(a => a.Id)
                .ToListAsync();

            if (assignmentIds.Any())
            {
                var exerciseLogs = _context.ExerciseLogs.Where(el => assignmentIds.Contains(el.AssignmentId));
                _context.ExerciseLogs.RemoveRange(exerciseLogs);

                var assignments = _context.Assignments.Where(a => assignmentIds.Contains(a.Id));
                _context.Assignments.RemoveRange(assignments);
            }

            var reservations = _context.Reservations.Where(r => r.ClientId == clientId);
            _context.Reservations.RemoveRange(reservations);

            var checkIns = _context.CheckIns.Where(c => c.ClientId == clientId);
            _context.CheckIns.RemoveRange(checkIns);

            _context.Users.Remove(client);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<AppUser>> GetPendingClientsAsync()
        {
            return await _context.Users
                .Where(u => u.Role == Role.Client && !u.IsApproved)
                .OrderBy(u => u.FullName)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> DeletePendingAsync(string clientId)
        {
            var client = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == clientId && u.Role == Role.Client && !u.IsApproved);

            if (client == null) return false;

            _context.Users.Remove(client);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
