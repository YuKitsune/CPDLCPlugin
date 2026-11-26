namespace CPDLCServer.Web.Pages.ViewModels;

// The page links below a table. The path is the page URL. It can have a query string, for example a search.
public record PaginationViewModel(string Path, int CurrentPage, int TotalPages)
{
    public string Url(int pageNumber) => $"{Path}{(Path.Contains('?') ? '&' : '?')}pageNumber={pageNumber}";
}
