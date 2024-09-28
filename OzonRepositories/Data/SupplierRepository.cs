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
    public class SupplierRepository : MainRepository, IRepository<Supplier>
    {
        public SupplierRepository(OzonOrderContext orderContext) : base(orderContext)
        {
        }

        public Task<int> Add(Supplier value)
        {
            _context.Suppliers.AddAsync(value);
            return _context.SaveChangesAsync();
        }

        public Task<int> Delete(Supplier value)
        {
            _context.Remove(value);
            return _context.SaveChangesAsync();
        }

        public async Task<List<Supplier>> Get()
        {
            return _context.Suppliers.ToList();
        }

        public async Task<Supplier> GetAsync(int id)
        {
            return _context.Suppliers.Find(id);
        }

        public async Task<Supplier> GetAsync(Supplier value)
        {
            return _context.Suppliers.SingleOrDefault(a => a.Name == value.Name);
        }

        public async Task<int> Update(Supplier value)
        {
            _context.Suppliers.Update(value);
            return _context.SaveChanges();
        }
    }
}
