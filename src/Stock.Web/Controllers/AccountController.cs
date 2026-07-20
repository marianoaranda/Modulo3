using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stock.Web.Models;
using Stock.Web.Security;
using Stock.Web.Services;

namespace Stock.Web.Controllers;

public class AccountController : Controller
{
    private readonly IStockApiClient _api;

    public AccountController(IStockApiClient api) => _api = api;

    /// <summary>Pantalla de inicio de sesión (RF-11), única pantalla anónima (RF-12).</summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var sesion = await _api.LoginAsync(model.Usuario, model.Password);
        if (sesion is null)
        {
            // Mismo mensaje para usuario inexistente y contraseña incorrecta (AC-12, AC-13).
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos");
            return View(model);
        }

        var identidad = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.Name, model.Usuario),
                new Claim(ClaimTypes.GivenName, sesion.NombreCompleto),
                new Claim(ClaimTypes.Role, sesion.Perfil),
                new Claim(SessionClaims.Token, sesion.Token)
            },
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { ExpiresUtc = sesion.ExpiraUtc });

        return Url.IsLocalUrl(model.ReturnUrl)
            ? Redirect(model.ReturnUrl!)
            : RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
