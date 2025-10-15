using IzinTalepUygulamasi.Data;
using IzinTalepUygulamasi.Models;
using IzinTalepUygulamasi.Models.ViewModels;
using IzinTalepUygulamasi.Services.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IzinTalepUygulamasi.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly ILogger<LeaveRequestController> _logger;
        private readonly ILeaveRequestService _leaveRequestService;

        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); 
        private string CurrentUserName => User.Identity?.Name ?? "Bilinmeyen";
        public LeaveRequestController(ILogger<LeaveRequestController> logger,ILeaveRequestService leaveRequestService)
        {
            _logger = logger;
            _leaveRequestService = leaveRequestService;
        }
        public async Task<IActionResult> Index(string statusFilter) 
        {
            var leaveRequests = await _leaveRequestService.GetUserLeaveRequestsAsync(CurrentUserId, statusFilter);

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

            if (!ModelState.IsValid)
            {
                var validationErrors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogWarning("Geçersiz izin talebi modeli. Kullanıcı: {User}, Hatalar: {Errors}", CurrentUserName, validationErrors);
                return View(model);
            }

            try
            {
                var errorMessage = await _leaveRequestService.CreateLeaveRequestAsync(model, CurrentUserId, CurrentUserName);

                if (!string.IsNullOrEmpty(errorMessage))
                {

                    ModelState.AddModelError("", errorMessage);

                    return View(model);
                }

                _logger.LogInformation("Yeni izin talebi başarıyla oluşturuldu. Oluşturan: {User}, İzin Türü: {LeaveType}, Başlangıç: {StartDate}, Bitiş: {EndDate}",
                    CurrentUserName,
                    model.LeaveType,
                    model.StartDate,
                    model.EndDate);

                return RedirectToAction("Index", "LeaveRequest");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İzin talebi oluşturulurken beklenmedik bir hata oluştu. Kullanıcı: {User}", CurrentUserName);
                ModelState.AddModelError("", "İzin talebiniz oluşturulurken beklenmedik bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
                return View(model);
            }
        }

        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _leaveRequestService.FindLeaveRequestByIdAsync(id.Value);

            if (request==null)
            {
                return NotFound();
            }

            if(request.RequestingEmployeeId != CurrentUserId)
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var errorMessage = await _leaveRequestService.UpdateLeaveRequestAsync(id, model, CurrentUserId, CurrentUserName);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ModelState.AddModelError("", errorMessage);
                    return View(model);
                }

                _logger.LogInformation("İzin talebi (ID: {LeaveRequestId}) başarıyla güncellendi. Güncelleyen: {User}", id, CurrentUserName);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İzin talebi (ID: {LeaveRequestId}) güncellenirken beklenmedik bir hata oluştu. Kullanıcı: {User}", id, CurrentUserName);
                ModelState.AddModelError("", "İşleminiz sırasında beklenmedik bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
                return View(model);
            }
        }


        [Authorize(Roles = "Employee")]
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var leaveRequest = await _leaveRequestService.FindLeaveRequestByIdAsync(id.Value);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            if (leaveRequest.RequestingEmployeeId != CurrentUserId)
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var errorMessage = await _leaveRequestService.DeleteLeaveRequestAsync(id, CurrentUserId, CurrentUserName);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return new JsonResult(new { success = false, message = errorMessage }) { StatusCode = 403 };
                }

                return Ok(new { success = true, message = "Talep başarıyla silindi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteConfirmed action'ında beklenmedik bir hata oluştu. Talep ID: {Id}", id);

                return new JsonResult(new { success = false, message = "İşlem sırasında sunucuda bir hata oluştu." }) { StatusCode = 500 };
            }
        }
    }
}
