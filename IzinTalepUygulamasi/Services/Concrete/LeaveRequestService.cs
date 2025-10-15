using IzinTalepUygulamasi.Data;
using IzinTalepUygulamasi.Models;
using IzinTalepUygulamasi.Models.ViewModels;
using IzinTalepUygulamasi.Services.Abstract;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IzinTalepUygulamasi.Services.Concrete
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LeaveRequestService> _logger;

        public LeaveRequestService(ApplicationDbContext context, ILogger<LeaveRequestService> logger)
        {
            _context = context;
            _logger = logger;
        }

       

        public async Task<IEnumerable<LeaveRequest>> GetUserLeaveRequestsAsync(int userId, string statusFilter)
        {
            var userLeaveRequests =  _context.LeaveRequests
                                                 .Where(lr => lr.RequestingEmployeeId == userId);

            if (!string.IsNullOrEmpty(statusFilter))
            {
                RequestStatus status = (RequestStatus)Enum.Parse(typeof(RequestStatus), statusFilter);
                userLeaveRequests = userLeaveRequests.Where(lr => lr.Status == status);
            }

            var leaveRequests = await userLeaveRequests.OrderBy(lr => lr.RequestDate).ToListAsync();

            return leaveRequests;
        }
        public async Task<string?> CreateLeaveRequestAsync(LeaveRequestCreateViewModel model, int userId, string userName)
        {
            var leaveRequests = await _context.LeaveRequests.Where(lr => lr.RequestingEmployeeId == userId &&
                (lr.Status == RequestStatus.PENDING || lr.Status == RequestStatus.APPROVED)).ToListAsync();

            var overlap = leaveRequests.Any(r => model.StartDate <= r.EndDate && model.EndDate >= r.StartDate);
            var error = "";
            if (overlap)
            {
                error = "Bu tarihler arasında bekleyen veya onaylanmış izniniz bulunmakta. Tarihleri kontrol ediniz.";
               
                _logger.LogWarning("Geçersiz izin talebi oluşturma denemesi. Kullanıcı: {User}, Hatalar: {Errors}",userName,error);
                return error;
            }
            var leaveRequest = new LeaveRequest
            {
                RequestingEmployeeId = userId,
                LeaveType = model.LeaveType,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Reason = model.Reason,
                RequestDate = DateTime.Now,
                Status = RequestStatus.PENDING,
                CreatedAt = DateTime.Now,
                CreatedBy = userName
            };

            await _context.LeaveRequests.AddAsync(leaveRequest);
            await _context.SaveChangesAsync();

            return null;
        }

        public async Task<string?> DeleteLeaveRequestAsync(int leaveRequestId, int userId, string userName)
        {
            var leaveRequestToDelete = await _context.LeaveRequests.FindAsync(leaveRequestId);

            if (leaveRequestToDelete == null)
            {
                return "Silinmek istenen talep bulunamadı.";
            }

            if (leaveRequestToDelete.RequestingEmployeeId != userId)
            {
                _logger.LogWarning("Yetkisiz silme denemesi. Talep ID: {LeaveRequestId}, Talep Sahibi ID: {OwnerId}, Deneyen Kullanıcı: {User}",
                    leaveRequestId, leaveRequestToDelete.RequestingEmployeeId, userName);

                return "Bu talebi silme yetkiniz bulunmamaktadır.";
            }

            if (leaveRequestToDelete.Status != RequestStatus.PENDING)
            {
                _logger.LogWarning("Statüsü değiştirilemez olan bir talep ({Status}) silinmeye çalışıldı. Talep ID: {LeaveRequestId}, Kullanıcı: {User}",
                    leaveRequestToDelete.Status, leaveRequestId, userName);

                return $"Bu talep '{leaveRequestToDelete.Status}' statüsünde olduğu için artık silinemez.";
            }

            try
            {
                _context.LeaveRequests.Remove(leaveRequestToDelete);
                await _context.SaveChangesAsync();

                _logger.LogInformation("İzin talebi başarıyla silindi. Silen: {User}, Talep ID: {Id}", userName, leaveRequestId);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Talep silinirken beklenmedik bir hata oluştu. Talep ID: {Id}", leaveRequestId);
                return "Silme işlemi sırasında sunucuda beklenmedik bir hata oluştu.";
            }
        }

        public async Task<string?> UpdateLeaveRequestAsync(int id, LeaveRequestCreateViewModel model, int userId, string userName)
        {
            var leaveRequestToUpdate = await _context.LeaveRequests.FindAsync(id);

            if (leaveRequestToUpdate == null)
            {
                _logger.LogWarning("Var olmayan bir izin talebi (ID: {id}) güncellenmeye çalışıldı. Kullanıcı: {User}", id, userName);
                return "Güncellenmek istenen talep bulunamadı.";
            }
            if (leaveRequestToUpdate.RequestingEmployeeId != userId)
            {
                _logger.LogWarning("Yetkisiz güncelleme denemesi. Talep ID: {Id}, Talep Sahibi ID: {OwnerId}, Deneyen Kullanıcı: {User}",
                    id, leaveRequestToUpdate.RequestingEmployeeId, userName);
                return "Bu talebi düzenleme yetkiniz bulunmamaktadır.";
            }

            if (leaveRequestToUpdate.Status != RequestStatus.PENDING)
            {
                _logger.LogWarning("Statüsü değiştirilemez olan bir talep ({Status}) güncellenmeye çalışıldı. Talep ID: {Id}, Kullanıcı: {User}",leaveRequestToUpdate.Status, id, userName);
                return $"Bu talep '{leaveRequestToUpdate.Status}' statüsünde olduğu için artık düzenlenemez.";
            }

            var otherLeaveRequests = await _context.LeaveRequests
                .Where(lr => lr.Id != id &&
                             lr.RequestingEmployeeId == userId &&
                             (lr.Status == RequestStatus.PENDING || lr.Status == RequestStatus.APPROVED))
                .ToListAsync();

            var isOverlap = otherLeaveRequests.Any(r => model.StartDate <= r.EndDate && model.EndDate >= r.StartDate);

            if (isOverlap)
            {
                var error = "Bu tarihler arasında bekleyen veya onaylanmış başka bir izniniz bulunmakta.";
                _logger.LogWarning("İzin talebi güncelleme sırasında tarih çakışması. Talep ID: {Id}, Kullanıcı: {User}", id, userName);
                return error;
            }

            leaveRequestToUpdate.LeaveType = model.LeaveType;
            leaveRequestToUpdate.StartDate = model.StartDate;
            leaveRequestToUpdate.EndDate = model.EndDate;
            leaveRequestToUpdate.Reason = model.Reason;
            leaveRequestToUpdate.UpdatedAt = DateTime.Now;
            leaveRequestToUpdate.UpdatedBy = userName;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "İzin talebi güncellenirken bir eşzamanlılık hatası oluştu. Talep ID: {Id}", id);
                return "Talep güncellenirken bir sorun oluştu. Sayfayı yenileyip tekrar deneyin.";
            }

            return null;
        }

        public async Task<LeaveRequest?> FindLeaveRequestByIdAsync(int id)
        {
            return await _context.LeaveRequests.FindAsync(id);
        }
    }
}