namespace IzinTalepUygulamasi.Models.ViewModels
{
    public class LeaveRequestViewModel
    {
        public int Id { get; set; }
        public string EmployeeFullName { get; set; }
        public string EmployeeUserName { get; set; }
        public DateTime RequestDate { get; set; }
        public LeaveType LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public int NumberOfDays { get; set; }
        public RequestStatus Status { get; set; }
        public byte[] RowVersion { get; set; }
    }
}
