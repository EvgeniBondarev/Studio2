using OServcies.FiltersServcies.FilterModels;
using OzonDomains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.FiltersServcies.FilterModels
{
    public class OzonClientFilterModel : ITableFilterModel
    {
        public string? Name { get; set; }

        private CurrencyCode? _currencyCode { get; set; }
        public CurrencyCode? CurrencyCode
        {
            get { return _currencyCode; }
            set
            {
                if (value == OzonDomains.CurrencyCode.NON)
                {
                    _currencyCode = null;
                }
                else
                {
                    _currencyCode = value;
                }
            }
        }
    }
}
