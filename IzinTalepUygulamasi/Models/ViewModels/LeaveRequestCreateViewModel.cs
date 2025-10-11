using System.ComponentModel.DataAnnotations;

namespace IzinTalepUygulamasi.Models.ViewModels
{
    public class LeaveRequestCreateViewModel
    {
        [Required]
        public LeaveType LeaveType { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
