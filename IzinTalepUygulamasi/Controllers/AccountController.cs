using IzinTalepUygulamasi.Models;
using IzinTalepUygulamasi.Models.ViewModels;
using IzinTalepUygulamasi.Services.Abstract;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IzinTalepUygulamasi.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;
        public AccountController(IAuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Geçersiz giriş denemesi (Validation Hatası): Form kurallara uymuyor. Denenen Kullanıcı Adı: {Username}", model.Username);
                return View(model); 
            }
            
            var user = await _authService.ValidateUser(model.Username, model.Password);

            if (user != null)
            {
                var principal = _authService.CreateClaimsPrincipal(user);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                _logger.LogInformation("Kullanıcı başarıyla giriş yaptı: {Username} (ID: {UserId})", user.Username, user.Id);
                return RedirectToAction("Index", "Home");
            }
            _logger.LogWarning("Başarısız giriş denemesi: Yanlış kullanıcı adı veya şifre. Denenen Kullanıcı Adı: {Username}", model.Username);
            ModelState.AddModelError(string.Empty, "Geçersiz kullanıcı adı veya şifre.");

            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("Kullanıcı başarıyla çıkış yaptı: {Username}", User.Identity?.Name);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            var attemptedPath = HttpContext.Request.Query["ReturnUrl"].FirstOrDefault();

            _logger.LogWarning("Yetkisiz erişim denemesi: {Username} kullanıcısı {Path} yoluna erişmeye çalıştı.", User.Identity?.Name, attemptedPath ?? "[Bilinmeyen Yol]");
            return View();
        }
    }
}
