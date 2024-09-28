using OzonDomains.Models;
using OzonRepositories.Context;
using OzonRepositories.Data;
using Servcies.DataServcies.DTO;
using Servcies.PriceСlculationServcies;
using System.Collections.Generic;
using System.Reflection;

namespace Servcies.DataServcies
{
    public class OrdersDataServcies : IDataServcies
    {
        private readonly string[] ignoredProperties = { "AppStatus",  "СurrencyId", "ProductInfoId", "UpdatedColumns", "Id", "AppStatusId", "ShipmentWarehouseId",
                                                        "IsVerified", "Comment", "Key", "IsAccepted", "MaxCommissionInfo", "MinCommissionInfo", "MinDiscount",
                                                        "MaxDiscount", "PurchasePrice","Supplier", "SupplierId", "MinProfit", "MaxProfit", "UpdatedBy", "IsReturnable",
                                                        "OrderNumberToSupplier", "FromFile", "OriginalPurchasePrice"};

        private readonly string[] ignoredWebProperties = {"СurrencyId", "ProductInfoId", "UpdatedColumns", "Id", "ShipmentWarehouseId",
                                                        "ShipmentWarehouse", "Сurrency", "ProductInfo", "MaxCommissionInfo", "MinCommissionInfo",
                                                        "IsVerified", "Key", "IsAccepted", "OzonClient", "FromFile"};




        private readonly OrderRepository _repository;
        private readonly OrderHistoryDataServcies _historyDataServcies;
        private readonly OzonOrderContext _context;
        private readonly OrderPriceManager _orderPriceManager;
        private readonly AppStatusDataServcies _appStatusDataServcies;
        private readonly SupplierDataServcies _supplierDataServcies;
        private readonly ProductsDataServcies _productsDataServcies;
        private readonly WarehouseDataServcies _warehouseDataServcies;
     
        public OrdersDataServcies(OrderRepository repository,
                                 OzonOrderContext context,
                                 OrderHistoryDataServcies historyDataServcies,
                                 OrderPriceManager orderPriceManager,
                                 AppStatusDataServcies appStatusDataServcies,
                                 SupplierDataServcies supplierDataServcies,
                                 ProductsDataServcies productsDataServcies,

                                 WarehouseDataServcies warehouseDataServcies)
        {
            _repository = repository;
            _context = context;
            _historyDataServcies = historyDataServcies;
            _orderPriceManager = orderPriceManager;
            _appStatusDataServcies = appStatusDataServcies;
            _supplierDataServcies = supplierDataServcies;
            _productsDataServcies = productsDataServcies;
            _warehouseDataServcies = warehouseDataServcies;
        }

        public async Task<IQueryable<Order>> GetOrders()
        {
            var result = await _repository.Get();

            return result;
        }

        public async Task<Order> GetOrder(int id)
        {
            var result = await _repository.GetAsync(id);

            return result;
        }

        public async Task<Order> GetOrder(Order order)
        {
            var result = await _repository.GetAsync(order);

            return result;
        }

        public async Task<int> UpdateOrder(Order order)
        {
            return await _repository.Update(order);
        }

        public async Task<IQueryable<Order>> GetOrders(int skip, int take)
        {
            return await _repository.GetOrdersWithPagination(skip, take);
        }

        public async Task<int> GetTotalOrderCount()
        {
            return await _repository.GetTotalOrderCount();
        }

        public async Task<int> GetReturnableCount()
        {
            var orders = await _repository.Get();
            return orders.Where(o => o.IsReturnable == true).Count();
        }

        public async Task<IQueryable<Order>> GetOrdersByManufacturerCode(string code)
        {
            return await _repository.GetOrdersByManufacturerCode(code);
        }

        public async Task<int> DeleteOrder(int orderId)
        {
            Order orderToDelete = await _repository.GetAsync(orderId);

            if(orderToDelete != null && orderToDelete.AppStatus.Name == "Не указан")
            {
                return await _repository.Delete(orderToDelete);
            }
            return 0;
        }

