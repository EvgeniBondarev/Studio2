using OzonDomains.Models;
using OzonRepositories.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.DataServcies
{
    public class WarehouseDataServcies : IDataServcies
    {
        private readonly WarehouseRepository _repository;

        public WarehouseDataServcies(WarehouseRepository repository)
        {
            _repository = repository;
        }

        public Task<int> AddWarehouse(Warehouse value)
        {
            return _repository.Add(value);
        }

        public Task<int> DeleteWarehouse(Warehouse value)
        {
            return (_repository.Delete(value));
        }

        public async Task<List<Warehouse>> GetWarehouses()
        {
            return await _repository.Get();
        }

        public async Task<Warehouse> GetWarehouseAsync(int id)
        {
            return await _repository.GetAsync(id);
        }

        public async Task<Warehouse> GetWarehouseAsync(Warehouse value)
        {
            return await _repository.GetAsync(value);
        }

        public async Task<int> UpdateWarehouse(Warehouse value)
        {
            return (await _repository.Update(value));
        }

        public async Task<int> SaveChanges()
        {
           return await _repository.SaveChanges();
        }
    }
}
