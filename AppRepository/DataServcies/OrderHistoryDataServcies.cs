using Microsoft.EntityFrameworkCore;
using OzonDomains.Models;
using OzonRepositories.Context;
using OzonRepositories.Data;

namespace Servcies.DataServcies
{
    public class OrderHistoryDataServcies : IDataServcies
    {
        private readonly OrdersHistoryRepository _repository;
        private readonly OzonOrderContext _context;

        public OrderHistoryDataServcies(OrdersHistoryRepository repository,
                                        OzonOrderContext context)
        {
            _repository = repository;
            _context = context;
        }

        public async Task<List<Order>> GetHistoryList()
        {
            return await _repository.Get();
        }

        public async Task<List<Order>> GetOrderHistoryList(int id)
        {
            return await _repository.Get(id);
        }

        public async Task<List<Order>> GetOrderHistoryList(Order order)
        {
            return await _repository.Get(order.Id);
        }

        public async Task<List<OrderHistory>> GetFullOrderHistory(int id)
        {
            List<OrderHistory> historyList = await GetOrderHistoryOrderBy(id);

            foreach (var order in historyList)
            {
                order.Order.AppStatus = _context.AppStatuses.FirstOrDefault(a => a.Id == order.Order.AppStatusId);
                order.Order.ShipmentWarehouse = _context.Warehouses.FirstOrDefault(w => w.Id == order.Order.ShipmentWarehouseId);
                order.Order.Сurrency = _context.Currencys.FirstOrDefault(c => c.Id == order.Order.СurrencyId);
                order.Order.ProductInfo = _context.Products.FirstOrDefault(p => p.Id == order.Order.ProductInfoId);
                order.Order.Supplier = _context.Suppliers.FirstOrDefault(p => p.Id == order.Order.SupplierId);
                order.Order.OzonClient = _context.OzonClients.FirstOrDefault(p => p.Id == order.Order.OzonClientId);
                order.Order.Manufacturer = _context.Manufacturers.FirstOrDefault(p => p.Id == order.Order.ManufacturerId);
            }
            return historyList.Reverse<OrderHistory>().ToList();
        }

        public async Task<List<OrderHistory>> GetOrderHistoryOrderBy(int id)
        {
            return _context.Orders.TemporalAll()
                .Where(o => o.Id == id)
                .OrderBy(e => EF.Property<DateTime>(e, "PeriodStart"))
                .Select(o => new OrderHistory
                {
                    Order = o,
                    Start = EF.Property<DateTime>(o, "PeriodStart"),
                    End = EF.Property<DateTime>(o, "PeriodEnd")
                }).ToList();
        }

        public async Task<List<OrderHistory>> GetOrderHistoryOrderBy(Order order)
        {
            var histOrder = await GetOrderHistoryList(order);

            return _context.Orders.TemporalAll()
                .Where(o => o.Id == order.Id)
                .OrderBy(e => EF.Property<DateTime>(e, "PeriodStart"))
                .Select(o => new OrderHistory
                {
                    Order = o,
                    Start = EF.Property<DateTime>(o, "PeriodStart"),
                    End = EF.Property<DateTime>(o, "PeriodEnd")
                }).ToList();
        }

        public async Task<int> OrderRecoverUpdate(int id, DateTime stratRecDate, DateTime endRecDate)
        {
            var orderRec = _context.Orders.TemporalFromTo(stratRecDate, endRecDate).FirstOrDefault(o => o.Id == id);

            var result = 0;

            using (var transaction = _context.Database.BeginTransaction())
            {
                orderRec.IsVerified = true;

                _context.Orders.Update(orderRec);
                result = _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            return result;
        }

        public async Task<int> OrderRecoverUpdate(Order order, DateTime stratRecDate, DateTime endRecDate)
        {
            var orderToRec = _context.Orders.TemporalBetween(stratRecDate, endRecDate).FirstOrDefault(o => o.Id == order.Id);
            var result = 0;

            using (var transaction = _context.Database.BeginTransaction())
            {
                _context.Orders.Update(orderToRec);
                result = _context.SaveChanges();
                _context.Database.CommitTransaction();
            }
            return result;
        }

        public async Task<int> OrderRecoverDelete(int id, DateTime stratRecDate, DateTime endRecDate)
        {
            var orderToRec = _context.Orders.TemporalBetween(stratRecDate, endRecDate).FirstOrDefault(o => o.Id == id);
            var result = 0;

            using (var transaction = _context.Database.BeginTransaction())
            {
                _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[Users] ON");
                _context.Orders.Add(orderToRec);
                _context.SaveChanges();

                _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[Users] OFF");
                _context.Database.CommitTransaction();
            }
            return result;
        }


        public async Task<int> OrderRecoverDelete(Order order, DateTime stratRecDate, DateTime endRecDate)
        {
            var orderToRec = _context.Orders.TemporalBetween(stratRecDate, endRecDate).FirstOrDefault(o => o.Id == order.Id);
            var result = 0;

            using (var transaction = _context.Database.BeginTransaction())
            {
                _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[Users] ON");
                _context.Orders.Add(orderToRec);
                _context.SaveChanges();

                _context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT [dbo].[Users] OFF");
                _context.Database.CommitTransaction();
            }
            return result;
        }
        public async Task<int> SaveChanges()
        {
            return await _repository.SaveChanges();
        }
    }
}
