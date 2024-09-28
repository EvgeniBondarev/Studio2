using Microsoft.EntityFrameworkCore;
using OzonDomains;
using OzonDomains.Models;
using Servcies.DataServcies;
using Servcies.FiltersServcies.FilterModels;
using System.Reflection;


namespace Servcies.FiltersServcies.DataFilterManagers
{
    public class TransactionDataFilterManager : IFilterManagerAsync<Transaction, TransactionFilterModel>
    {
        private QueryableDataFilter<Transaction> _filter;
        private TransactionDataServcies _servcies;
        public TransactionDataFilterManager(QueryableDataFilter<Transaction> filter,
                                            TransactionDataServcies transactionDataServcies)
        {
            _filter = filter;
            _servcies = transactionDataServcies;
        }
        public async Task<List<Transaction>> FilterByFilterData(List<Transaction> standartTransactions, TransactionFilterModel filterModel)
        {
            bool filterIsNull = AreAllPropertiesNull(filterModel);

            if (filterIsNull)
            {
                return standartTransactions;
            }

            IQueryable<Transaction> transactions = await _servcies.GetTransactions();


            transactions = _filter.FilterByString(transactions, tr => tr.CreateBy, filterModel.CreateBy);
            transactions = _filter.FilterByDate(transactions, tr => tr.CreatedDateTime, filterModel.CreatedDateTime);
            transactions = _filter.FilterByEnum(transactions, t => t.Type, filterModel.Type);

            return await transactions.ToListAsync();
        }

        public async Task<List<Transaction>> FilterByRadioButton(List<Transaction> filterModel, string buttonState)
        {
            throw new NotImplementedException();
        }

        public bool AreAllPropertiesNull(TransactionFilterModel model)
        {
            PropertyInfo[] properties = typeof(TransactionFilterModel).GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                if (prop.Name == nameof(TransactionFilterModel.Type))
                {
                    if (model.Type.HasValue && model.Type != TransactionType.All)
                    {
                        return false;
                    }
                }
                else if (prop.Name == nameof(TransactionFilterModel.CreateBy))
                {
                    if (!string.IsNullOrEmpty(model.CreateBy) && model.CreateBy != "Все")
                    {
                        return false;
                    }
                }
                else if (prop.Name == nameof(TransactionFilterModel.CreatedDateTime))
                {
                    if (model.CreatedDateTime.HasValue)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

    }
}
