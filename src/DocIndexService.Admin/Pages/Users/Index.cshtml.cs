using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocIndexService.Admin.Pages.Users;

[Authorize(Policy = "CanManageUsers")]
public sealed class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
