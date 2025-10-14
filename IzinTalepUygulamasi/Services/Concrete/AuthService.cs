using IzinTalepUygulamasi.Data; // DbContext için
using IzinTalepUygulamasi.Models;
using IzinTalepUygulamasi.Services.Abstract;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore; // ToLower ve FirstOrDefaultAsync için
using System.Security.Claims;

namespace IzinTalepUygulamasi.Services.Concrete
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<User?> ValidateUser(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());


            if (user != null && user.PasswordHash == password)//register işlemi olursa değiştirilecek
            {
                return user;
            }

            return null;
        }

        public ClaimsPrincipal CreateClaimsPrincipal(User user)
        {
            var claims = new List<Claim> {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            return new ClaimsPrincipal(claimsIdentity);
        }
    }
}