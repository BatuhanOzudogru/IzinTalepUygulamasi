using IzinTalepUygulamasi.Data;
using IzinTalepUygulamasi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Index()
        {
            var pendingRequests = await _context.LeaveRequests
                .Include(lr => lr.RequestingEmployee)
                .Where(lr => lr.Status == RequestStatus.PENDING)
                .OrderBy(lr => lr.RequestDate)
                .ToListAsync();

            return View(pendingRequests);
        }
        public IActionResult Report()
        {
            return View();
        }
    }
}
