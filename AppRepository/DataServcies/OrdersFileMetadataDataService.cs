using OzonDomains.Models;
using OzonRepositories.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.DataServcies
{
    public class OrdersFileMetadataDataService : IDataServcies
    {
        private readonly OrdersFileMetadataRepository _repository;

        public OrdersFileMetadataDataService(OrdersFileMetadataRepository repository)
        {
            _repository = repository;
        }

        public Task<int> AddOrdersFileMetadata(OrdersFileMetadata value)
        {
            return _repository.Add(value);
        }

        public Task<int> DeleteOrdersFileMetadata(OrdersFileMetadata value)
        {
            return _repository.Delete(value);
        }

        public async Task<List<OrdersFileMetadata>> GetOrdersFileMetadatas()
        {
            return await _repository.Get();
        }

        public async Task<OrdersFileMetadata> GetOrdersFileMetadataAsync(int id)
        {
            return await _repository.GetAsync(id);
        }

        public async Task<OrdersFileMetadata> GetOrdersFileMetadataAsync(OrdersFileMetadata value)
        {
            return await _repository.GetAsync(value);
        }

        public async Task<int> UpdateOrdersFileMetadata(OrdersFileMetadata value)
        {
            return await _repository.Update(value);
        }

        public async Task<int> SaveChanges()
        {
            return await _repository.SaveChanges();
        }
    }

}
