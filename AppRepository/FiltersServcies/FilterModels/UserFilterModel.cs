using OzonDomains;
using OzonDomains.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.FiltersServcies.FilterModels
{
    public class UserFilterModel
    {
        public string? UserName { get; set; }

        public UserAccess? UserAccess { get; set; }

        private string _userRole { get; set; }
        public string UserRole
        {
            get { return _userRole; }
            set
            {
                if (value == "Все")
                {
                    _userRole = null;
                }
                else
                {
                    _userRole = value;
                }
            }
        }
    }
}
