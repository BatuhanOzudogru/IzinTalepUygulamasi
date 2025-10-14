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
        private readonly ILogger<LeaveRequestController> _logger;
        public LeaveRequestController(ApplicationDbContext context, ILogger<LeaveRequestController> logger)
        {
            _context = context;
            _logger = logger;
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
            var currentUserName = User.Identity?.Name ?? "Bilinmeyen Kullanıcı";
            if (ModelState.IsValid)
            {

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var requestingEmployeeId = int.Parse(userId);

                var employeesRequests = await _context.LeaveRequests.Where(lr => lr.RequestingEmployeeId == requestingEmployeeId &&
                (lr.Status == RequestStatus.PENDING || lr.Status == RequestStatus.APPROVED)).ToListAsync();


                var overlap = employeesRequests.Any(r =>
                model.StartDate <= r.EndDate && model.EndDate >= r.StartDate);

                if (overlap)
                {
                    ModelState.AddModelError("", "Bu tarihler arasında bekleyen veya onaylanmış izniniz bulunmakta. Tarihleri kontrol ediniz.");
                    var error = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    _logger.LogWarning("Geçersiz izin talebi oluşturma denemesi. Kullanıcı: {User}, Hatalar: {Errors}",
                        currentUserName,
                        error);
                    return View(model);
                }


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
                _logger.LogInformation("Yeni izin talebi başarıyla oluşturuldu. Oluşturan: {User}, İzin Türü: {LeaveType}, Başlangıç: {StartDate}, Bitiş: {EndDate}",
                    currentUserName,
                    model.LeaveType,
                    model.StartDate,
                    model.EndDate);

                return RedirectToAction("Index", "LeaveRequest");
            }
            var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            _logger.LogWarning("Geçersiz izin talebi oluşturma denemesi. Kullanıcı: {User}, Hatalar: {Errors}",
                currentUserName,
                errors);

            return View(model);
        }

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _context.LeaveRequests.FindAsync(id);

            if (request==null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var requestingEmployeeId = int.Parse(userId);

            if(request.RequestingEmployeeId != requestingEmployeeId)
            {
                return Forbid();
            }

            if (request.Status != RequestStatus.PENDING)
            {
                TempData["ErrorMessage"] = "Sadece 'Beklemede' olan talepler düzenlenebilir.";
                return RedirectToAction("Index");
            }
            var model = new LeaveRequestCreateViewModel
            {
                LeaveType = request.LeaveType,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Reason = request.Reason
            };

            return View(model);
        }

        [Authorize(Roles = "Employee")]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, LeaveRequestCreateViewModel model)
        {
            var leaveRequestToUpdate = await _context.LeaveRequests.FindAsync(id);

            if (leaveRequestToUpdate == null)
            {
                return NotFound();
            }
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (leaveRequestToUpdate.RequestingEmployeeId != currentUserId)
            {
                return Forbid();
            }

            if (leaveRequestToUpdate.Status != RequestStatus.PENDING)
            {
                ModelState.AddModelError("", "Bu talep artık düzenlenemez.");
                return View(model);
            }

            if (ModelState.IsValid)
            {

                if (model.EndDate < model.StartDate)
                {
                    ModelState.AddModelError("EndDate", "Bitiş tarihi başlangıç tarihinden önce olamaz. Lütfen tarihleri kontrol edin.");
                    return View(model);
                }
                DateTime earliestAllowedDate = DateTime.Now.Date.AddDays(-7);

                if (model.StartDate.Date < earliestAllowedDate)
                {
                    ModelState.AddModelError("StartDate", "Başlangıç tarihi bugünden en fazla 7 gün öncesi olabilir.");
                    return View(model);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var requestingEmployeeId = int.Parse(userId);

                var employeesRequests = await _context.LeaveRequests.Where(lr => lr.Id != id &&  lr.RequestingEmployeeId == requestingEmployeeId &&
                (lr.Status == RequestStatus.PENDING || lr.Status == RequestStatus.APPROVED)).ToListAsync();


                var overlap = employeesRequests.Any(r =>
                model.StartDate <= r.EndDate && model.EndDate >= r.StartDate);

                if (overlap)
                {
                    ModelState.AddModelError("", "Bu tarihler arasında bekleyen veya onaylanmış başka bir izniniz bulunmakta.");
                    return View(model);
                }

                leaveRequestToUpdate.LeaveType = model.LeaveType;
                leaveRequestToUpdate.StartDate = model.StartDate;
                leaveRequestToUpdate.EndDate = model.EndDate;
                leaveRequestToUpdate.Reason = model.Reason;
                leaveRequestToUpdate.UpdatedAt = DateTime.Now;
                leaveRequestToUpdate.UpdatedBy = User.Identity.Name;

                try
                {
                    _context.Update(leaveRequestToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Değişiklikler kaydedilemedi. Lütfen tekrar deneyin.");
                    return View(model);
                }
                return RedirectToAction("Index");
            }
            return View(model);
        }


        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var leaveRequest = await _context.LeaveRequests.FirstOrDefaultAsync(m => m.Id == id);

            if (leaveRequest == null) 
            {
                return NotFound();
            }
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (leaveRequest.RequestingEmployeeId != currentUserId)
            {
                return Forbid();
            }

            if (leaveRequest.Status != RequestStatus.PENDING)
            {
                TempData["ErrorMessage"] = "Sadece 'Beklemede' olan talepler silinebilir.";
                return RedirectToAction("Index");
            }

            return View(leaveRequest);
        }


        [HttpDelete]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserName = User.Identity?.Name ?? "Bilinmeyen";
            var leaveRequest = await _context.LeaveRequests.FindAsync(id);
            if (leaveRequest==null)
            {
                return NotFound(new { success = false, message = "Kayıt bulunamadı." });
            }
            
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (leaveRequest.RequestingEmployeeId != currentUserId || leaveRequest.Status != RequestStatus.PENDING)
            {
                _logger.LogWarning("{User} tarafından ID'si {Id} olan talebe yetkisiz veya geçersiz silme denemesi yapıldı (AJAX).", currentUserName, id);
                return new JsonResult(new { success = false, message = "Bu işlem yapılamaz." }) { StatusCode = 403 };
            }

            
            try
            {
                _context.LeaveRequests.Remove(leaveRequest);
                await _context.SaveChangesAsync();
                _logger.LogInformation("İzin talebi başarıyla silindi. Silen: {User}, Talep ID: {Id}", currentUserName, id);
                return Ok(new { success = true, message = "Talep başarıyla silindi." });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Talep silinirken beklenmedik bir hata oluştu. Talep ID: {Id}", id);
                return new JsonResult(new { success = false, message = "Silme işlemi sırasında sunucuda bir hata oluştu." }) { StatusCode = 500 };
            }
            

            

            
        }
    }
}
