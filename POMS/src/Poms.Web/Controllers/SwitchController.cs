using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Poms.Domain.Entities;
using System.Security.Cryptography;
using System.Text;

namespace Poms.Web.Controllers;

/// <summary>
/// Handles seamless version switching between prototype and production with auto-login.
/// Uses HMAC-SHA256 signed tokens (60s expiry) — no password transmitted.
/// Shared secret configured via SWITCH_SECRET environment variable on both Railway services.
/// </summary>
public class SwitchController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly string _switchSecret;

    private const string ProductionUrl = "https://motivation-production-production.up.railway.app";
    private const string PrototypeUrl  = "https://motivation-production-f454.up.railway.app";

    public SwitchController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration config)
    {
        _signInManager  = signInManager;
        _userManager    = userManager;
        _switchSecret   = Environment.GetEnvironmentVariable("SWITCH_SECRET")
                          ?? config["SWITCH_SECRET"]
                          ?? "poms-demo-switch-secret";
    }

    // Called when user clicks the banner switch button.
    // Generates a signed token and redirects to the other app.
    [HttpGet]
    public IActionResult Go(string target)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect(target == "production" ? ProductionUrl : PrototypeUrl);

        var email = User.Identity!.Name!;
        var ts    = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sig   = Sign($"{email}:{ts}");

        var baseUrl = target == "production" ? ProductionUrl : PrototypeUrl;
        var url = $"{baseUrl}/Switch/AutoLogin"
                + $"?user={Uri.EscapeDataString(email)}"
                + $"&ts={ts}"
                + $"&sig={Uri.EscapeDataString(sig)}";

        return Redirect(url);
    }

    // Called by the other app after redirect — validates token and signs user in.
    [HttpGet]
    public async Task<IActionResult> AutoLogin(string user, long ts, string sig)
    {
        // Reject tokens older than 60 seconds
        var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts;
        if (age < 0 || age > 60)
            return RedirectToAction("Index", "Home");

        // Validate HMAC signature
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(sig),
                Encoding.UTF8.GetBytes(Sign($"{user}:{ts}"))))
            return RedirectToAction("Index", "Home");

        var appUser = await _userManager.FindByEmailAsync(user)
                   ?? await _userManager.FindByNameAsync(user);
        if (appUser == null)
            return RedirectToAction("Index", "Home");

        await _signInManager.SignInAsync(appUser, isPersistent: false);
        return RedirectToAction("Index", "Home");
    }

    private string Sign(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_switchSecret));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }
}
