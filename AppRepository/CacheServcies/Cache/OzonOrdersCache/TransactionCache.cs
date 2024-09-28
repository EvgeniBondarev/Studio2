using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OzonDomains.Models;
using Servcies.DataServcies;
using Services.CacheServcies.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Servcies.CacheServcies.Cache.OzonOrdersCache
{
    public class TransactionCache : AppCache<Transaction>, IAppCache<Transaction>
    {
        private readonly IMemoryCache _cache;
        private readonly TransactionDataServcies _transactionsService;
        private const string CacheKey = "Transactions";
        private const int InitialLoadCount = 100;
        private const int IncrementCount = 30;
        private int _maxPage = 1;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);
        private int _maxItemsToLoad = InitialLoadCount;

        public TransactionCache(IMemoryCache memoryCache, TransactionDataServcies transactionsService) : base(memoryCache)
        {
            _cache = memoryCache;
            _transactionsService = transactionsService;
        }

        public async Task<List<Transaction>> Get(int page)
        {
            if (!_cache.TryGetValue(CacheKey, out List<Transaction> cachedTransactions))
            {
                cachedTransactions = (await _transactionsService.GetTransactions(0, InitialLoadCount)).ToList();
                _maxPage = InitialLoadCount / IncrementCount; 

                _cache.Set(CacheKey, cachedTransactions, _cacheDuration);
            }

            int requiredItemCount = page * IncrementCount;
            if (cachedTransactions.Count < requiredItemCount)
            {
                int itemsToLoad = requiredItemCount - cachedTransactions.Count;
                List<Transaction> additionalTransactions = (await _transactionsService.GetTransactions(cachedTransactions.Count, itemsToLoad)).ToList();
                if (additionalTransactions.Any())
                {
                    cachedTransactions.AddRange(additionalTransactions);
                    _cache.Set(CacheKey, cachedTransactions, _cacheDuration);
                }
                _maxPage = Math.Max(_maxPage, page);
                _maxItemsToLoad = itemsToLoad;
            }

            return cachedTransactions.Take(requiredItemCount).ToList();
        }

        public async Task<List<Transaction>> Get()
        {
            string cacheKey = CacheKey;

            if (_cache.TryGetValue(cacheKey, out List<Transaction> transactions))
            {
                transactions = transactions.ToList();
                return await Task.FromResult(transactions);
            }

            throw new NotImplementedException("Данные не найдены в кэше");
        }

        public async Task<List<Transaction>> Set()
        {
            string cacheKey = CacheKey;

            List<Transaction> newTransactions = (await _transactionsService.GetTransactions()).ToList();

            _cache.Set(cacheKey, newTransactions, _cacheDuration);

            return newTransactions;
        }
        public async Task UpdateCacheIncrementally(int transactionId)
        {
            Transaction updatedTransaction = await _transactionsService.GetTransaction(transactionId);

            if (updatedTransaction != null)
            {
                string cacheKey = CacheKey;

                if (_cache.TryGetValue(cacheKey, out List<Transaction> cachedTransactions))
                {
                    var existingTransactionIndex = cachedTransactions.FindIndex(tr => tr.Id == transactionId);

                    if (existingTransactionIndex != -1)
                    {
                        cachedTransactions[existingTransactionIndex] = updatedTransaction;
                    }
                    else
                    {
                        cachedTransactions.Add(updatedTransaction);
                    }

                    _cache.Set(cacheKey, cachedTransactions, _saveTime);
                }
            }
        }

        public async Task Update()
        {
            List<Transaction> cachedOrders = (await _transactionsService.GetTransactions(0, _maxItemsToLoad)).ToList();

            _cache.Set(CacheKey, cachedOrders, _saveTime);
        }
    }
}
