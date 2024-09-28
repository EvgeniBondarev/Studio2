using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.ApiServcies.YandexApi.Filters
{
    public class YandexFilter : IYandexModel
    {
        [JsonProperty("clientId")]
        public string ClientId { get; set; }
    }
}
