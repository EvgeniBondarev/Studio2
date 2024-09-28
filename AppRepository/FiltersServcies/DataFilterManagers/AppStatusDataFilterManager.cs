using OzonDomains.Models;
using Servcies.FiltersServcies.FilterModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.FiltersServcies.DataFilterManagers
{
    public class AppStatusDataFilterManager : IFilterManager<AppStatus, AppStatusFilterModel>
    {
        private readonly DataFilter<AppStatus> _filter;

        public AppStatusDataFilterManager(DataFilter<AppStatus> filter)
        {
            _filter = filter;
        }

        public List<AppStatus> FilterByFilterData(List<AppStatus> appStatuses, AppStatusFilterModel filterData)
        {
            appStatuses = _filter.FilterByString(appStatuses, pr => pr.Name, filterData.Name).ToList();

            return appStatuses;
        }

        public List<AppStatus> FilterByRadioButton(List<AppStatus> filterModel, string buttonState)
        {
            throw new NotImplementedException();
        }
    }
}
