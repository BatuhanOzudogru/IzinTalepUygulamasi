using IzinTalepUygulamasi.Models;
using IzinTalepUygulamasi.Models.ViewModels;
using System.Security.Claims;

namespace IzinTalepUygulamasi.Services.Abstract
{
    public interface ILeaveRequestService
    {
        Task<LeaveRequest?> FindLeaveRequestByIdAsync(int id);
        Task<IEnumerable<LeaveRequest>> GetUserLeaveRequestsAsync(int userId, string statusFilter);
        Task<string?> CreateLeaveRequestAsync(LeaveRequestCreateViewModel model, int userId, string userName);
        Task<string?> UpdateLeaveRequestAsync(int id, LeaveRequestCreateViewModel model, int userId, string userName);
        Task<string?> DeleteLeaveRequestAsync(int leaveRequestId, int userId, string userName);
    }
}
