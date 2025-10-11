using IzinTalepUygulamasi.Models;
using System.Security.Claims;

namespace IzinTalepUygulamasi.Services.Abstract
{
    public interface IAuthService
    {
        User? ValidateUser(string username, string password);
        ClaimsPrincipal CreateClaimsPrincipal(User user);
    }
}
