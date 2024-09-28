using OzonDomains.Models;
using OzonRepositories.Data;

namespace Servcies.DataServcies
{
    public class SupplierDataServcies : IDataServcies
    {
        private readonly SupplierRepository _repository;

        public SupplierDataServcies(SupplierRepository repository)
        {
            _repository = repository;
        }

        public Task<int> AddSupplier(Supplier value)
        {
            return _repository.Add(value);
        }

        public Task<int> DeleteSupplier(Supplier value)
        {
            return (_repository.Delete(value));
        }

        public async Task<List<Supplier>> GetSuppliers()
        {
            return await _repository.Get();
        }

        public async Task<Supplier> GetSupplierAsync(int id)
        {
            return await _repository.GetAsync(id);
        }

        public async Task<Supplier> GetSupplierAsync(Supplier value)
        {
            return await _repository.GetAsync(value);
        }

        public async Task<int> UpdateSupplier(Supplier value)
        {
            return (await _repository.Update(value));
        }
        public async Task<int> SaveChanges()
        {
            return await _repository.SaveChanges();
        }
    }
}
