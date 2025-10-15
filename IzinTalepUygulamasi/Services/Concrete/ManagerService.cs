using IzinTalepUygulamasi.Data;
using IzinTalepUygulamasi.Models;
using IzinTalepUygulamasi.Services.Abstract;
using Microsoft.EntityFrameworkCore;
using System.Text;
using X.PagedList;
using X.PagedList.Extensions;

public class ManagerService : IManagerService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ManagerService> _logger;

    public ManagerService(ApplicationDbContext context, ILogger<ManagerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IPagedList<LeaveRequest>> GetFilteredPendingRequestsAsync(string? search, LeaveType? leaveTypeFilter, DateTime? startDateFilter, int page, int pageSize)
    {
        var pendingRequests = _context.LeaveRequests
            .Include(lr => lr.RequestingEmployee)
            .Where(lr => lr.Status == RequestStatus.PENDING)
            .OrderBy(lr => lr.RequestDate)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            pendingRequests = pendingRequests.Where(r =>
                r.RequestingEmployee.FullName.Contains(search) ||
                r.RequestingEmployee.Username.Contains(search));
        }
        if (leaveTypeFilter.HasValue)
        {
            pendingRequests = pendingRequests.Where(r => r.LeaveType == leaveTypeFilter.Value);
        }
        if (startDateFilter.HasValue)
        {
            pendingRequests = pendingRequests.Where(r => r.StartDate.Date == startDateFilter.Value.Date);
        }

        return pendingRequests.ToPagedList(page, pageSize);
    }

    public async Task<int> GetApprovedThisMonthCountAsync()
    {
        var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
        return await _context.LeaveRequests
            .CountAsync(lr => lr.Status == RequestStatus.APPROVED && lr.UpdatedAt >= firstDayOfMonth && lr.UpdatedAt <= lastDayOfMonth);
    }

    public async Task<int> GetTotalPendingRequestsCountAsync()
    {
        return await _context.LeaveRequests.CountAsync(lr => lr.Status == RequestStatus.PENDING);
    }

    public async Task<string> GetMostUsedLeaveTypeAsync()
    {
        var mostUsedLeaveType = await _context.LeaveRequests
            .Where(lr => lr.Status == RequestStatus.APPROVED)
            .GroupBy(lr => lr.LeaveType)
            .Select(g => new { LeaveType = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .FirstOrDefaultAsync();
        return mostUsedLeaveType != null ? mostUsedLeaveType.LeaveType.ToString() : "Veri Yok";
    }

    public async Task<LeaveRequest?> GetLeaveRequestWithDetailsAsync(int id)
    {
        return await _context.LeaveRequests
            .Include(lr => lr.RequestingEmployee)
            .FirstOrDefaultAsync(lr => lr.Id == id);
    }

    public async Task<string?> ProcessLeaveRequestAsync(int id, string decision, string managerComments, int managerId, string managerName, byte[] rowVersion)
    {
        var leaveRequest = await _context.LeaveRequests
                                    .Include(lr => lr.RequestingEmployee)
                                    .FirstOrDefaultAsync(lr => lr.Id == id);

        if (leaveRequest == null)
        {
            return "İşlem yapılmak istenen talep bulunamadı.";
        }

        if (rowVersion != null)
        {
            _context.Entry(leaveRequest).Property("RowVersion").OriginalValue = rowVersion;
        }

        if (decision == "Reject" && string.IsNullOrWhiteSpace(managerComments))
        {
            return "Talebi reddederken açıklama girmek zorunludur.";
        }

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                leaveRequest.Status = (decision == "Approve") ? RequestStatus.APPROVED : RequestStatus.REJECTED;
                leaveRequest.UpdatedAt = DateTime.Now;
                leaveRequest.UpdatedBy = managerName;

                var approvalLog = new ApprovalLog
                {
                    LeaveRequestId = leaveRequest.Id,
                    ProcessedByManagerId = managerId,
                    NewStatus = leaveRequest.Status,
                    Comments = managerComments,
                    ProcessingDate = DateTime.Now
                };

                await _context.ApprovalLogs.AddAsync(approvalLog);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Talep ID {Id} başarıyla işlendi. Yeni Durum: {NewStatus}", id, leaveRequest.Status);
                return null;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Concurrency hatası! Yönetici {ManagerName}, talep {Id}'yi işlerken başka bir işlem yapıldı.", managerName, id);
                return "Bu kayıt siz düzenlemeye başladıktan sonra başka bir yönetici tarafından güncellenmiş.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Talep işlenirken BEKLENMEDİK BİR HATA oluştu! Talep ID: {Id}, Yönetici: {ManagerName}", id, managerName);
                return "İşlem sırasında beklenmedik bir hata oluştu: " + ex.Message;
            }
        }
    }

    public async Task<List<MonthlyReportItem>> GetMonthlyReportDataAsync(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var approvedLeaves = await _context.LeaveRequests
            .Include(lr => lr.RequestingEmployee)
            .Where(lr => lr.Status == RequestStatus.APPROVED && lr.StartDate <= endDate && lr.EndDate >= startDate)
            .ToListAsync();

        return approvedLeaves
            .GroupBy(lr => lr.RequestingEmployee)
            .Select(g => new MonthlyReportItem
            {
                Employee = g.Key,
                TotalDays = g.Sum(l => (l.EndDate - l.StartDate).TotalDays + 1)
            })
            .OrderBy(x => x.Employee.FullName)
            .ToList();
    }
}