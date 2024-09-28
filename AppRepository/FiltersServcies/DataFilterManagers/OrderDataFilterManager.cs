using OServcies.FiltersServcies.FilterModels;
using OzonDomains.Models;
using Servcies.DataServcies;
using Services.CacheServcies.Cache.OzonOrdersCache;
using System.Reflection;
using System.Text.RegularExpressions;


namespace Servcies.FiltersServcies.DataFilterManagers
{
    public class OrderDataFilterManager : IFilterManagerAsync<Order, OrderFilterModel>
    {
        private QueryableDataFilter<Order> _filter;
        private DataFilter<Order> _forTransactionOrdersFilter;
        private readonly OrdersDataServcies _ordersDataServcies;
        private readonly OrderCache _orderCache;
        public OrderDataFilterManager(QueryableDataFilter<Order> dataFilter,
                                     DataFilter<Order> forTransactionOrdersFilter,
                                     OrdersDataServcies ordersDataServcies,
                                     OrderCache orderCache)
        {
            _filter = dataFilter;
            _forTransactionOrdersFilter = forTransactionOrdersFilter;
            _ordersDataServcies = ordersDataServcies;
            _orderCache = orderCache;
        }
        public async Task<List<Order>> FilterByRadioButton(List<Order> orders, string buttonState)
        {
            if (buttonState == "returnable")
            {
                orders = orders.Where(d => d.IsReturnable == true).ToList();
            }
            if (buttonState == "activated")
            {
                orders = orders.Where(d => d.IsVerified == true).ToList();
            }
            else if (buttonState == "notActivated")
            {
                orders = orders.Where(d => d.IsVerified == false).ToList();
            }
            else if (buttonState == "changed")
            {
                orders = orders.Where(d => d.UpdatedColumns != null && d.UpdatedColumns.Count() != 0).ToList();
            }
            else if (buttonState == "accepted")
            {
                orders = orders.Where(d => d.IsAccepted == true).ToList();
            }
            else if (buttonState == "all")
            {

            }
            return orders;
        }

