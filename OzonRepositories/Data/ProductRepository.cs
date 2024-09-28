using OzonDomains.Models;
using OzonRepositories.Context;

namespace OzonRepositories.Data
{
    public class ProductRepository : MainRepository, IRepository<Product>
    {
        public ProductRepository(OzonOrderContext orderContext) : base(orderContext)
        {
        }

        public async Task<int> Add(Product value)
        {
            _context.Products.AddAsync(value);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> Delete(Product value)
        {
            _context.Products.Remove(value);
            return await _context.SaveChangesAsync();
        }

        public async Task<List<Product>> Get()
        {
            return _context.Products.ToList();  
        }

        public async Task<Product> GetAsync(int id)
        {
            return _context.Products.FirstOrDefault(p => p.Id == id);
        }

        public async Task<Product> GetAsync(Product value)
        {
            return _context.Products.FirstOrDefault(p => p.Article == value.Article);
        }

        public async Task<int> Update(Product value)
        {
            _context.Products.Update(value);
            return await _context.SaveChangesAsync();
        }
    }
}
