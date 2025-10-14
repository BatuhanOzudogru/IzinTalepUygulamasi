using IzinTalepUygulamasi.Data;
using IzinTalepUygulamasi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using X.PagedList.Extensions;

namespace IzinTalepUygulamasi.Controllers
{
    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManagerController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string search,LeaveType? leaveTypeFilter,DateTime? startDateFilter,int page = 1)
        {
            int pageSize = 10;

            var pendingRequests = _context.LeaveRequests
                .Include(lr => lr.RequestingEmployee)
                .Where(lr => lr.Status == RequestStatus.PENDING)
                .OrderBy(lr => lr.RequestDate);
             

            if (!string.IsNullOrEmpty(search))
            {
                pendingRequests = pendingRequests.Where(r =>
                    r.RequestingEmployee.FullName.Contains(search) ||
                    r.RequestingEmployee.Username.Contains(search)
                ).OrderBy(lr => lr.RequestDate);
            }
            if (leaveTypeFilter.HasValue)
            {
                pendingRequests = pendingRequests.Where(r => r.LeaveType == leaveTypeFilter.Value).OrderBy(lr => lr.RequestDate);
            }
            if (startDateFilter.HasValue)
            {
                pendingRequests = pendingRequests.Where(r => r.StartDate.Date == startDateFilter.Value.Date).OrderBy(lr => lr.RequestDate);
            }

            var pagedRequests =  pendingRequests.ToPagedList(page, pageSize);

            ViewBag.Search = search;
            ViewBag.LeaveTypeFilter = (int?)leaveTypeFilter;
            ViewBag.StartDateFilter = startDateFilter?.ToString("yyyy-MM-dd");

            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            ViewBag.ApprovedThisMonth = await _context.LeaveRequests
                .Where(lr => lr.Status == RequestStatus.APPROVED && lr.UpdatedAt >= firstDayOfMonth && lr.UpdatedAt <= lastDayOfMonth)
                .CountAsync();

            ViewBag.PendingRequestsCount = await _context.LeaveRequests
                .CountAsync(lr => lr.Status == RequestStatus.PENDING);

            var mostUsedLeaveType = await _context.LeaveRequests
                .Where(lr => lr.Status == RequestStatus.APPROVED)
                .GroupBy(lr => lr.LeaveType)
                .Select(g => new { LeaveType = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefaultAsync();

            ViewBag.MostUsedLeaveType = mostUsedLeaveType != null ? mostUsedLeaveType.LeaveType.ToString() : "Veri Yok";

            return View(pagedRequests);
        }
        public IActionResult Report()
        {
            return View();
        }

        public async Task<IActionResult> Details (int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.RequestingEmployee)
                .FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            return View(leaveRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRequest(int id, string decision, string managerComments, byte[] rowVersion)
        {

            var leaveRequest = await _context.LeaveRequests
                                        .Include(lr => lr.RequestingEmployee)
                                        .FirstOrDefaultAsync(lr => lr.Id == id);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            if (rowVersion != null)
            {
                _context.Entry(leaveRequest).Property("RowVersion").OriginalValue = rowVersion;
            }

            if (decision == "Reject" && string.IsNullOrWhiteSpace(managerComments))
            {
                ModelState.AddModelError(string.Empty, "Talebi reddederken açıklama girmek zorunludur.");
                return View("Details", leaveRequest);
            }



            var managerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    leaveRequest.Status = (decision == "Approve") ? RequestStatus.APPROVED : RequestStatus.REJECTED;
                    leaveRequest.UpdatedAt = DateTime.Now;
                    leaveRequest.UpdatedBy = User.Identity.Name;

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

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    var entry = ex.Entries.Single();
                    var databaseValues = await entry.GetDatabaseValuesAsync();

                    
                    var databaseLeaveRequest = (LeaveRequest)databaseValues.ToObject();
                    ModelState.AddModelError(string.Empty, "Bu kayıt siz düzenlemeye başladıktan sonra başka bir yönetici tarafından güncellenmiş.");
                    return View("Details", leaveRequest);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError(string.Empty, "İşlem sırasında beklenmedik bir hata oluştu: " + ex.Message);
                    return View("Details", leaveRequest);
                }
            }
        }

        public async Task<IActionResult> MonthlyReport(int? year, int? month)
        {
            int currentYear = year ?? DateTime.Now.Year;
            int currentMonth = month ?? DateTime.Now.Month;

            var startDate = new DateTime(currentYear, currentMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var approvedLeaves = await _context.LeaveRequests
                .Include(lr => lr.RequestingEmployee)
                .Where(lr =>
                    lr.Status == RequestStatus.APPROVED &&
                    lr.StartDate >= startDate && lr.EndDate <= endDate)
                .ToListAsync();

            var reportData = approvedLeaves
                .GroupBy(lr => lr.RequestingEmployee)
                .Select(g => new
                {
                    Employee = g.Key,
                    TotalDays = g.Sum(l => (l.EndDate - l.StartDate).TotalDays + 1)
                })
                .OrderBy(x => x.Employee.FullName)
                .ToList();

            ViewBag.Year = currentYear;
            ViewBag.Month = currentMonth;
            ViewBag.Years = Enumerable.Range(DateTime.Now.Year - 5, 10).ToList();
            ViewBag.Months = Enumerable.Range(1, 12).ToList();

            return View(reportData);
        }

        public async Task<IActionResult> DownloadMonthlyReportCsv(int? year, int? month)
        {
            int currentYear = year ?? DateTime.Now.Year;
            int currentMonth = month ?? DateTime.Now.Month;
            var startDate = new DateTime(currentYear, currentMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var approvedLeaves = await _context.LeaveRequests
                .Include(lr => lr.RequestingEmployee)
                .Where(lr =>
                lr.Status == RequestStatus.APPROVED && 
                lr.StartDate >= startDate && lr.EndDate <= endDate)
                .ToListAsync();
            var reportData = approvedLeaves
                .GroupBy(lr => lr.RequestingEmployee)
                .Select(g => new
                {
                    Employee = g.Key,
                    TotalDays = g.Sum(l => (l.EndDate - l.StartDate).TotalDays + 1)
                })
                .OrderBy(x => x.Employee.FullName)
                .ToList();


            var builder = new StringBuilder();
            builder.AppendLine("Calisan Adi,Toplam Izin Gunu");

            foreach (var item in reportData)
            {
                builder.AppendLine($"\"{item.Employee.FullName}\",{item.TotalDays}");
            }

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"AylikIzinRaporu_{currentYear}_{currentMonth}.csv");
        }
    }
}
