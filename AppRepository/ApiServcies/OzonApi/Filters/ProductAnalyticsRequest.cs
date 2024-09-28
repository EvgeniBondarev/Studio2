using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.ApiServcies.OzonApi.Filters
{
    public class ProductAnalyticsRequest : IRequestModel
    {
        [JsonProperty("posting_number")]
        public string PostingNumber { get; set; }

        [JsonProperty("with")]
        public ProductAnalyticsRequestFilter With { get; set; }
    }
}
