using OzonDomains.Models;
using OzonRepositories.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OzonRepositories.Data
{
    public class AppStatusRepository : MainRepository, IRepository<AppStatus>
    {
        public AppStatusRepository(OzonOrderContext orderContext) : base(orderContext)
        {
        }

        public Task<int> Add(AppStatus value)
        {
            _context.AppStatuses.AddAsync(value);
            return _context.SaveChangesAsync();
        }

        public Task<int> Delete(AppStatus value)
        {
            _context.Remove(value);
            return _context.SaveChangesAsync();
        }

        public async Task<List<AppStatus>> Get()
        {
            return _context.AppStatuses.ToList();
        }

        public async Task<AppStatus> GetAsync(int id)
        {
            return _context.AppStatuses.Find(id);
        }

        public async Task<AppStatus> GetAsync(AppStatus value)
        {
            return _context.AppStatuses.SingleOrDefault(a => a.Name == value.Name);
        }

        public async Task<int> Update(AppStatus value)
        {
            _context.AppStatuses.Update(value);
            return _context.SaveChanges();
        }
    }
}
