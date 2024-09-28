using System.ComponentModel.DataAnnotations;

namespace OzonDomains.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [StringLength(50)]
        [Display(Name = "Поставщик")]
        public string? Name { get; set; }

        [Display(Name = "Коэффицент себестоимости")]
        public decimal? CostFactor { get; set; } = 1;

        [Display(Name = "Стоимость за кг")]
        public decimal? WeightFactor { get; set; } = 1;

        [Display(Name = "Валюта")]
        public CurrencyCode CurrencyCode { get; set; }

        [Display(Name = "Валюта стоимость за кг")]
        public CurrencyCode WeightFactorCurrencyCode { get; set; }


    }
}
