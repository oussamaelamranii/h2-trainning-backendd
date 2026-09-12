using H2_Trainning.Data;
using H2_Trainning.Interfaces;
using H2_Trainning.Models;
using Microsoft.EntityFrameworkCore;

namespace H2_Trainning.Repositories
{
    public class AvailabilityRepository : IAvailabilityRepository
    {
        private readonly ApplicationDbContext _context;

        public AvailabilityRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<AvailabilitySlot>> GetByCoachIdAsync(string coachId)
        {
            return await _context.AvailabilitySlots
                .Include(s => s.Reservation)
                .Where(s => s.CoachId == coachId)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<AvailabilitySlot>> GetAllAvailableAsync()
        {
            return await _context.AvailabilitySlots
                .Include(s => s.Coach)
                .Where(s => !s.IsBooked)
                .OrderBy(s => s.Date)
                .ThenBy(s => s.StartTime)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<AvailabilitySlot?> GetByIdAsync(int id)
        {
            return await _context.AvailabilitySlots
                .Include(s => s.Reservation)
                .Include(s => s.Coach)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<AvailabilitySlot> CreateAsync(AvailabilitySlot slot)
        {
            _context.AvailabilitySlots.Add(slot);
            await _context.SaveChangesAsync();
            return slot;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var slot = await _context.AvailabilitySlots
                .Include(s => s.Reservation)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (slot == null) return false;

            // Also remove linked reservation if exists
            if (slot.Reservation != null)
            {
                _context.Reservations.Remove(slot.Reservation);
            }

            _context.AvailabilitySlots.Remove(slot);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
