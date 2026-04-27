using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Poms.Web.Models;
using System.Security.Cryptography;
using System.Text;

namespace Poms.Web.Controllers;

/// <summary>
/// Handles seamless version switching between prototype and production with auto-login.
/// Uses HMAC-SHA256 signed tokens (60s expiry) without transmitting passwords.
/// Shared secret configured via SWITCH_SECRET or VersionSwitch__Secret on both Railway services.
/// </summary>
public class SwitchController : Controller
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly string _switchSecret;
    private readonly VersionSwitchOptions _switchOptions;

    public SwitchController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IConfiguration config,
        IOptions<VersionSwitchOptions> switchOptions)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _switchOptions = switchOptions.Value;
        _switchSecret = Environment.GetEnvironmentVariable("SWITCH_SECRET")
                        ?? Environment.GetEnvironmentVariable("VersionSwitch__Secret")
                        ?? _switchOptions.Secret
                        ?? config["VersionSwitch:Secret"]
                        ?? config["SWITCH_SECRET"]
                        ?? "poms-demo-switch-secret";
    }

    [HttpGet]
    public IActionResult Go(string? target)
    {
        var targetVariant = ResolveTargetVariant(target);
        var baseUrl = _switchOptions.GetUrl(targetVariant);
        if (string.IsNullOrWhiteSpace(baseUrl))
            return RedirectToAction("Index", "Home");

        if (User.Identity?.IsAuthenticated != true)
            return Redirect(baseUrl);

        var email = User.Identity!.Name!;
        var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var sig = Sign($"{email}:{ts}");

        var url = $"{baseUrl}/Switch/AutoLogin"
                + $"?user={Uri.EscapeDataString(email)}"
                + $"&ts={ts}"
                + $"&sig={Uri.EscapeDataString(sig)}";

        return Redirect(url);
    }

    [HttpGet]
    public async Task<IActionResult> AutoLogin(string user, long ts, string sig)
    {
        var age = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - ts;
        if (age < 0 || age > 60)
            return RedirectToAction("Index", "Home");

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

    private string ResolveTargetVariant(string? target)
    {
        if (string.Equals(target, "production", StringComparison.OrdinalIgnoreCase))
            return "production";

        if (string.Equals(target, "prototype", StringComparison.OrdinalIgnoreCase))
            return "prototype";

        return _switchOptions.TargetVariant;
    }
}
