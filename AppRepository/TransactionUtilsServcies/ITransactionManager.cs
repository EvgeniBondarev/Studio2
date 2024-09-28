using OzonDomains.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.TransactionUtilsServcies
{
    public interface ITransactionManager
    {
        Task<(int, string)> CreateTransaction(List<Order> orders, string userName, DateTime createDateTime, string comment);
    }
}
