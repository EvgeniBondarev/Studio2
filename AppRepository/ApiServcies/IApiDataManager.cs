using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.ApiServcies
{
    public interface IApiDataManager<T> where T : IApiDataManager<T>
    {
        Task<bool> GetTestRequest(string clientId, string apiKey);
        T SetClient(string clientId, string apiKey);
    }
}
