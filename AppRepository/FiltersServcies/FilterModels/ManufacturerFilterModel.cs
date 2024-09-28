using OServcies.FiltersServcies.FilterModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.FiltersServcies.FilterModels
{
    public class ManufacturerFilterModel : ITableFilterModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
