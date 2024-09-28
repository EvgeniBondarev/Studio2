using Azure;
using Microsoft.Extensions.Caching.Memory;
using OzonDomains.Models;
using Servcies.DataServcies;


namespace Services.CacheServcies.Cache.OzonOrdersCache
{
    public class OrderCache : AppCache<Order>, IAppCache<Order>
    {
        private readonly IMemoryCache _cache;
        private readonly OrdersDataServcies _ordersService;
        private const string CacheKey = "Orders";
        private const int InitialLoadCount = 500;
        private const int IncrementCount = 100;
        private int _maxPage = 1;
        private int _maxItemsToLoad = InitialLoadCount;

        public OrderCache(IMemoryCache memoryCache, OrdersDataServcies ordersDataServcies) : base(memoryCache)
        {
            _cache = memoryCache;
            _ordersService = ordersDataServcies;
        }

        public async Task<List<Order>> Get(int page)
        {
            if (!_cache.TryGetValue(CacheKey, out List<Order> cachedOrders))
            {
                // Загружаем первую тысячу записей
                cachedOrders = (await _ordersService.GetOrders(0, InitialLoadCount)).ToList();
                _maxPage = 10; // Первая тысяча записей = 10 страниц по 100 записей

                _cache.Set(CacheKey, cachedOrders, _saveTime);
            }

            int requiredItemCount = page * IncrementCount;
            if (cachedOrders.Count < requiredItemCount)
            {
                int itemsToLoad = requiredItemCount - cachedOrders.Count;
                List<Order> additionalOrders = (await _ordersService.GetOrders(cachedOrders.Count, itemsToLoad)).ToList();
                if (additionalOrders.Any())
                {
                    cachedOrders.AddRange(additionalOrders);
                    _cache.Set(CacheKey, cachedOrders, _saveTime);
                }
                _maxPage = Math.Max(_maxPage, page);
                _maxItemsToLoad = itemsToLoad;
            }

            return cachedOrders.Take(requiredItemCount).OrderBy(o => o.ProcessingDate).ToList();
        }


        public Task<List<Order>> Get()
        {
            // Устанавливаем ключ для кэша
            string cacheKey = CacheKey;

            // Пытаемся получить данные из кэша
            if (_cache.TryGetValue(cacheKey, out List<Order> orders))
            {
                // Если данные есть в кэше, сортируем их и возвращаем
                orders = orders.OrderBy(o => o.ProcessingDate).ToList();
                return Task.FromResult(orders);
            }

            // Если данных в кэше нет, выбрасываем исключение
            throw new NotImplementedException("Данные не найдены в кэше");
        }

        public async Task UpdateCacheIncrementally(int orderId)
        {
            // Получаем заказ из сервиса по его ID
            Order updatedOrder = await _ordersService.GetOrder(orderId);

            if (updatedOrder != null)
            {
                // Устанавливаем ключ для кэша
                string cacheKey = CacheKey;

                // Получаем текущие данные из кэша
                if (_cache.TryGetValue(cacheKey, out List<Order> cachedOrders))
                {
                    // Проверяем, есть ли обновленный заказ в кэше по его ID
                    var existingOrderIndex = cachedOrders.FindIndex(o => o.Id == orderId);

                    if (existingOrderIndex != -1)
                    {
                        // Если заказ найден, заменяем его на обновленный
                        cachedOrders[existingOrderIndex] = updatedOrder;
                    }
                    else
                    {
                        // Если заказ не найден, добавляем его в кэш
                        cachedOrders.Add(updatedOrder);
                    }

                    // Обновляем кэш для списка заказов
                    _cache.Set(cacheKey, cachedOrders, _saveTime); // Например, кэшируем на 10 минут
                }
            }
        }

