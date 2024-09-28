using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.FiltersServcies.DataFilterManagers
{
    public interface IFilterManagerAsync<T, K>
    {
        Task<List<T>> FilterByRadioButton(List<T> filterModel, string buttonState);
        Task<List<T>> FilterByFilterData(List<T> filterModel, K filterData);
    }
}
