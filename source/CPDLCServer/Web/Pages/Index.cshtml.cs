using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CPDLCServer.Web.Pages;

// The CPDLC overview is the start page
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        return Redirect("/cpdlc");
    }
}
