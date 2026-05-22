using H2_Trainning.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json.Serialization;

namespace H2_Trainning.Models
{
    public class AppUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; }
        [JsonIgnore]
        public ICollection<Post> Posts { get; set; } = new List<Post>();

        [JsonIgnore]
        public ICollection<WeightHistoryLog> WeightHistoryLogs { get; set; } = new List<WeightHistoryLog>();
        [Required]
        public Role Role { get; set; }       // enum: Coach, Client
        [MaxLength(200)]
        public string? Goal { get; set; }    // clients only
        public bool IsApproved { get; set; } = true; // false for pending client signups
        [MaxLength(200)]
        public string? PlainPassword { get; set; } // stored so coach can view client credentials
        public string? CoachId { get; set; } // FK → coach that owns this client
        public AppUser? Coach { get; set; }

        public ICollection<AppUser> Clients { get; set; }
        public ICollection<Assignment> Assignments { get; set; }
        public ICollection<AvailabilitySlot> AvailabilitySlots { get; set; }
        public ICollection<Reservation> ClientReservations { get; set; }
        public ICollection<Reservation> CoachReservations { get; set; }
    }
}
