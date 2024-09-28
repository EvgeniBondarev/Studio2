using Microsoft.EntityFrameworkCore;
using OzonDomains.Models;
using OzonRepositories.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OzonRepositories.Data
{
    public class OrdersHistoryRepository : MainRepository, IRepository<Order>
    {
        public OrdersHistoryRepository(OzonOrderContext orderContext) : base(orderContext)
        {
        }

        public Task<int> Add(Order value)
        {
            throw new NotImplementedException();
        }

        public Task<int> Delete(Order value)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Order>> Get()
        {
            return _context.Orders.TemporalAll()
                                   .ToList();
        }

        public async Task<List<Order>> Get(int id)
        {
            var hist =  await Get();
            return hist.Where(o => o.Id == id).ToList();
        }

        public async Task<List<Order>> Get(Order order)
        {
            var hist = await Get();
            return hist.Where(o => o.Id == order.Id).ToList();
        }

        public async Task<Order> GetAsync(int id)
        {
            var hist = await Get();
            return hist.FirstOrDefault(o => o.Id == id);
        }

        public async Task<Order> GetAsync(Order value)
        {
            var hist = await Get();
            return hist.FirstOrDefault(o => o.Id == value.Id);
        }

        public Task<int> Update(Order value)
        {
            throw new NotImplementedException();
        }
    }
}
