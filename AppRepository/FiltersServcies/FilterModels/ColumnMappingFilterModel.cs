using OServcies.FiltersServcies.FilterModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.FiltersServcies.FilterModels
{
    public class ColumnMappingFilterModel : ITableFilterModel
    {
        public string? MappingName { get; set; }
    }
}
