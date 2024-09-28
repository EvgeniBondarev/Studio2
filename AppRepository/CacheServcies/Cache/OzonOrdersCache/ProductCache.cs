using Microsoft.Extensions.Caching.Memory;
using OzonDomains.Models;
using Servcies.DataServcies;


namespace Services.CacheServcies.Cache.OzonOrdersCache
{
    public class ProductCache : AppCache<Product>, IAppCache<Product>
    {
        private ProductsDataServcies _productServcies;
        public ProductCache(IMemoryCache memoryCache, ProductsDataServcies productsDataServcies) : base(memoryCache)
        {
            _productServcies = productsDataServcies;
        }

        public async Task<List<Product>> Get()
        {
            _cache.TryGetValue("Products", out List<Product>? products);

            if (products is null)
            {
                products = await Set();
            }
            return products;
        }

        public Task<List<Product>> Get(int page)
        {
            throw new NotImplementedException();
        }

        public async Task<List<Product>> Set()
        {
            var products = await _productServcies.GetProducts();
            _cache.Set("Products", products, new MemoryCacheEntryOptions().SetAbsoluteExpiration(_saveTime));
            return products;
        }

        public async Task Update()
        {
            var products = await _productServcies.GetProducts();
            _cache.Set("Products", products, new MemoryCacheEntryOptions().SetAbsoluteExpiration(_saveTime));
        }
    }
}
