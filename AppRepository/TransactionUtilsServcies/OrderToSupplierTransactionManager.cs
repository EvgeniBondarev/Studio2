using Microsoft.EntityFrameworkCore;
using OzonDomains;
using OzonDomains.Models;
using OzonRepositories.Context;
using OzonRepositories.Data;
using Servcies.DataServcies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.TransactionUtilsServcies
{
    public class OrderToSupplierTransactionManager : ITransactionManager
    {
        private readonly OrdersDataServcies _ordersDataServcies;
        private readonly TransactionDataServcies _transactionDataServcies;
        private readonly OzonOrderContext _context;

        public OrderToSupplierTransactionManager(OrdersDataServcies ordersDataServcies,
                                                 TransactionDataServcies transactionDataServcies,
                                                 OzonOrderContext context)
        {
            _ordersDataServcies = ordersDataServcies;
            _transactionDataServcies = transactionDataServcies;
            _context = context;
        }

        public async Task<(int, string)> CreateTransaction(List<Order> orders, 
                                                string userName, 
                                                DateTime createDateTime,
                                                string comment)
        {
                Transaction transaction = new Transaction()
                {
                    Type = TransactionType.OrderedToSupplier,
                    Orders = orders,
                    CreateBy = userName,
                    CreatedDateTime = createDateTime,
                    Comment = comment
                };
                await _transactionDataServcies.AddTransaction(transaction);

                foreach(var order in orders)
                {
                    await ConfirmAccepted(order);
                    
                }
                return (await _transactionDataServcies.SaveChanges(), $"{transaction.FormattedCreatedDate}\t{transaction.FormattedCreatedTime}");
        }


        public async Task ConfirmAccepted(Order order)
        {
            order.IsAccepted = true;
            order.IsVerified = true;
            order.UpdatedColumns = null;
        }
    }
}
