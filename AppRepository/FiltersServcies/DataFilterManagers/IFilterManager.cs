using OServcies.FiltersServcies.FilterModels;
using OzonDomains.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.FiltersServcies.DataFilterManagers
{
    public interface IFilterManager<T, K>
    {
        List<T> FilterByRadioButton(List<T> filterModel, string buttonState);
        List<T> FilterByFilterData(List<T> filterModel, K filterData);
    }
}
