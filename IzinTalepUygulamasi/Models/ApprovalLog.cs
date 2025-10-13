using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IzinTalepUygulamasi.Models
{
    public class ApprovalLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LeaveRequestId { get; set; }

        [ForeignKey("LeaveRequestId")]
        public LeaveRequest LeaveRequest { get; set; }

        [Required]
        public int ProcessedByManagerId { get; set; }

        [ForeignKey("ProcessedByManagerId")]
        public User ProcessedByManager { get; set; }

        [Required]
        public RequestStatus NewStatus { get; set; }

        [MaxLength(500)]
        public string? Comments { get; set; }

        [Required]
        public DateTime ProcessingDate { get; set; }
    }
}
