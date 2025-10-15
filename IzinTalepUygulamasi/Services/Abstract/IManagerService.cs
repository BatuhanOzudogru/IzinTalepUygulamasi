using IzinTalepUygulamasi.Models;
using X.PagedList;

namespace IzinTalepUygulamasi.Services.Abstract
{
    public interface IManagerService
    {
        Task<IPagedList<LeaveRequest>> GetFilteredPendingRequestsAsync(string? search, LeaveType? leaveTypeFilter, DateTime? startDateFilter, int page, int pageSize);
        Task<int> GetApprovedThisMonthCountAsync();
        Task<int> GetTotalPendingRequestsCountAsync();
        Task<string> GetMostUsedLeaveTypeAsync();
        Task<LeaveRequest?> GetLeaveRequestWithDetailsAsync(int id);
        Task<string?> ProcessLeaveRequestAsync(int id, string decision, string managerComments, int managerId, string managerName, byte[] rowVersion);
        Task<List<MonthlyReportItem>> GetMonthlyReportDataAsync(int year, int month);
    }
}
