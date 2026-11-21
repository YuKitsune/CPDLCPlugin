namespace CPDLCServer.Web.Pages.ViewModels;

// The page links below a table. The path is the page URL without the query string.
public record PaginationViewModel(string Path, int CurrentPage, int TotalPages)
{
    public string Url(int pageNumber) => $"{Path}?pageNumber={pageNumber}";
}
