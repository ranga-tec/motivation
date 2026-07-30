using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Poms.Web.Areas.Identity.Pages.Account;

[Authorize(Policy = "AdminOnly")]
public sealed class RegisterModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToAction("Users", "Admin", new { area = string.Empty });
    }

    public IActionResult OnPost()
    {
        return RedirectToAction("Users", "Admin", new { area = string.Empty });
    }
}