        public async Task<List<Order>> FilterByFilterData(List<Order> standartOrders, OrderFilterModel filterData)
        {
            bool filterIsNull = AreAllPropertiesNull(filterData);

            if (filterIsNull)
            {
                return standartOrders.OrderBy(pr => pr.ProcessingDate).ToList();
            }

            IQueryable<Order> orders = await _ordersDataServcies.GetOrders();
            bool exactMatch = true;

            orders = _filter.FilterByString(orders, pr => pr.Key, filterData.Key);
            orders = _filter.FilterByString(orders, pr => pr.ShipmentNumber, filterData.ShipmentNumber);
            orders = _filter.FilterByString(orders, pr => pr.ShipmentWarehouse != null ? pr.ShipmentWarehouse.Name : "", filterData.ShipmentWarehouse, exactMatch);
            orders = _filter.FilterByString(orders, pr => pr.Status, filterData.Status, exactMatch);
            orders = _filter.FilterByString(orders, pr => pr.AppStatus != null ? pr.AppStatus.Name : "", filterData.AppStatus, exactMatch);
            orders = _filter.FilterByString(orders, pr => pr.ProductName, filterData.ProductName);
            orders = _filter.FilterByString(orders, pr => pr.ProductKey, filterData.Article);
            orders = _filter.FilterByString(orders, pr => pr.Supplier != null ? pr.Supplier.Name : "", filterData.Supplier, exactMatch);
            orders = _filter.FilterByString(orders, pr => pr.ProductInfo != null ? pr.ProductInfo.CommercialCategory : "", filterData.CommercialCategory, exactMatch);
            orders = _filter.FilterByString(orders, pr => pr.NewCategory, filterData.NewCategory);
            orders = _filter.FilterByString(orders, pr => pr.Сurrency != null ? pr.Сurrency.Name : "", filterData.Сurrency, exactMatch);
            orders = _filter.FilterByString(orders, pr => pr.DeliveryCity, filterData.DeliveryCity);
            orders = _filter.FilterByString(orders, pr => pr.OrderNumberToSupplier, filterData.OrderNumberToSupplier);
            orders = _filter.FilterByString(orders, pr => pr.Manufacturer != null ? pr.Manufacturer.Name : "", filterData.Manufacturer, exactMatch);
            orders = _filter.FilterByString(orders, pr => pr.OzonClient != null ? pr.OzonClient.Name : "", filterData.OzonClient);

            orders = _filter.FilterByDate(orders, pr => pr.ProcessingDate, filterData.ProcessingDate);
            orders = _filter.FilterByDate(orders, pr => pr.ShippingDate, filterData.ShippingDate);

            orders = _filter.FilterByDecimal(orders, pr => pr.ProductInfo.CurrentPriceWithDiscount, filterData.CurrentPriceWithDiscount);
            orders = _filter.FilterByDecimal(orders, pr => pr.Quantity, filterData.Quantity, tolerance: 0);
            orders = _filter.FilterByDecimal(orders, pr => pr.ShipmentAmount, filterData.ShipmentAmount);
            orders = _filter.FilterByDecimal(orders, pr => pr.Price, filterData.Price);
            orders = _filter.FilterByDecimal(orders, pr => pr.PurchasePrice, filterData.PurchasePrice);
            orders = _filter.FilterByDecimal(orders, pr => pr.MinOzonCommission, filterData.OzonCommission);
            orders = _filter.FilterByDecimal(orders, pr => pr.MinProfit, filterData.Profit);
            orders = _filter.FilterByDecimal(orders, pr => pr.MinDiscount, filterData.Discount);
            orders = _filter.FilterByDecimal(orders, pr => pr.CostPrice, filterData.CostPrice);

            orders = _filter.FilterByDouble(orders, pr => pr.ProductInfo.VolumetricWeight, filterData.VolumetricWeight);


            List<Order> result = _forTransactionOrdersFilter.FilterByInt(orders.ToList(), pr => pr.TimeLeftDay, filterData.TimeLeftDay).ToList(); ;

            if (filterData.DeliveryPeriod != null)
            {
                var filterByDay = result
                .AsEnumerable()
                .Where(o => o.DeliveryPeriod.HasValue &&
                            o.DeliveryPeriod.Value.Days == StringToIntDay(filterData.DeliveryPeriod));

                result = filterByDay
                    .Where(o => o.DeliveryPeriod.HasValue &&
                                o.DeliveryPeriod.Value.Hours == StringToIntHours(filterData.DeliveryPeriod))
                    .ToList();
            }

            

            return result.OrderBy(o => o.ProcessingDate).ToList();
        }

