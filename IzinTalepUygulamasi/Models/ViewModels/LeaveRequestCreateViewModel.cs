using System.ComponentModel.DataAnnotations;

namespace IzinTalepUygulamasi.Models.ViewModels
{
    public class LeaveRequestCreateViewModel
    {
        public LeaveType LeaveType { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; } = DateTime.Now;

        public string? Reason { get; set; }
    }
}
