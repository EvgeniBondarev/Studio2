using Microsoft.AspNetCore.Mvc.Rendering;
using OzonDomains.Models;

namespace OzonOrdersWeb.ViewModels.OzonClientViewModels
{
    public class EditOzonClientViewModel
    {
        public OzonClient OzonClient { get; set; }
        public List<SelectListItem> CurrencyCodes { get; set; }
        public List<SelectListItem> ClientTypes { get; set; }
    }
}
