using IzinTalepUygulamasi.Models;
using IzinTalepUygulamasi.Models.ViewModels;
using X.PagedList;

namespace IzinTalepUygulamasi.Services.Abstract
{
    public interface IManagerService
    {
        Task<IPagedList<LeaveRequestViewModel>> GetFilteredPendingRequestsAsync(string? search, LeaveType? leaveTypeFilter, DateTime? startDateFilter, int page, int pageSize);
        Task<int> GetApprovedThisMonthCountAsync();
        Task<int> GetTotalPendingRequestsCountAsync();
        Task<string> GetMostUsedLeaveTypeAsync();
        Task<LeaveRequestViewModel?> GetLeaveRequestWithDetailsAsync(int id);
        Task<string?> ProcessLeaveRequestAsync(int id, string decision, string managerComments, int managerId, string managerName, byte[] rowVersion);
        Task<List<MonthlyReportItem>> GetMonthlyReportDataAsync(int year, int month);
    }
}
