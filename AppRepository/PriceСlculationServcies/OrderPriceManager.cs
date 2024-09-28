using OzonDomains.Models;
using Servcies.DataServcies;

namespace Servcies.PriceСlculationServcies
{
    public class OrderPriceManager
    {
        private readonly CurrencyRateFetcher _currencyRateFetcher;
        private readonly ProductsDataServcies _productsDataServcies;

        public OrderPriceManager(CurrencyRateFetcher currencyRateFetcher)
        {
            _currencyRateFetcher = currencyRateFetcher;
        }

        public async Task<Order> SetPurchasePriceToRUB(Order order)
        {
            if(order.Supplier != null)
            {
                switch (order.Supplier.CurrencyCode)
                {
                    case OzonDomains.CurrencyCode.USD:
                        order.OriginalPurchasePrice = order.PurchasePrice;
                        decimal rateUSD = await _currencyRateFetcher.GetUSDRateAsync();
                        order.PurchasePrice = order.PurchasePrice * order.Supplier.CostFactor * rateUSD;
                        break;

                    case OzonDomains.CurrencyCode.EUR:
                        order.OriginalPurchasePrice = order.PurchasePrice;
                        decimal rateEUR = await _currencyRateFetcher.GetEURRateAsync();
                        order.PurchasePrice = order.PurchasePrice * order.Supplier.CostFactor * rateEUR;
                        break;

                    case OzonDomains.CurrencyCode.BYN:
                        order.OriginalPurchasePrice = order.PurchasePrice;
                        decimal rateBYN = await _currencyRateFetcher.GetBYNRateAsync();
                        order.PurchasePrice = order.PurchasePrice * order.Supplier.CostFactor * rateBYN;
                        break;

                    case OzonDomains.CurrencyCode.RUB:
                        order.OriginalPurchasePrice = null;
                        break;
                }
            }
            return order;
        }

        public async Task<Order> CanculateProfit(Order order)
        {
            if (order.CostPrice != 0 && order.CostPrice != null && order.Price != null)
            {
                order.MaxProfit = null;
                order.MinProfit = null;
                decimal? startCommission = order.Price - order.CostPrice;

                if (order.MinOzonCommission != null)
                {
                    order.MaxProfit = startCommission - order.MinOzonCommission;
                }
                if (order.MaxOzonCommission != null)
                {
                    order.MinProfit = startCommission - order.MaxOzonCommission;
                }
            }

            return order;
        }

        public async Task<Order> CalculateDiscount(Order order)
        {
            order.MaxDiscount = null;
            order.MinDiscount = null;

            if (order.CostPrice != 0 && order.CostPrice != null)
            {
                if (order.MinProfit != null)
                {
                    order.MinDiscount = (order.MinProfit / order.CostPrice) * 100;
                }
                if (order.MaxProfit != null)
                {
                    order.MaxDiscount = (order.MaxProfit / order.CostPrice) * 100;
                }
            }

            return order;
        }

        public async Task<Order> CalculateCostPrice(Order order)
        {
            if (order.Supplier != null && order.ProductInfo != null)
            {
                decimal? weightFactorInRub = 0m;
                switch (order.Supplier.WeightFactorCurrencyCode)
                {
                    case OzonDomains.CurrencyCode.USD:
                        decimal rateUSD = await _currencyRateFetcher.GetUSDRateAsync();
                        weightFactorInRub = order.Supplier.WeightFactor * rateUSD;
                        break;

                    case OzonDomains.CurrencyCode.EUR:
                        decimal rateEUR = await _currencyRateFetcher.GetEURRateAsync();
                        weightFactorInRub = order.Supplier.WeightFactor * rateEUR;
                        break;

                    case OzonDomains.CurrencyCode.BYN:
                        decimal rateBYN = await _currencyRateFetcher.GetBYNRateAsync();
                        weightFactorInRub = order.Supplier.WeightFactor * rateBYN;
                        break;

                    case OzonDomains.CurrencyCode.RUB:
                        break;
                }

                if (order.Supplier.WeightFactor != 0 && order.ProductInfo != null && order.ProductInfo.Weight != null)
                {

                    order.CostPrice = order.ProductInfo.Weight * weightFactorInRub + order.PurchasePrice;
                }
                else
                {
                    order.CostPrice = order.PurchasePrice;
                }

                
            }
            return order;
        }
    }
}

