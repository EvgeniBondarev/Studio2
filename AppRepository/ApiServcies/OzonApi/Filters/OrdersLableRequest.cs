using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.ApiServcies.OzonApi.Filters
{
    public class OrdersLableRequest : IRequestModel
    {
        [JsonProperty("posting_number")]
        public string[] ShipmentNumber { get; set; }
    }
}
