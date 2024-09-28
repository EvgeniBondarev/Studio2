using Servcies.ParserServcies.FielParsers.Models;

namespace OzonOrdersWeb.ViewModels.OrderViewModels
{
    public class MatchedRowViewModel
    {
        public List<MatchedRowModel> MatchedResults { get; set; }
        public string? MainFileName { get; set; }
        public string? ScondaryFileName { get; set; }
    }
}
