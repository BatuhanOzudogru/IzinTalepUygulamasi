using IzinTalepUygulamasi.Models;
using IzinTalepUygulamasi.Services.Abstract;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace IzinTalepUygulamasi.Services.Concrete
{
    public class AuthService : IAuthService
    {

        private readonly List<User> _users = new List<User>
        {
            new User { Id = 1, FullName = "Ali Yönetici", Username = "manager", Role = "Manager", PasswordHash = "123" },
            new User { Id = 2, FullName = "Ayşe Çalışan", Username = "ayse", Role = "Employee", PasswordHash = "123" },
            new User { Id = 3, FullName = "Mehmet Çalışan", Username = "mehmet", Role = "Employee", PasswordHash = "123" }
        };

        public ClaimsPrincipal CreateClaimsPrincipal(User user)
        {
            var claims = new List<Claim> {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role), 
                new Claim("FullName", user.FullName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
            return new ClaimsPrincipal(claimsIdentity);
        }

        public User? ValidateUser(string username, string password)
        {
            return _users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower() && u.PasswordHash == password);
        }
    }
}
