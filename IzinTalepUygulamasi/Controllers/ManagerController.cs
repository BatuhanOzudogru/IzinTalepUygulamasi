using IzinTalepUygulamasi.Data;
using IzinTalepUygulamasi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
    }
}
