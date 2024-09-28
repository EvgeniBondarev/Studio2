using OzonRepositories.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.DataServcies
{
    public class DuplicateOrdersServcies : IDataServcies
    {
        private readonly DuplicateOrdersRepository _repository;

        public DuplicateOrdersServcies(DuplicateOrdersRepository repository)
        {
            _repository = repository;
        }

        public (int?, int?) DeleteDuplicateOrders()
        {
            return _repository.DeleteDuplicateOrders();
        }

        public Task<int> SaveChanges()
        {
            throw new NotImplementedException();
        }
    }
}
