using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OzonRepositories.Context;
using System.Data;

namespace OzonRepositories.Data
{
    public class DuplicateOrdersRepository : MainRepository
    {
        public DuplicateOrdersRepository(OzonOrderContext orderContext) : base(orderContext)
        {
        }
        public (int?, int?) DeleteDuplicateOrders()
        {
            // Параметры для храненной процедуры
            var duplicateCountParam = new SqlParameter("@DuplicateCount", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            var deletedRowsCountParam = new SqlParameter("@DeletedRowsCount", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };

            try
            {
                // Вызов хранимой процедуры
                _context.Database.ExecuteSqlRaw("DeleteDuplicateOrders @DuplicateCount OUTPUT, @DeletedRowsCount OUTPUT",
                    duplicateCountParam, deletedRowsCountParam);

                // Получение значений выходных параметров
                int? duplicateCount = duplicateCountParam.Value != DBNull.Value ? (int)duplicateCountParam.Value : 0;
                int? deletedRowsCount = deletedRowsCountParam.Value != DBNull.Value ? (int)deletedRowsCountParam.Value : 0;

                return (duplicateCount, deletedRowsCount);
            }
            catch (Exception ex)
            {
                return (-1, -1);
            }
        }
    }
}
