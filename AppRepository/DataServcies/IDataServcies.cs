using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.DataServcies
{
    public interface IDataServcies
    {
        Task<int> SaveChanges();
    }
}
