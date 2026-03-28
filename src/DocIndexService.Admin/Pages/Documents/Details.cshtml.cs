using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocIndexService.Admin.Pages.Documents;

public sealed class DetailsModel : PageModel
{
    public Guid DocumentId { get; private set; }

    public IActionResult OnGet(Guid id)
    {
        if (id == Guid.Empty)
        {
            return RedirectToPage("/Documents/Index");
        }

        DocumentId = id;
        return Page();
    }
}
