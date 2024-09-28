using Microsoft.EntityFrameworkCore;
using OzonDomains.Models;
using OzonRepositories.Context;
using System.Linq.Expressions;

namespace OzonRepositories.Data
{
    public class OrderRepository : MainRepository
    {
        private readonly OzonOrderContext _context;
        public OrderRepository(OzonOrderContext context) : base(context)
        {
            _context = context;
        }

        public async Task<int> Add(Order value)
        {
            await _context.Orders.AddAsync(value);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> Delete(Order value)
        {
            _context.Orders.Remove(value);
            return await _context.SaveChangesAsync();
        }

        public async Task<IQueryable<Order>> Get()
        {
            return _context.Orders.Include(c => c.Сurrency)
                                        .Include(w => w.ShipmentWarehouse)
                                        .Include(p => p.ProductInfo)
                                        .Include(a => a.AppStatus)
                                        .Include(s => s.Supplier)
                                        .Include(o => o.OzonClient)
                                        .Include(m => m.Manufacturer)
                                        .Include(f => f.ExcelFileData)
                                        .OrderByDescending(o => o.ProcessingDate);
        }

        public async Task<Order> GetAsync(int id)
        {
            return await _context.Orders.Include(c => c.Сurrency)
                                        .Include(w => w.ShipmentWarehouse)
                                        .Include(p => p.ProductInfo)
                                        .Include(a => a.AppStatus)
                                        .Include(s => s.Supplier)
                                        .Include(o => o.OzonClient)
                                        .Include(m => m.Manufacturer)
                                        .Include(f => f.ExcelFileData)
                                        .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order> GetAsync(Order value)
        {
            return await _context.Orders.Include(c => c.Сurrency)
                                        .Include(w => w.ShipmentWarehouse)
                                        .Include(p => p.ProductInfo)
                                        .Include(a => a.AppStatus)
                                        .Include(s => s.Supplier)
                                        .Include(o => o.OzonClient)
                                        .Include(m => m.Manufacturer)
                                        .Include(f => f.ExcelFileData)
                                        .FirstOrDefaultAsync(o => o.Key == value.Key);
        }

        public async Task<IQueryable<Order>> GetOrdersByManufacturerCode(string code)
        {
            return _context.Orders.Include(m => m.Manufacturer)
                                  .Where(o => o.Manufacturer.Code == code);
        }

        public async Task<int> Update(Order value)
        {
            _context.Orders.Update(value);
            return await _context.SaveChangesAsync();
        }

        public async Task<IQueryable<Order>> GetOrdersWithPagination(int skip, int take)
        {
            return _context.Orders
                .Include(c => c.Сurrency)
                .Include(w => w.ShipmentWarehouse)
                .Include(p => p.ProductInfo)
                .Include(a => a.AppStatus)
                .Include(s => s.Supplier)
                .Include(o => o.OzonClient)
                .Include(m => m.Manufacturer)
                .Include(f => f.ExcelFileData)
                .OrderByDescending(o => o.ProcessingDate) // Сортировка по убыванию даты обработки (или другого поля, например, ID)
                .Skip(skip)
                .Take(take);
        }

        // Новый метод для получения общего количества заказов
        public async Task<int> GetTotalOrderCount()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<IQueryable<KeyValuePair<string, int>>> GetUniqueValues<T>(Expression<Func<Order, T>> selector, Expression<Func<Order, bool>> predicate = null) where T : class
        {
            IQueryable<Order> query = _context.Orders;

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return query
                .Select(selector)
                .Where(value => value != null)
                .GroupBy(value => value)
                .Select(group => new KeyValuePair<string, int>(group.Key.ToString(), group.Count()))
                .AsQueryable();
        }

        public async Task<List<string>> GetShipmentNumbers()
        {
            return await _context.Orders
                                 .Where(o => o.ShipmentNumber != null)
                                 .Select(o => o.ShipmentNumber)
                                 .ToListAsync();
        }
    }
}
