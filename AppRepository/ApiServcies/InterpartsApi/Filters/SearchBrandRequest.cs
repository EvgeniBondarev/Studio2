using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.ApiServcies.InterpartsApi.Filters
{
    public class SearchBrandRequest : IInterpartsRequestModel
    {
        public string RequestUrl { get; set; }
        public string Code { get; set; }
    }
}
