using IzinTalepUygulamasi.Data;
using IzinTalepUygulamasi.Models;
using IzinTalepUygulamasi.Models.ViewModels;
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

    public async Task<IPagedList<LeaveRequestViewModel>> GetFilteredPendingRequestsAsync(string? search, LeaveType? leaveTypeFilter, DateTime? startDateFilter, int page, int pageSize)
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

        var pendingRequestFromDb =  pendingRequests.ToPagedList(page, pageSize);
        var viewModels = pendingRequestFromDb.Select(lr => new LeaveRequestViewModel
        {
            Id = lr.Id,
            EmployeeFullName = lr.RequestingEmployee.FullName,
            EmployeeUserName = lr.RequestingEmployee.Username,
            RequestDate = lr.RequestDate,
            LeaveType = lr.LeaveType,
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            RowVersion = lr.RowVersion,
            Status = lr.Status,
            Reason = lr.Reason,
            NumberOfDays = CalculateBusinessDays(lr.StartDate, lr.EndDate)
        });
        var pagedViewModel = new StaticPagedList<LeaveRequestViewModel>(viewModels, pendingRequestFromDb);
        return pagedViewModel;
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

    public async Task<LeaveRequestViewModel?> GetLeaveRequestWithDetailsAsync(int id)
    {
        var leaveRequestFromDb = await _context.LeaveRequests
        .Include(lr => lr.RequestingEmployee)
        .FirstOrDefaultAsync(lr => lr.Id == id);
        if (leaveRequestFromDb == null)
        {
            return null;
        }

        var viewModel = new LeaveRequestViewModel
        {
            Id = leaveRequestFromDb.Id,
            EmployeeFullName = leaveRequestFromDb.RequestingEmployee.FullName,
            EmployeeUserName = leaveRequestFromDb.RequestingEmployee.Username,
            RequestDate = leaveRequestFromDb.RequestDate,
            LeaveType = leaveRequestFromDb.LeaveType,
            StartDate = leaveRequestFromDb.StartDate,
            EndDate = leaveRequestFromDb.EndDate,
            RowVersion = leaveRequestFromDb.RowVersion,
            Status = leaveRequestFromDb.Status,
            Reason = leaveRequestFromDb.Reason,
            NumberOfDays = CalculateBusinessDays(leaveRequestFromDb.StartDate, leaveRequestFromDb.EndDate)
        };
        return viewModel;
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
    public static int CalculateBusinessDays(DateTime startDate, DateTime endDate)
    {
        int businessDays = 0;
        for (var currentDate = startDate.Date; currentDate <= endDate.Date; currentDate = currentDate.AddDays(1))
        {
            if (currentDate.DayOfWeek != DayOfWeek.Saturday && currentDate.DayOfWeek != DayOfWeek.Sunday)
            {
                businessDays++;
            }
        }
        return businessDays;
    }
    public async Task<List<MonthlyReportItem>> GetMonthlyReportDataAsync(int year, int month)
    {
        var reportStartDate = new DateTime(year, month, 1);
        var reportEndDate = reportStartDate.AddMonths(1).AddDays(-1);

        var approvedLeaves = await _context.LeaveRequests
            .Include(lr => lr.RequestingEmployee)
            .Where(lr => lr.Status == RequestStatus.APPROVED && lr.StartDate <= reportEndDate && lr.EndDate >= reportStartDate)
            .ToListAsync();

        return approvedLeaves
            .GroupBy(lr => lr.RequestingEmployee)
            .Select(g => new MonthlyReportItem
            {
                Employee = g.Key,
                TotalDays = g.Sum(l =>
                {
                    var effectiveStartDate = l.StartDate > reportStartDate ? l.StartDate : reportStartDate;
                    var effectiveEndDate = l.EndDate < reportEndDate ? l.EndDate : reportEndDate;
                    return CalculateBusinessDays(effectiveStartDate, effectiveEndDate);
                })
            })
        .OrderBy(x => x.Employee.FullName)
        .ToList();
    }
}