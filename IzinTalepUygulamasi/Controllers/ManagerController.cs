using IzinTalepUygulamasi.Data;
using IzinTalepUygulamasi.Models;
using IzinTalepUygulamasi.Services.Abstract;
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
        private readonly IManagerService _managerService;
        private readonly ILogger<ManagerController> _logger;

        public ManagerController(IManagerService managerService, ILogger<ManagerController> logger)
        {
            _managerService = managerService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? search, LeaveType? leaveTypeFilter, DateTime? startDateFilter, int page = 1)
        {
            int pageSize = 10;

            var pagedRequests = await _managerService.GetFilteredPendingRequestsAsync(search, leaveTypeFilter, startDateFilter, page, pageSize);
            ViewBag.ApprovedThisMonth = await _managerService.GetApprovedThisMonthCountAsync();
            ViewBag.PendingRequestsCount = await _managerService.GetTotalPendingRequestsCountAsync();
            ViewBag.MostUsedLeaveType = await _managerService.GetMostUsedLeaveTypeAsync();

            ViewBag.Search = search;
            ViewBag.LeaveTypeFilter = (int?)leaveTypeFilter;
            ViewBag.StartDateFilter = startDateFilter?.ToString("yyyy-MM-dd");

            return View(pagedRequests);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Details action'ı ID olmadan çağrıldı.");
                return NotFound();
            }

            var leaveRequest = await _managerService.GetLeaveRequestWithDetailsAsync(id.Value);

            if (leaveRequest == null)
            {
                _logger.LogWarning("Yönetici {ManagerName}, bulunamayan bir talep (ID: {Id}) detaylarına erişmeye çalıştı.", User.Identity?.Name, id);
                return NotFound();
            }

            _logger.LogInformation("Yönetici {ManagerName}, {EmployeeName} adlı çalışana ait {Id} ID'li talebin detaylarını inceliyor.",
                User.Identity?.Name, leaveRequest.RequestingEmployee.FullName, id);

            return View(leaveRequest);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRequest(int id, string decision, string managerComments, byte[] rowVersion)
        {
            var managerName = User.Identity?.Name ?? "Bilinmeyen";
            var managerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var errorMessage = await _managerService.ProcessLeaveRequestAsync(id, decision, managerComments, managerId, managerName, rowVersion);

            if (errorMessage != null)
            {
                ModelState.AddModelError(string.Empty, errorMessage);

                var leaveRequestForView = await _managerService.GetLeaveRequestWithDetailsAsync(id);
                return View("Details", leaveRequestForView);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> MonthlyReport(int? year, int? month)
        {
            int currentYear = year ?? DateTime.Now.Year;
            int currentMonth = month ?? DateTime.Now.Month;
            var reportData = await _managerService.GetMonthlyReportDataAsync(currentYear, currentMonth);
            ViewBag.Year = currentYear;
            ViewBag.Month = currentMonth;
            ViewBag.Years = Enumerable.Range(DateTime.Now.Year - 5, 10).ToList();
            ViewBag.Months = Enumerable.Range(1, 12).ToList();

            _logger.LogInformation("Yönetici {ManagerName}, {Year}-{Month} için aylık raporu görüntüledi.", User.Identity?.Name, currentYear, currentMonth);

            return View(reportData);
        }

        public async Task<IActionResult> DownloadMonthlyReportCsv(int? year, int? month)
        {
            int currentYear = year ?? DateTime.Now.Year;
            int currentMonth = month ?? DateTime.Now.Month;

            var reportData = await _managerService.GetMonthlyReportDataAsync(currentYear, currentMonth);
            _logger.LogInformation("Yönetici {ManagerName}, {Year}-{Month} için aylık raporu CSV olarak indirdi.", User.Identity?.Name, currentYear, currentMonth);

            var builder = new StringBuilder();
            builder.AppendLine("Calisan Adi;Toplam Izin Gunu");
            foreach (var item in reportData)
            {
                builder.AppendLine($"\"{item.Employee.FullName}\";{item.TotalDays}");
            }

            var csvBytes = Encoding.UTF8.GetBytes(builder.ToString());
            var bom = Encoding.UTF8.GetPreamble();
            var csvWithBom = bom.Concat(csvBytes).ToArray();

            return File(csvWithBom, "text/csv", $"AylikIzinRaporu_{currentYear}_{currentMonth}.csv");
        }
    }
}
