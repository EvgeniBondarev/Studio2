using OzonDomains.Models;
using OzonRepositories.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OzonRepositories.Data
{
    public class UserAccessRepository : MainRepository, IRepository<UserAccess>
    {
        public UserAccessRepository(OzonOrderContext orderContext) : base(orderContext)
        {
        }

        public async Task<int> Add(UserAccess value)
        {
            _context.UserAccess.Add(value);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> Delete(UserAccess value)
        {
            _context.UserAccess.Remove(value);
            return await _context.SaveChangesAsync();
        }

        public async Task<List<UserAccess>> Get()
        {
            return _context.UserAccess.ToList();
        }

        public async Task<UserAccess> GetAsync(int id)
        {
            return _context.UserAccess.FirstOrDefault(u => u.Id == id);
        }

        public async Task<UserAccess> GetAsync(UserAccess value)
        {
            return _context.UserAccess.FirstOrDefault(u => u.Name == value.Name);
        }

        public async Task<int> Update(UserAccess value)
        {
            _context.UserAccess.Update(value);
            return await _context.SaveChangesAsync();
        }
    }

}
