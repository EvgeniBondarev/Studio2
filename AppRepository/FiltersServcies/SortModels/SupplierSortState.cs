using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.FiltersServcies.SortModels
{
    public enum SupplierSortState
    {
        StandardState,

        NameAsc, 
        NameDesc,

        CostFactorAsc, 
        CostFactorDesc,

        WeightFactorAsc,
        WeightFactorDesc,

        CurrencyCodeAsc, 
        CurrencyCodeDesc,

        WeightFactorCurrencyCodeAsc,
        WeightFactorCurrencyCodeDesc,
    }
}
