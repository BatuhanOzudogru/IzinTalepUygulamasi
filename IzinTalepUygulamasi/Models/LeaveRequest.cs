using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IzinTalepUygulamasi.Models
{
    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequestingEmployeeId { get; set; }

        [ForeignKey("RequestingEmployeeId")]
        public User RequestingEmployee { get; set; }

        public LeaveType LeaveType { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public DateTime RequestDate { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public RequestStatus Status { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