        public async Task UpdateCacheIncrementally(List<Order> ordes)
        {
            foreach (var order in ordes)
            {
                if (order != null)
                {
                    // Устанавливаем ключ для кэша
                    string cacheKey = CacheKey;

                    // Получаем текущие данные из кэша
                    if (_cache.TryGetValue(cacheKey, out List<Order> cachedOrders))
                    {
                        // Проверяем, есть ли обновленный заказ в кэше по его ID
                        var existingOrderIndex = cachedOrders.FindIndex(o => o.Id == order.Id);

                        if (existingOrderIndex != -1)
                        {
                            // Если заказ найден, заменяем его на обновленный
                            cachedOrders[existingOrderIndex] = order;
                        }
                        else
                        {
                            // Если заказ не найден, добавляем его в кэш
                            cachedOrders.Add(order);
                        }

                        // Обновляем кэш для списка заказов
                        _cache.Set(cacheKey, cachedOrders, _saveTime); // Например, кэшируем на 10 минут
                    }
                }
            }
        }


        public async Task<List<Order>> Set()
        {
            string cacheKey = CacheKey;

            List<Order> newOrders = (await _ordersService.GetOrders()).ToList();

            _cache.Set(cacheKey, newOrders, _saveTime);

            return newOrders;
        }

        public async Task Update()
        {
            List<Order> cachedOrders = (await _ordersService.GetOrders(0, _maxItemsToLoad)).ToList();

            _cache.Set(CacheKey, cachedOrders, _saveTime);
        }

        public async Task RemoveOrderFromCache(int orderId)
        {
            // Устанавливаем ключ для кэша
            string cacheKey = CacheKey;

            // Получаем текущие данные из кэша
            if (_cache.TryGetValue(cacheKey, out List<Order> cachedOrders))
            {
                // Находим индекс заказа в кэше по его ID
                var existingOrderIndex = cachedOrders.FindIndex(o => o.Id == orderId);

                if (existingOrderIndex != -1)
                {
                    // Если заказ найден, удаляем его из кэша
                    cachedOrders.RemoveAt(existingOrderIndex);

                    // Обновляем кэш для списка заказов
                    _cache.Set(cacheKey, cachedOrders, _saveTime); // Например, кэшируем на 10 минут
                }
            }
        }
        public async Task RemoveOrdersFromCache(int[] orderIds)
        {
            // Устанавливаем ключ для кэша
            string cacheKey = CacheKey;

            // Получаем текущие данные из кэша
            if (_cache.TryGetValue(cacheKey, out List<Order> cachedOrders))
            {
                foreach (int orderId in orderIds)
                {
                    // Находим индекс заказа в кэше по его ID
                    var existingOrderIndex = cachedOrders.FindIndex(o => o.Id == orderId);

                    if (existingOrderIndex != -1)
                    {
                        // Если заказ найден, удаляем его из кэша
                        cachedOrders.RemoveAt(existingOrderIndex);

                        // Обновляем кэш для списка заказов
                        _cache.Set(cacheKey, cachedOrders, _saveTime); // Например, кэшируем на 10 минут
                    }
                }
            }
        }

        public async Task RemoveOrderFromCache(List<Order> ordes)
        {
            foreach (var order in ordes)
            {
                if (order != null)
                {
                    // Устанавливаем ключ для кэша
                    string cacheKey = CacheKey;

                    // Получаем текущие данные из кэша
                    if (_cache.TryGetValue(cacheKey, out List<Order> cachedOrders))
                    {
                        // Проверяем, есть ли обновленный заказ в кэше по его ID
                        var existingOrderIndex = cachedOrders.FindIndex(o => o.Id == order.Id);

                        if (existingOrderIndex != -1)
                        {
                            // Если заказ найден, удаляем его из кэша
                            cachedOrders.RemoveAt(existingOrderIndex);

                            // Обновляем кэш для списка заказов
                            _cache.Set(cacheKey, cachedOrders, _saveTime); // Например, кэшируем на 10 минут
                        }
                    }
                }
            }
        }

    }
}
