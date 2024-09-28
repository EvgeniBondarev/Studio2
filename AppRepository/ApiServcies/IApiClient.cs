using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.ApiServcies
{
    public interface IApiClient
    {
        public JObject MakeRequest<T>(T requestModel, string requestUri) where T : IRequestModel;

        public Task<bool> GetTestRequest(string businessId, string apiKey);
    }
}