        public async Task<List<Order>> FilterTransactionsOrdersByFilterData(List<Order> orders, OrderFilterModel filterData)
        {
            bool filterIsNull = AreAllPropertiesNull(filterData);
            bool exactMatch = true;

            if (filterIsNull)
            {
                return orders.OrderBy(pr => pr.ProcessingDate).ToList();
            }

            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.Key, filterData.Key).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.ShipmentNumber, filterData.ShipmentNumber).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.ShipmentWarehouse?.Name ?? string.Empty, filterData.ShipmentWarehouse, exactMatch).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.Status, filterData.Status, exactMatch).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.AppStatus?.Name ?? string.Empty, filterData.AppStatus, exactMatch).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.ProductName, filterData.ProductName).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.ProductKey, filterData.Article).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.Supplier?.Name ?? string.Empty, filterData.Supplier, exactMatch).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.ProductInfo?.CommercialCategory ?? string.Empty, filterData.CommercialCategory, exactMatch).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.NewCategory, filterData.NewCategory).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.Сurrency?.Name ?? string.Empty, filterData.Сurrency, exactMatch).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.DeliveryCity, filterData.DeliveryCity).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.OrderNumberToSupplier, filterData.OrderNumberToSupplier).ToList();
            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.Manufacturer?.Name ?? string.Empty, filterData.Manufacturer, exactMatch).ToList();

            orders = _forTransactionOrdersFilter.FilterByString(orders, pr => pr.OzonClient?.Name ?? string.Empty, filterData.OzonClient).ToList();


            orders = _forTransactionOrdersFilter.FilterByDate(orders, pr => pr.ProcessingDate, filterData.ProcessingDate).ToList();
            orders = _forTransactionOrdersFilter.FilterByDate(orders, pr => pr.ShippingDate, filterData.ShippingDate).ToList();

            orders = _forTransactionOrdersFilter.FilterByDecimal(orders, pr => pr.ProductInfo?.CurrentPriceWithDiscount ?? 0, filterData.CurrentPriceWithDiscount).ToList();
            orders = _forTransactionOrdersFilter.FilterByDecimal(orders, pr => pr.Quantity, filterData.Quantity, tolerance: 0).ToList();
            orders = _forTransactionOrdersFilter.FilterByDecimal(orders, pr => pr.ShipmentAmount, filterData.ShipmentAmount).ToList();
            orders = _forTransactionOrdersFilter.FilterByDecimal(orders, pr => pr.Price, filterData.Price).ToList();
            orders = _forTransactionOrdersFilter.FilterByDecimal(orders, pr => pr.PurchasePrice, filterData.PurchasePrice).ToList();
            orders = _forTransactionOrdersFilter.FilterByDecimal(orders, pr => pr.MinOzonCommission, filterData.OzonCommission).ToList();
            orders = _forTransactionOrdersFilter.FilterByDecimal(orders, pr => pr.MinProfit, filterData.Profit).ToList();
            orders = _forTransactionOrdersFilter.FilterByDecimal(orders, pr => pr.MinDiscount, filterData.Discount).ToList();
            orders = _forTransactionOrdersFilter.FilterByDecimal(orders, pr => pr.CostPrice, filterData.CostPrice).ToList();

            orders = _forTransactionOrdersFilter.FilterByDouble(orders, pr => pr.ProductInfo?.VolumetricWeight ?? 0, filterData.VolumetricWeight).ToList();

            orders = _forTransactionOrdersFilter.FilterByInt(orders, pr => pr.TimeLeftDay, filterData.TimeLeftDay).ToList();    

            List<Order> result;

            if (filterData.DeliveryPeriod != null)
            {
                var filterByDay = orders
                .AsEnumerable()
                .Where(o => o.DeliveryPeriod.HasValue &&
                            o.DeliveryPeriod.Value.Days == StringToIntDay(filterData.DeliveryPeriod));

                result = filterByDay
                    .Where(o => o.DeliveryPeriod.HasValue &&
                                o.DeliveryPeriod.Value.Hours == StringToIntHours(filterData.DeliveryPeriod))
                    .ToList();
            }
            else
            {
                result = orders.OrderBy(o => o.ProcessingDate).ToList();
            }


            return result.OrderBy(o => o.ProcessingDate).ToList(); ;
        }

        public async Task<IEnumerable<Order>> FilterFromFile(bool filter)
        {
            IQueryable<Order> orders = await _ordersDataServcies.GetOrders();

            return orders.Where(o => o.FromFile == filter).OrderBy(pr => pr.ProcessingDate).ToList();
        }

        public bool AreAllPropertiesNull(OrderFilterModel model)
        {
            PropertyInfo[] properties = typeof(OrderFilterModel).GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                object? value = prop.GetValue(model);
                if (value != null)
                {
                    return false;
                }
            }

            return true;
        }

        private int? StringToIntDay(string deliveryPeriod)
        {
            if (deliveryPeriod != null)
            {
                MatchCollection matches = Regex.Matches(deliveryPeriod, @"\d+");

                if (matches.Count == 2)
                {
                    return int.Parse(matches[0].Value); ;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        private int? StringToIntHours(string deliveryPeriod)
        {
            if (deliveryPeriod != null)
            {
                MatchCollection matches = Regex.Matches(deliveryPeriod, @"\d+");

                if (matches.Count == 2)
                {
                    return int.Parse(matches[1].Value);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
