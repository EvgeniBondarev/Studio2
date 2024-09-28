using OzonDomains.Models;

namespace OzonOrdersWeb.ViewModels.OrderViewModels
{
    public class OrderRowViewModel
    {
        public Order Order { get; set; }
        public int Index { get; set; }
        public List<string> UnavailableStatuses { get; set; } = new List<string>
        {
            "Доставлен",
            "Отменён",
            "Отменен",
            "Доставляется",
            "Ожидает в ПВЗ",
            "Отменен при до.",
            "Отменен при об",
            "Отгружен прода",
            "Отменен продав"
        };
        public bool OrderWithOneMatches { get; set; }
        public Manufacturer FileOrderManufacturer { get; set; }
        public int OrderId { get; set; }

        public bool CheckedCase1
        {
            get
            {
                return Order != null && Order.AppStatus != null && 
                    Order.AppStatus.Name == "Заказан поставщику" && Order.Status == "Доставлен";
            }
        }

        public bool CheckedCase2
        {
            get
            {
                return Order != null && Order.AppStatus != null && 
                    Order.AppStatus.Name == "Заказан поставщику" && Order.Status == "Доставляется";
            }
        }
        
    }
}
