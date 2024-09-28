using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using RestEase.Implementation;
using Servcies.ApiServcies.InterpartsApi.Filters;
using Servcies.ApiServcies.InterpartsApi.Models;
using Servcies.ApiServcies.OzonApi;

namespace Servcies.ApiServcies.InterpartsApi
{
    public class InterpartsApiDataManager
    {
        private InterpartsApiClient _interpartsApiClient;
        public string _key;

        public InterpartsApiDataManager()
        {
            _interpartsApiClient = new InterpartsApiClient();
        }
        public InterpartsApiDataManager SetClient(string key)
        {
            _key = key;
            return this;
        }

        public async Task<string> GetbBrandByCode(string code)
        {
            if(string.IsNullOrEmpty(code))
            {
                return null;
            }
            SearchBrandRequest searchBrandRequest = new SearchBrandRequest()
            {
                Code = code,
                RequestUrl = InterpartsApiUrl.SEARCH_BASE_URL + "?token=" + _key + "&code=" + code
            };

            var result = _interpartsApiClient.MakeRequest(searchBrandRequest);

            if (result != null && result["result"] != null && result["result"].HasValues)
            {
                return result["result"][0]["brand"].ToString();
            }
            return null;
        }

        public async Task<List<SupplierNameAndDirectionModel>> GetbSupplierNameAndDirection(string code, string brand)
        {
            SearchBrandRequest searchBrandRequest = new SearchBrandRequest()
            {
                Code = code,
                RequestUrl = InterpartsApiUrl.SEARCH_BASE_URL + "?token=" + _key + "&code=" + code + "&brand=" + brand
            };

            List<SupplierNameAndDirectionModel> supplierNameAndDirectionModels = [];
            var result = _interpartsApiClient.MakeRequest(searchBrandRequest);

            if(result["result"] != null)
            {
                foreach (var pair in result["result"])
                {
                    supplierNameAndDirectionModels.Add(new SupplierNameAndDirectionModel()
                    {
                        SupplierName = pair["supplierName"].ToString(),
                        Direction = pair["direction"].ToString()
                    });
                }
            }
            return supplierNameAndDirectionModels;
        }
    }
}