        public async Task<Dictionary<string, int>> GetUniqueArticles()
        {
            var result = await _repository.GetUniqueValues(o => o.ProductKey);
            return result.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public async Task<Dictionary<string, int>> GetUniqueDeliveryCities()
        {
            var result = await _repository.GetUniqueValues(o => o.DeliveryCity);
            return result.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public async Task<Dictionary<string, int>> GetUniqueShipmentNumbers()
        {
            var shipmentNumbers = await _repository.GetShipmentNumbers();
            var result = shipmentNumbers
                .Select(sn => sn.Split('-')[0])
                .GroupBy(sn => sn)
                .ToDictionary(g => g.Key, g => g.Count());

            return result;
        }

        public async Task<int> MultiplayDeleteOrders(int[] idArray)
        {
            int result = 0;
            
            foreach(int orderId in idArray)
            {
                Order orderToDelete = await _repository.GetAsync(orderId);

                if (orderToDelete != null && orderToDelete.AppStatus.Name == "Не указан")
                {
                    result += await _repository.Delete(orderToDelete);
                }
            }

            return result;
        }

        public async Task<int[]> AddOrders(List<Order> orders)
        {

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                var chunkSize = 1500;
                var updateCount = 0;
                var addCount = 0;
                var ordersToInsert = new List<Order>();

                for (int i = 0; i < orders.Count; i += chunkSize)
                {
                    var currentChunk = orders.Skip(i).Take(chunkSize).ToList();
                    var existingArticles = new HashSet<string>(currentChunk.Select(p => p.ShipmentNumber + p.ProductKey));
                    List<Order> existingOrdersChunk = (await GetOrders()).ToList();
                    existingOrdersChunk = existingOrdersChunk.Where(o => existingArticles.Contains(o.ShipmentNumber + o.ProductKey)).ToList();

                    foreach (var order in currentChunk)
                    {
                        var existingOrder = _context.Orders.Where(o => o.Key == order.Key).FirstOrDefault();

                        if (existingOrder != null)
                        {
                            UpdateOrder(existingOrder, order);

                            updateCount++;
                        }
                        else
                        {

                            ordersToInsert.Add(await CastToModel(order));
                            addCount++;
                        }
                    }
                    await _context.Orders.AddRangeAsync(ordersToInsert);
                    await _context.SaveChangesAsync();
                    //_context.ChangeTracker.Clear();
                    ordersToInsert.Clear();
                }

                await transaction.CommitAsync();

                return [addCount, updateCount];
            }

        }

        public async Task<bool> EditOrder(Order order)
        {
            try
            {
                var complitOrder = await CastToModel(order);
                var existingOrder = await GetOrder(complitOrder.Id);
                await UpdateOrderWeb(existingOrder, complitOrder);
                existingOrder.IsVerified = true;

                await SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public async Task<int> MultiplayEditOrder(IEnumerable<Order> orders)
        {
            foreach(Order order in orders)
            {
                try
                {
                    Order complitOrder = await CastToModel(order);
                    Order existingOrder = await GetOrder(complitOrder.Id);
                    await UpdateOrderWeb(existingOrder, complitOrder);
                    existingOrder.IsVerified = true;
                }
                catch (Exception ex)
                {
                    throw new Exception(message: $"Не удалось изменить заказ {order.ShipmentNumber}");
                }
            }
            return await SaveChanges();
        }    
        public async Task<Order> TransactOrder(Order order)
        {
            var complitOrder = await CastToModel(order);
            var existingOrder = await GetOrder(complitOrder.Id);
            await UpdateOrderWeb(existingOrder, complitOrder);
            existingOrder.IsVerified = true;

            await SaveChanges();
            return existingOrder;
        }
        private async Task UpdateOrderWeb(Order existingOrder, Order jsonOrder)
        {
            var isVerified = existingOrder.IsVerified;
            var isIsAccepted = existingOrder.IsAccepted;

            var orderChangeList = GetOrderChangeList(existingOrder, jsonOrder, ignoredWebProperties);

            if (orderChangeList.Count() > 0)
            {
                existingOrder.UpdatedColumns = orderChangeList;
            }

            if (existingOrder.UpdatedColumns != null)
            {
                var properties = typeof(Order).GetProperties();

                foreach (var prop in properties)
                {
                    try
                    {
                        var jsonOrderProp = jsonOrder.GetType().GetProperty(prop.Name);

                        if (jsonOrderProp != null)
                        {
                            var o = jsonOrderProp.GetValue(jsonOrder);

                            if (o != null && prop.Name != "Id" && existingOrder.UpdatedColumns.Contains(prop.Name))
                            {
                                var existingOrderProp = existingOrder.GetType().GetProperty(prop.Name);
                                if (existingOrderProp != null && existingOrderProp.CanWrite) // Check if the property has a setter
                                {
                                    existingOrderProp.SetValue(existingOrder, o);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log or handle the exception as needed
                        Console.WriteLine($"Error setting property {prop.Name}: {ex.Message}");
                    }
                }
            }

            // Дополнительная проверка для IsAccepted
            if (existingOrder.UpdatedColumns != null && !existingOrder.UpdatedColumns.Contains("IsAccepted"))
            {
                existingOrder.IsAccepted = false;
            }
            else
            {
                existingOrder.IsAccepted = isIsAccepted;
            }

            existingOrder.IsVerified = isVerified;
        }


        private async Task UpdateOrder(Order existingOrder, Order jsonOrder)
        {
            var isVerified = existingOrder.IsVerified;
            var isIsAccepted = existingOrder.IsAccepted;

            if (isVerified)
            {

            }

            var orderChangeList = GetOrderChangeList(existingOrder, jsonOrder, ignoredProperties);

            if (orderChangeList.Count() > 0)
            {
                existingOrder.UpdatedColumns = orderChangeList;
            }
            else
            {
                existingOrder.UpdatedColumns = null;
            }

            if (existingOrder.UpdatedColumns != null)
            {
                var properties = typeof(Order).GetProperties();
                var jsonOrderType = jsonOrder.GetType();

                foreach (var prop in properties)
                {
                    try
                    {
                        var jsonOrderProp = jsonOrderType.GetProperty(prop.Name);

                        if (jsonOrderProp != null)
                        {
                            var o = jsonOrderProp.GetValue(jsonOrder);

                            if (o != null && prop.Name != "Id" && existingOrder.UpdatedColumns.Contains(prop.Name))
                            {
                                var existingOrderProp = existingOrder.GetType().GetProperty(prop.Name);
                                if (existingOrderProp != null && existingOrderProp.CanWrite) // Check if the property has a setter
                                {
                                    existingOrderProp.SetValue(existingOrder, o);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log or handle the exception as needed
                        Console.WriteLine($"Error setting property {prop.Name}: {ex.Message}");
                    }
                }

            }

            if (existingOrder.UpdatedColumns != null && existingOrder.UpdatedColumns[0] != "IsAccepted")
            {
                existingOrder.IsAccepted = false;
            }
            else
            {
                existingOrder.IsAccepted = isIsAccepted;
            }

            existingOrder.IsVerified = isVerified;

            existingOrder = await CastToModel(existingOrder);

        }

        public List<string> GetOrderChangeList(Order existingOrder, Order jsonOrder, string[] ignoredProperties)
        {
            List<string> changeProp = new List<string>();

            changeProp = CompareObjects(existingOrder, jsonOrder, changeProp, ignoredProperties);

            return changeProp;
        }

        private List<string> CompareObjects(object obj1, object obj2, List<string> changeProp, string[] ignoredProperties)
        {
            Type type = obj1.GetType();

            foreach (PropertyInfo property in type.GetProperties())
            {
                if (property.CanRead)
                {
                    if (ignoredProperties.Contains(property.Name))
                    {
                        continue;
                    }

                    object val1 = property.GetValue(obj1) == "" ? null : property.GetValue(obj1);
                    object val2 = property.GetValue(obj2) == "" ? null : property.GetValue(obj2);

                    if (val1 != null && val2 != null)
                    {
                        if (property.PropertyType.Assembly == type.Assembly && !property.PropertyType.IsPrimitive && property.PropertyType != typeof(string))
                        {
                            CompareObjects(val1, val2, changeProp, ignoredProperties);
                        }
                        else if (!val1.Equals(val2))
                        {
                            changeProp.Add(property.Name);
                        }
                    }
                    else if (val1 != null || val2 != null)
                    {
                        changeProp.Add(property.Name);
                    }
                }
            }
            return changeProp;
        }


        public async Task ConfirmAccepted(int id)
        {
            var order = await _repository.GetAsync(id);

            order.IsAccepted = true;
            order.IsVerified = true;
            order.UpdatedColumns = null;

            await SaveChanges();
        }

        public async Task RejectAccepted(int id)
        {
            var order = await _repository.GetAsync(id);

            order.IsAccepted = false;

            _context.SaveChanges();
        }

        private async Task<Order> CastToModel(Order order)
        {
            if (order.AppStatus == null && order.AppStatusId != null)
            {
                order.AppStatus = await _appStatusDataServcies.GetAppStatusAsync(order.AppStatusId.Value);
                order.Supplier = await _supplierDataServcies.GetSupplierAsync(order.SupplierId.Value);

                decimal? weight = order.ProductInfo.Weight;
                order.ProductInfo = await _productsDataServcies.GetProductAsync(order.ProductInfoId.Value);
                order.ProductInfo.Weight = weight;
                await _productsDataServcies.Update(order.ProductInfo);

                order = await _orderPriceManager.SetPurchasePriceToRUB(order);

                order = await _orderPriceManager.CalculateCostPrice(order);

                order = await _orderPriceManager.CanculateProfit(order);
                order = await _orderPriceManager.CalculateDiscount(order);
            }
            order = await SetIsReturnable(order);

            return order;
        }

        private async Task<Order> SetIsReturnable(Order order)
        {
            if (order.AppStatus != null)
            {
                if (order.AppStatus.Name == "Заказан поставщику" && order.Status == "Отменён")
                {
                    order.IsReturnable = true;
                }
                else
                {
                    order.IsReturnable = false;
                }
            }
            return order;
        }

        public async Task<List<Order>> CalculateCostPriceForNotFullOrders(List<Order> orders)
        {
            List<Order> result = new List<Order>();
            foreach (var order in orders)
            {
                var reformedOrder = await _orderPriceManager.SetPurchasePriceToRUB(order);
                result.Add(await _orderPriceManager.CalculateCostPrice(reformedOrder));
            }
            return result;
        }

        public async Task<NotFullOrdersModel> GetNotFullOrdersModel(List<Order> orders)
        {
            IQueryable<Order> appOrders = await _repository.Get();

            // Коллекция для отслеживания использованных заказов
            HashSet<Order> usedOrders = new HashSet<Order>();

            // Найдем уникальные заказы
            List<Order> UniqueOrders = orders
                .GroupJoin(appOrders,
                           order => order.ProductKey.Split('=')[0], // Извлекаем ключ до '='
                           repoOrder => repoOrder.ProductKey.Split('=')[0], // Извлекаем ключ до '='
                           (order, repoOrders) => new { Order = order, Matches = repoOrders.ToList() })
                .Where(or => !or.Matches.Any() // Заказ не имеет совпадений
                             || or.Matches.All(repoOrder => repoOrder.Status == "Отменен" || repoOrder.Status == "Отменён")) // Все совпавшие заказы имеют статус "Отменен" или "Отменён"
                .Select(or => or.Order)
                .ToList();

            // Добавляем уникальные заказы в usedOrders, чтобы исключить их из дальнейшей обработки
            foreach (var order in UniqueOrders)
            {
                usedOrders.Add(order);
            }

            // Создаем словарь для заказов с несколькими совпадениями, исключая использованные заказы
            Dictionary<Order, List<Order>> OrdersWithMultipleMatches = orders
                .GroupJoin(appOrders,
                           order => order.ProductKey.Split('=')[0], // Извлекаем ключ до '='
                           repoOrder => repoOrder.ProductKey.Split('=')[0], // Извлекаем ключ до '='
                           (order, repoOrders) => new { Order = order, Matches = repoOrders.ToList() })
                .Where(x => x.Matches.Count > 1) // Учитываем только заказы с несколькими совпадениями
                .ToDictionary(
                    x => x.Order,
                    x => x.Matches
                        .Where(repoOrder =>
                            repoOrder.ProductKey.Split('=')[0] == x.Order.ProductKey.Split('=')[0] && // Проверяем совпадение ключей
                            !usedOrders.Contains(repoOrder) && // Исключаем уже использованные заказы
                            (repoOrder.Status != "Отменен" && repoOrder.Status != "Отменён" // Исключаем заказы со статусом "Отменен" или "Отменён"
                             || x.Order.ProductKey.Split('=')[0] == repoOrder.ProductKey.Split('=')[0])) // Если статус "Отменен", проверяем ключ
                        .ToList()
                );

            // Обновляем usedOrders после обработки OrdersWithMultipleMatches
            foreach (var match in OrdersWithMultipleMatches.Values.SelectMany(m => m))
            {
                usedOrders.Add(match);
            }

            // Создаем словарь для заказов с одним совпадением, исключая уже использованные заказы
            Dictionary<Order, Order> OrdersWithOneMatches = orders
                .GroupJoin(appOrders,
                           order => order.ProductKey.Split('=')[0], // Извлекаем ключ до '='
                           repoOrder => repoOrder.ProductKey.Split('=')[0], // Извлекаем ключ до '='
                           (order, repoOrders) => new { Order = order, Matches = repoOrders.ToList() })
                .Where(x => x.Matches.Count == 1) // Учитываем только заказы с одним совпадением
                .ToDictionary(
                    x => x.Order,
                    x => x.Matches
                        .Where(repoOrder =>
                            repoOrder.ProductKey.Split('=')[0] == x.Order.ProductKey.Split('=')[0] && // Проверяем совпадение ключей
                            !usedOrders.Contains(repoOrder) && // Исключаем уже использованные заказы
                            (repoOrder.Status != "Отменен" && repoOrder.Status != "Отменён" // Исключаем заказы со статусом "Отменен" или "Отменён"
                             || x.Order.ProductKey.Split('=')[0] == repoOrder.ProductKey.Split('=')[0])) // Если статус "Отменен", проверяем ключ
                        .FirstOrDefault()
                );

            // Добавляем в usedOrders заказы с одним совпадением
            foreach (var match in OrdersWithOneMatches.Values.Where(m => m != null))
            {
                usedOrders.Add(match);
            }

            List<Order> usedProductKeys = new List<Order>(OrdersWithMultipleMatches.Keys);
            List<Order> usedProductKeysOneMatches = new List<Order>(OrdersWithOneMatches.Keys);

            // Получение уникальных ProductKey из usedProductKeys и usedProductKeysOneMatches
            HashSet<string> usedKeys = new HashSet<string>(
                usedProductKeys.Select(o => o.ProductKey.Split('=')[0])
                .Concat(usedProductKeysOneMatches.Select(o => o.ProductKey.Split('=')[0]))
            );

            // Фильтрация UniqueOrders
            UniqueOrders = UniqueOrders
                .Where(o => !usedKeys.Contains(o.ProductKey.Split('=')[0]))
                .ToList();


            // Сортируем OrdersWithMultipleMatches по приоритету статуса "Заказан поставщику"
            string priorityStatus = "Заказан поставщику";
            OrdersWithMultipleMatches = OrdersWithMultipleMatches
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value
                        .OrderBy(order => order.AppStatus?.Name == priorityStatus ? 0 : 1)
                        .ThenBy(order => order.AppStatus?.Name)
                        .ToList()
                );

            return new NotFullOrdersModel()
            {
                UniqueOrders = UniqueOrders,
                OrdersWithMultipleMatches = OrdersWithMultipleMatches,
                OrdersWithOneMatches = OrdersWithOneMatches
            };
        }

        public async Task ProcessingNotFullOrder(Order notFullOrder, List<int> orderIds)
        {
            notFullOrder = await CastToFullOrder(notFullOrder, orderIds == null || orderIds.Count == 0);

            if(orderIds == null || orderIds.Count == 0)
            {
                notFullOrder =  await CastToModel(notFullOrder);
                await AddOrders([notFullOrder]);
            }
            else if(orderIds.Count > 0) 
            {
                orderIds = orderIds.Distinct().ToList();
                foreach (var id in orderIds)
                {
                    var orderToUpdate = await _repository.GetAsync(id); 
                    if(orderToUpdate != null)
                    {
                        orderToUpdate.AppStatus = notFullOrder.AppStatus;
                        orderToUpdate.PurchasePrice = notFullOrder.PurchasePrice;
                        orderToUpdate.CostPrice = notFullOrder.CostPrice;

                        orderToUpdate = await _orderPriceManager.CanculateProfit(orderToUpdate);
                        orderToUpdate = await _orderPriceManager.CalculateDiscount(orderToUpdate);

                        await AddOrders([orderToUpdate]);
                    }
                }

            }   
           
        }

        public async Task<Order> CastToFullOrder(Order notFullOrder, bool newOrder)
        {
            if(notFullOrder.AppStatus != null && notFullOrder.AppStatus.Id != null)
            {
                notFullOrder.AppStatus = await _appStatusDataServcies.GetAppStatusAsync(notFullOrder.AppStatus.Id);
            }
            if(notFullOrder.Manufacturer != null && notFullOrder.Manufacturer.Id != null)
            {
                notFullOrder.Manufacturer = _context.Manufacturers.Where(m => m.Id == notFullOrder.Manufacturer.Id).FirstOrDefault();
            }
            if(notFullOrder.ShipmentWarehouse != null && notFullOrder.ShipmentWarehouse.Id != null)
            {
                notFullOrder.ShipmentWarehouse = await _warehouseDataServcies.GetWarehouseAsync(notFullOrder.ShipmentWarehouse.Id);
            }
            if(notFullOrder.SupplierId  != 0)
            {
                notFullOrder.Supplier = await _supplierDataServcies.GetSupplierAsync(notFullOrder.SupplierId.Value);
            }
            if(notFullOrder.ProductInfo != null && notFullOrder.ProductInfo.Id != null)
            {
                notFullOrder.ProductInfo = await _productsDataServcies.GetProductAsync(notFullOrder.ProductInfo.Id);
            }
            if(notFullOrder.OzonClient != null && notFullOrder.OzonClient.Id != null)
            {
                notFullOrder.OzonClient = _context.OzonClients.Where(o => o.Id == notFullOrder.OzonClient.Id).FirstOrDefault();
            }

            if (newOrder)
            {
                notFullOrder = await _orderPriceManager.CanculateProfit(notFullOrder);
                notFullOrder = await _orderPriceManager.CalculateDiscount(notFullOrder);
            }
            return notFullOrder;
        }
        public async Task<List<Order>> SetNumberInExcel(List<Order> orders)
        {
            int numberInExcel = 2;
            foreach(Order order in orders) 
            { 
                order.NumberInExcel = numberInExcel;
                numberInExcel++;
            }
            return orders;
        }

        public async Task<int> SaveChanges()
        {
            return await _repository.SaveChanges();
        }

    }
}
