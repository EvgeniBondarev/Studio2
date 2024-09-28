using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.ApiServcies.InterpartsApi
{
    public class InterpartsApiClient 
    {
        public JObject MakeRequest<T>(T requestModel, string requestUri = null) where T : IInterpartsRequestModel
        {
            try
            {
                if(requestUri == null)
                {
                    requestUri = requestModel.RequestUrl;
                }
                var request = (HttpWebRequest)WebRequest.Create(requestUri);
                request.Method = "GET";
                request.ContentType = "application/json";

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        var jsonResponse = streamReader.ReadToEnd();
                        return JObject.Parse(jsonResponse);
                    }
                }
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse httpResponse)
            {
                throw new Exception($"Request failed. Status code: {httpResponse.StatusCode}");
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while processing the request.", ex);
            }
        }

        public Task<bool> GetTestRequest(string businessId, string apiKey)
        {
            throw new NotImplementedException();
        }
    }

}
