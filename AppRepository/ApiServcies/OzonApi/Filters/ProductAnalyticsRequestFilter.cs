using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.ApiServcies.OzonApi.Filters
{
    public class ProductAnalyticsRequestFilter
    {
        [JsonProperty("analytics_data")]
        public bool AnalyticsData { get; set; }

        [JsonProperty("barcodes")]
        public bool Barcodes { get; set; }

        [JsonProperty("financial_data")]
        public bool FinancialData { get; set; }

        [JsonProperty("product_exemplars")]
        public bool ProductExemplars { get; set; }

        [JsonProperty("translit")]
        public bool Translit { get; set; }
    }
}
