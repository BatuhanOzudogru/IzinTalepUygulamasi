using IzinTalepUygulamasi.Data;
using IzinTalepUygulamasi.Models;
using IzinTalepUygulamasi.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IzinTalepUygulamasi.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        public LeaveRequestController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string statusFilter) 
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var userLeaveRequestsQuery = _context.LeaveRequests
                                                 .Where(lr => lr.RequestingEmployeeId == userId);

            if (!string.IsNullOrEmpty(statusFilter))
            {
                RequestStatus status = (RequestStatus)Enum.Parse(typeof(RequestStatus), statusFilter);
                userLeaveRequestsQuery = userLeaveRequestsQuery.Where(lr => lr.Status == status);
            }

            var leaveRequests = await userLeaveRequestsQuery.OrderBy(lr => lr.RequestDate).ToListAsync();

            ViewBag.StatusFilter = statusFilter;

            return View(leaveRequests);
        }

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public IActionResult Create()
        {
            var model = new LeaveRequestCreateViewModel();
            return View(model);
        }

        [Authorize(Roles = "Employee")]
        [HttpPost]
        public async Task<IActionResult> Create(LeaveRequestCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var requestingEmployeeId = int.Parse(userId);

                var leaveRequest = new LeaveRequest
                {
                    RequestingEmployeeId = requestingEmployeeId,
                    LeaveType = model.LeaveType,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    Reason = model.Reason,
                    RequestDate = DateTime.Now,
                    Status = RequestStatus.PENDING,
                    CreatedAt = DateTime.Now,
                    CreatedBy = User.Identity.Name
                };

                await _context.LeaveRequests.AddAsync(leaveRequest);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }
    }
}
