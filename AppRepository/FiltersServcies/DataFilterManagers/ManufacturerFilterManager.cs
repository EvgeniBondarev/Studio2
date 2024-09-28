using OfficeOpenXml.Style;
using OzonDomains.Models;
using Servcies.FiltersServcies.FilterModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.FiltersServcies.DataFilterManagers
{
    public class ManufacturerFilterManager : IFilterManager<Manufacturer, ManufacturerFilterModel>
    {
        private DataFilter<Manufacturer> _filter;
        public ManufacturerFilterManager(DataFilter<Manufacturer> dataFilter)
        {
            _filter = dataFilter;
        }

        public List<Manufacturer> FilterByFilterData(List<Manufacturer> manufacturers, ManufacturerFilterModel filterData)
        {
            manufacturers = _filter.FilterByString(manufacturers, pr => pr.Code, filterData.Code).ToList();
            manufacturers = _filter.FilterByString(manufacturers, pr => pr.Name, filterData.Name).ToList();

            return manufacturers;
        }

        public List<Manufacturer> FilterByRadioButton(List<Manufacturer> filterModel, string buttonState)
        {
            throw new NotImplementedException();
        }
    }
}
