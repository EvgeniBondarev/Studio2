using Newtonsoft.Json.Linq;
using OzonDomains;
using OzonDomains.Models;
using OzonRepositories.Context;
using OzonRepositories.Migrations.OzonIdentityDb;
using Servcies.DataServcies;
using Servcies.ParserServcies.HelpDictEnum;
using Servcies.ReleasServcies.ReleaseManager;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Servcies.ParserServcies
{
    public class OrderCaster
    {
        private readonly OzonOrderContext _context;
        private readonly ReleaseManager _releaseManager;
        private readonly OrdersFileMetadataDataService _ordersFileMetadataDataService;

        public OrderCaster(OzonOrderContext context,
                           ReleaseManager releaseManager,
                           OrdersFileMetadataDataService ordersFileMetadataDataService)
        {
            _context = context;
            _releaseManager = releaseManager;
            _ordersFileMetadataDataService = ordersFileMetadataDataService;
        }

        public async Task<List<Order>> JsonToOrders(JArray orders)
        {
            List<Order> resultOrders = new List<Order>();

            foreach (var order in orders)
            {
                resultOrders.Add(await CastToModel(order));
            }
            return resultOrders;
        }

        public async Task<List<Order>> ExcelToOrders(List<Dictionary<string, string>> table,
                                                    OzonClient client,
                                                    Manufacturer manufacturer,
                                                    Warehouse warehouse,
                                                    Supplier supplier,
                                                    string clientStatus,
                                                    CurrencyCode currencyCode,
                                                    DateTime? selectedShippingDate,
                                                    DateTime? selectedProcessingDate)
        {
            List<Order> resultOrders = new List<Order>();

            try
            {
                foreach (var row in table)
                {
                    resultOrders.Add(await CastToModelFromExcel(row, client, manufacturer, warehouse, supplier,clientStatus, currencyCode, selectedShippingDate, selectedProcessingDate));
                }
            }
            catch (Exception ex) 
            { 
            }

            return resultOrders;
        }


        public async Task<List<Order>> YandexToOrders(JArray orders)
        {
            List<Order> resultOrders = new List<Order>();

            foreach (var order in orders)
            {
                resultOrders.Add(await CastToModelFromYandex(order));
            }
            return resultOrders;
        }

        public async Task<List<Order>> SetFileDataAsync(List<Order> orders, string filePath, string fileName)
        {
            // Проверка на существование метаданных файла и создание их при необходимости
            var fileMetadata = await _ordersFileMetadataDataService.GetOrdersFileMetadataAsync(new OrdersFileMetadata
            {
                FileName = fileName,
                FolderName = filePath
            });

            // Если метаданные не найдены, создаем новые и назначаем их к переменной fileMetadata
            if (fileMetadata == null)
            {
                fileMetadata = new OrdersFileMetadata
                {
                    FileName = fileName,
                    FolderName = filePath
                };

                await _ordersFileMetadataDataService.AddOrdersFileMetadata(fileMetadata);

                // После добавления, удостоверимся, что мы работаем с актуальными данными из БД
                fileMetadata = await _ordersFileMetadataDataService.GetOrdersFileMetadataAsync(fileMetadata);
            }

            // Присвоение метаданных файла к каждому заказу
            foreach (var order in orders)
            {
                order.ExcelFileData = fileMetadata;
            }

            return orders;
        }

        private async Task<Order> CastToModel(JToken? jsonOrder)
        {
            Order order = new();

            CultureInfo culture = new("en-US");
            NumberStyles style = NumberStyles.Number;

            order.Key = jsonOrder["Номер отправления"].ToString() + jsonOrder["Артикул"].ToString();

            order.ShipmentNumber = jsonOrder["Номер отправления"].ToString();


            order.ProcessingDate = DateTime.TryParse(jsonOrder["Принят в обработку"].ToString(), culture, out var processingDate)
                ? processingDate
                : null;

            order.ShippingDate = DateTime.TryParse(jsonOrder["Дата отгрузки"].ToString(), culture, out var shipingData)
                ? shipingData
                : null;

            order.Status = SetCorrectStatus(jsonOrder["Статус"].ToString());

            order.ShipmentAmount = decimal.TryParse(DelSubStr(jsonOrder["Сумма отправления"].ToString()), style, culture, out var shipmentAmount)
                ? shipmentAmount
                : null;

            order.ProductName = jsonOrder["Наименование товара"].ToString();

            order.ProductKey = jsonOrder["Артикул"].ToString();



            order.Quantity = int.TryParse(DelSubStr(jsonOrder["Количество"].ToString()), style, culture, out var quantity)
            ? quantity
            : null;


            Warehouse shipmentWarehouse = _context.Warehouses.FirstOrDefault(w => w.Name == jsonOrder["productWarehousesAndCitysWithNumber"]["delivery_method"]["warehouse"].ToString());

            if (shipmentWarehouse != null)
            {
                order.ShipmentWarehouse = shipmentWarehouse;
            }
            else
            {
                var newWarehouse = new Warehouse
                {
                    Name = jsonOrder["productWarehousesAndCitysWithNumber"]["delivery_method"]["warehouse"].ToString()
                };

                _context.Warehouses.Add(newWarehouse);
                _context.SaveChanges();
                order.ShipmentWarehouse = newWarehouse;
            }

            string extractedCode = Regex.Match(jsonOrder["Артикул"].ToString(), @"=(\w{3})").Groups[1].Value;

            if (string.IsNullOrEmpty(extractedCode))
            {
                extractedCode = Regex.Match(jsonOrder["Артикул"].ToString(), @"=(\w{2})").Groups[1].Value;
            }

            Manufacturer manufacturer = _context.Manufacturers
                .Where(m => m.Code == extractedCode)
                .FirstOrDefault();

            if (manufacturer != null)
            {
                order.Manufacturer = manufacturer;
            }
            else
            {
                var newManufacturer = new Manufacturer { Code = extractedCode };
                _context.Manufacturers.Add(newManufacturer);
                _context.SaveChanges();
                order.Manufacturer = newManufacturer;
            }

            string currencyCode = jsonOrder["Код валюты отправления"].ToString();

            using (var transaction = _context.Database.BeginTransaction())
            {
                // Повторная проверка наличия валюты внутри транзакции
                Currency currency = _context.Currencys
                    .FirstOrDefault(c => c.Name == currencyCode);

                if (currency != null)
                {
                    order.Сurrency = currency;
                }
                else
                {
                    var newCurrency = new Currency
                    {
                        Name = currencyCode
                    };

                    _context.Currencys.Add(newCurrency);
                    _context.SaveChanges();

                    order.Сurrency = newCurrency;
                }

                // Завершаем транзакцию
                transaction.Commit();
            }


            order.Supplier = null;
            order.PurchasePrice = null;

            if (order.ProductInfo == null)
            {
                Product product = _context.Products.FirstOrDefault(p => p.Article == jsonOrder["Артикул"].ToString());
                if (product != null)
                {
                    order.ProductInfo = product;
                }
                else
                {
                    product = new Product();
                    product.Article = jsonOrder["Артикул"].ToString();
                    product.OzonProductId = jsonOrder["productWithArticle"]["Ozon Product ID"].ToString();
                    product.FboOzonSkuId = jsonOrder["productWithArticle"]["FBO OZON SKU ID"].ToString();
                    product.FbsOzonSkuId = jsonOrder["productWithArticle"]["FBS OZON SKU ID"].ToString();
                    product.CommercialCategory = jsonOrder["productWithArticle"]["Категория комиссии"].ToString();

                    double volume;
                    if (double.TryParse(DelSubStr(jsonOrder["productWithArticle"]["Объем товара, л"].ToString()), style, culture, out volume))
                    {
                        product.Volume = volume;
                    }
                    else
                    {
                        product.Volume = null;
                    }

                    double volumetricWeight;
                    if (double.TryParse(DelSubStr(jsonOrder["productWithArticle"]["Объемный вес, кг"].ToString()), style, culture, out volumetricWeight))
                    {
                        product.VolumetricWeight = volumetricWeight;
                    }
                    else
                    {
                        product.VolumetricWeight = null;
                    }

                    _context.Products.Add(product);
                    _context.SaveChanges();

                    order.ProductInfo = product;

                }
            }


            var city = jsonOrder["productWarehousesAndCitysWithNumber"]["analytics_data"]["city"].ToString();
            var region = jsonOrder["productWarehousesAndCitysWithNumber"]["analytics_data"]["region"].ToString();

            order.DeliveryCity = city != null ? city.ToString() : region.ToString();


            if (order.AppStatus == null)
            {
                string statusName = "Не указан";

                using (var transaction = _context.Database.BeginTransaction())
                {
                    // Повторная проверка наличия статуса внутри транзакции
                    AppStatus appStatus = _context.AppStatuses.FirstOrDefault(c => c.Name == statusName);

                    if (appStatus != null)
                    {
                        order.AppStatus = appStatus;
                    }
                    else
                    {
                        var newStatus = new AppStatus
                        {
                            Name = statusName
                        };

                        _context.AppStatuses.Add(newStatus);
                        _context.SaveChanges();

                        order.AppStatus = newStatus;
                    }

                    // Завершаем транзакцию
                    transaction.Commit();
                }
            }

            if (order.Supplier == null)
            {
                string supplierName = "Не указан";

                using (var transaction = _context.Database.BeginTransaction())
                {
                    // Повторная проверка наличия поставщика внутри транзакции
                    Supplier supplier = _context.Suppliers.FirstOrDefault(c => c.Name == supplierName);

                    if (supplier != null)
                    {
                        order.Supplier = supplier;
                    }
                    else
                    {
                        var newSupplier = new Supplier
                        {
                            Name = supplierName
                        };

                        _context.Suppliers.Add(newSupplier);
                        _context.SaveChanges();

                        order.Supplier = newSupplier;
                    }

                    // Завершаем транзакцию
                    transaction.Commit();
                }
            }

            order = SetCorrectProductKey(order);

            var (price, priceWithDiscount, maxComment, minComment, min, max) = CalculateСommissions(jsonOrder, order);

            order.Price = price;
            order.ProductInfo.CurrentPriceWithDiscount = priceWithDiscount;

            order.MaxOzonCommission = max;
            order.MinOzonCommission = min;
            order.MaxCommissionInfo = maxComment;
            order.MinCommissionInfo = minComment;

            _context.SaveChanges();

            return order;
        }

        public async Task<Order> CastToModelFromYandex(JToken? jsonOrder)
        {
            Order order = new();

            CultureInfo culture = new("en-US");
            NumberStyles style = NumberStyles.Number;
            var dateFormats = new[] { "dd-MM-yyyy HH:mm:ss", "dd-MM-yyyy" };

            order.Key = jsonOrder["id"].ToString() + jsonOrder["items"][0]["offerId"].ToString();

            order.ShipmentNumber = jsonOrder["id"].ToString();


            order.ProcessingDate = DateTime.TryParseExact(
                                    jsonOrder["creationDate"].ToString(),
                                    dateFormats,
                                    culture,
                                    DateTimeStyles.None,
                                    out var processingDate)
                                    ? processingDate
                                    : null;

            order.ShippingDate = jsonOrder["delivery"]?["shipments"]?.FirstOrDefault()?["shipmentDate"] != null &&
                                    DateTime.TryParseExact(
                                        jsonOrder["delivery"]["shipments"]?.FirstOrDefault()?["shipmentDate"]?.ToString(),
                                        dateFormats,
                                        culture,
                                        DateTimeStyles.None,
                                        out var shippingDate)
                                    ? shippingDate
                                    : null;

            order.Status = SetCorrectStatus(YandexStatus.OrderStatuses[jsonOrder["status"].ToString()]);

            //order.ShipmentAmount = decimal.TryParse(DelSubStr(jsonOrder["Сумма отправления"].ToString()), style, culture, out var shipmentAmount)
            //    ? shipmentAmount
            //    : null;

            order.ProductName = jsonOrder["items"][0]["offerName"].ToString();

            order.ProductKey = jsonOrder["items"][0]["offerId"].ToString();



            order.Quantity = int.TryParse(DelSubStr(jsonOrder["items"][0]["count"].ToString()), style, culture, out var quantity)
            ? quantity
            : null;


            Warehouse shipmentWarehouse = _context.Warehouses.FirstOrDefault(w => w.Name == jsonOrder["warehouseName"].ToString());

            if (shipmentWarehouse != null)
            {
                order.ShipmentWarehouse = shipmentWarehouse;
            }
            else
            {
                var newWarehouse = new Warehouse
                {
                    Name = jsonOrder["warehouseName"].ToString()
                };

                _context.Warehouses.Add(newWarehouse);
                _context.SaveChanges();
                order.ShipmentWarehouse = newWarehouse;
            }

            string extractedCode = Regex.Match(jsonOrder["items"][0]["offerId"].ToString(), @"=(\w{3})").Groups[1].Value;

            if (string.IsNullOrEmpty(extractedCode))
            {
                extractedCode = Regex.Match(jsonOrder["items"][0]["offerId"].ToString(), @"=(\w{2})").Groups[1].Value;
            }

            Manufacturer manufacturer = _context.Manufacturers
                .Where(m => m.Code == extractedCode)
                .FirstOrDefault();

            if (manufacturer != null)
            {
                order.Manufacturer = manufacturer;
            }
            else
            {
                var newManufacturer = new Manufacturer { Code = extractedCode };
                _context.Manufacturers.Add(newManufacturer);
                _context.SaveChanges();
                order.Manufacturer = newManufacturer;
            }

            string currencyCode = jsonOrder["currency"].ToString();

            using (var transaction = _context.Database.BeginTransaction())
            {
                // Повторная проверка наличия валюты внутри транзакции
                Currency currency = _context.Currencys
                    .FirstOrDefault(c => c.Name == currencyCode);

                if (currency != null)
                {
                    order.Сurrency = currency;
                }
                else
                {
                    var newCurrency = new Currency
                    {
                        Name = currencyCode
                    };

                    _context.Currencys.Add(newCurrency);
                    _context.SaveChanges();

                    order.Сurrency = newCurrency;
                }

                // Завершаем транзакцию
                transaction.Commit();
            }


            order.Supplier = null;
            order.PurchasePrice = null;

            if (order.ProductInfo == null)
            {
                Product product = _context.Products.FirstOrDefault(p => p.Article == jsonOrder["items"][0]["offerId"].ToString());
                if (product != null)
                {
                    order.ProductInfo = product;
                }
                else
                {
                    product = new Product();
                    product.Article = jsonOrder["items"][0]["offerId"].ToString();

                    product.Volume = null;

                    _context.Products.Add(product);
                    _context.SaveChanges();

                    order.ProductInfo = product;

                }
            }


            var city = jsonOrder["delivery"]?["address"]?["city"]?.ToString() ?? string.Empty;
            var region = jsonOrder["delivery"]?["address"]?["country"]?.ToString() ?? string.Empty;

            order.DeliveryCity = city != null ? city.ToString() : region.ToString();


            if (order.AppStatus == null)
            {
                string statusName = "Не указан";

                using (var transaction = _context.Database.BeginTransaction())
                {
                    // Повторная проверка наличия статуса внутри транзакции
                    AppStatus appStatus = _context.AppStatuses.FirstOrDefault(c => c.Name == statusName);

                    if (appStatus != null)
                    {
                        order.AppStatus = appStatus;
                    }
                    else
                    {
                        var newStatus = new AppStatus
                        {
                            Name = statusName
                        };

                        _context.AppStatuses.Add(newStatus);
                        _context.SaveChanges();

                        order.AppStatus = newStatus;
                    }

                    // Завершаем транзакцию
                    transaction.Commit();
                }
            }

            if (order.Supplier == null)
            {
                string supplierName = "Не указан";

                using (var transaction = _context.Database.BeginTransaction())
                {
                    // Повторная проверка наличия поставщика внутри транзакции
                    Supplier supplier = _context.Suppliers.FirstOrDefault(c => c.Name == supplierName);

                    if (supplier != null)
                    {
                        order.Supplier = supplier;
                    }
                    else
                    {
                        var newSupplier = new Supplier
                        {
                            Name = supplierName
                        };

                        _context.Suppliers.Add(newSupplier);
                        _context.SaveChanges();

                        order.Supplier = newSupplier;
                    }

                    // Завершаем транзакцию
                    transaction.Commit();
                }
            }

            order.Price = decimal.TryParse(DelSubStr(jsonOrder["items"][0]["priceBeforeDiscount"].ToString()), style, culture, out var price)
                            ? price
                            : null;

            order.ProductInfo.CurrentPriceWithDiscount = decimal.TryParse(DelSubStr(jsonOrder["items"][0]["buyerPriceBeforeDiscount"].ToString()), 
                                                                 style, culture, out var сurrentPriceWithDiscount)
                            ? price
                            : null;

            order.MaxOzonCommission = 0;
            order.MinOzonCommission = 0;

            order = SetCorrectProductKey(order);

            _context.SaveChanges();

            return order;
        }

        private async Task<Order> CastToModelFromExcel(Dictionary<string, string> dataColumn,
                                                       OzonClient selectedClient,
                                                       Manufacturer selectedManufacturer,
                                                       Warehouse selectedWarehouse,
                                                       Supplier selectedSupplier,
                                                       string selectedClientStatus,
                                                       CurrencyCode selectedCurrencyCode,
                                                       DateTime? selectedShippingDate,
                                                       DateTime? selectedProcessingDate)
        {
            Order order = new();

            CultureInfo culture = new("en-US");
            NumberStyles style = NumberStyles.Number;

            //"Номер отправления", "Ozon клиент", "Принят в обработку", "Дата отгрузки", "Статус Ozon Статус",
            //"Наименование товара", "Артикул", "Склад отгрузки Поставщик", "Номер заказа поставщику", "Цена сайта",
            //"Цена", "Кол.", "Сумма отправления", "Категория", "Объемный вес", "Цена закупки", "Комиссия ОЗОН",
            //"Прибыль", "Наценка %", "Город доставки", "Минимальная комиссия", "Максимальная комиссия"

            if(dataColumn.TryGetValue("Артикул", out var article))
            {
                order.ProductKey = article?.ToString();
            }
            else if(dataColumn.TryGetValue("Key", out var key))
            {
                order.ProductKey = key.ToString();
                selectedManufacturer = new Manufacturer() { Id = -1};
            }


            order.Key = (dataColumn.TryGetValue("Номер заказа", out var shipmentNumber) ? shipmentNumber?.ToString() : null)
                      + (order.ProductKey != null ? order.ProductKey : null);

            order.ShipmentNumber = shipmentNumber?.ToString();


            if (dataColumn.TryGetValue("Клиент", out var clientName))
            {
                OzonClient clientByName = _context.OzonClients.FirstOrDefault(o => o.Name == clientName);
                if (clientByName == null)
                {
                    OzonClient newOzonClient = new OzonClient()
                    {
                        Name = clientName,
                        CurrencyCode = CurrencyCode.USD,
                    };

                    _context.OzonClients.Add(newOzonClient);
                    await _context.SaveChangesAsync();

                    order.OzonClient = newOzonClient;
                }
                else
                {
                    order.OzonClient = clientByName;
                }
            }
            else if (selectedClient.Id != 0)
            {
                order.OzonClient = _context.OzonClients.Find(selectedClient.Id);
            }


            if (selectedCurrencyCode != CurrencyCode.NON && order.OzonClient != null)
            {
                if (order.OzonClient.CurrencyCode != selectedCurrencyCode)
                {
                    order.OzonClient.CurrencyCode = selectedCurrencyCode;
                }
            }
            else if (selectedClient.Id != 0)
            {
                order.OzonClient = _context.OzonClients.Find(selectedClient.Id);
            }

            var cultureDateInfo = new CultureInfo("ru-RU");

            order.ProcessingDate = dataColumn.TryGetValue("Принят в обработку", out var processingDateStr) &&
                                   DateTime.TryParse(processingDateStr, cultureDateInfo, DateTimeStyles.None, out var processingDate)
                                   ? processingDate
                                   : selectedProcessingDate != null ? selectedProcessingDate : DateTime.Now.AddHours(_releaseManager.GetTimeZone());


            order.ShippingDate = dataColumn.TryGetValue("Дата отгрузки", out var shippingDateStr) &&
                                 DateTime.TryParse(shippingDateStr, cultureDateInfo, DateTimeStyles.None, out var shippingDate)
                                 ? shippingDate
                                 : selectedShippingDate;

            order.Status = dataColumn.TryGetValue("Статус", out var status) ? SetCorrectStatus(status?.ToString()) : null;



            order.ProductName = dataColumn.TryGetValue("Наименование товара", out var productName) ? productName?.ToString() : null;

            

            order.Quantity = dataColumn.TryGetValue("Кол.", out var quantityStr) &&
                             int.TryParse(DelSubStr(quantityStr), style, culture, out var quantity)
                             ? quantity
                             : (int?)null;

            if (dataColumn.TryGetValue("Склад отгрузки", out var warehouseName))
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    // Повторная проверка наличия склада отгрузки внутри транзакции
                    Warehouse shipmentWarehouse = _context.Warehouses.FirstOrDefault(w => w.Name == warehouseName);

                    if (shipmentWarehouse != null)
                    {
                        order.ShipmentWarehouse = shipmentWarehouse;
                    }
                    else
                    {
                        var newWarehouse = new Warehouse { Name = warehouseName };
                        _context.Warehouses.Add(newWarehouse);
                        _context.SaveChanges();
                        order.ShipmentWarehouse = newWarehouse;
                    }
                    transaction.Commit();
                }
            }
            else if (selectedWarehouse != null && selectedWarehouse.Id != 0)
            {
                order.ShipmentWarehouse = _context.Warehouses.FirstOrDefault(w => w.Id == selectedWarehouse.Id);
            }

            if (dataColumn.TryGetValue("Производитель", out var manufacturerName))
            {
                Manufacturer manufacturer = _context.Manufacturers.FirstOrDefault(m => m.Name == manufacturerName);

                if (manufacturer != null)
                {
                    order.Manufacturer = manufacturer;
                }
                else
                {
                    var newManufacturer = new Manufacturer
                    {
                        Code = null,
                        Name = manufacturerName
                    };

                    _context.Manufacturers.Add(newManufacturer);
                    _context.SaveChanges();

                    order.Manufacturer = newManufacturer;
                }

            }
            else if (selectedManufacturer != null && selectedManufacturer.Id != 0)
            {
                if (selectedManufacturer.Id == -1)
                {
                    string extractedCode = Regex.Match(article.ToString(), @"=(\w{3})").Groups[1].Value;

                    if (string.IsNullOrEmpty(extractedCode))
                    {
                        extractedCode = Regex.Match(article.ToString(), @"=(\w{2})").Groups[1].Value;
                    }

                    Manufacturer manufacturer = _context.Manufacturers
                        .Where(m => m.Code == extractedCode)
                        .FirstOrDefault();

                    if (manufacturer != null)
                    {
                        order.Manufacturer = manufacturer;
                    }
                    else
                    {
                        var newManufacturer = new Manufacturer { Code = extractedCode };
                        _context.Manufacturers.Add(newManufacturer);
                        _context.SaveChanges();
                        order.Manufacturer = newManufacturer;
                    }
                }
                else if(selectedManufacturer.Id == -2)
                {
                    string tabelManufacturerName = order.ProductName != null ? order.ProductName.Split(' ')[0] : null;
                    if(tabelManufacturerName != null)
                    {
                        Manufacturer manufacturer = _context.Manufacturers.Where(m => m.Name.ToLower().Contains(tabelManufacturerName.ToLower())).FirstOrDefault();
                        if (manufacturer != null)
                        {
                            order.Manufacturer = manufacturer;
                        }
                    }
                }
                else
                {
                    order.Manufacturer = _context.Manufacturers.FirstOrDefault(m => m.Id == selectedManufacturer.Id);
                }
            }


            if (dataColumn.TryGetValue("Код валюты отправления", out var currencyName))
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    Currency currency = _context.Currencys.FirstOrDefault(c => c.Name == currencyName);

                    if (currency != null)
                    {
                        order.Сurrency = currency;
                    }
                    else
                    {
                        var newCurrency = new Currency
                        {
                            Name = currencyName
                        };

                        _context.Currencys.Add(newCurrency);
                        _context.SaveChanges();

                        order.Сurrency = newCurrency;
                    }

                    // Завершаем транзакцию
                    transaction.Commit();
                }
            }

            order.Supplier = null;
            order.PurchasePrice = null;

            if (order.ProductInfo == null && dataColumn.TryGetValue("Артикул", out var productArticle))
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    // Повторная проверка наличия продукта по артикулу внутри транзакции
                    Product product = _context.Products.FirstOrDefault(p => p.Article == productArticle);

                    if (product != null)
                    {
                        order.ProductInfo = product;
                    }
                    else
                    {
                        product = new Product
                        {
                            Article = productArticle
                            // Заполнить остальные поля продукта при необходимости
                        };

                        _context.Products.Add(product);
                        _context.SaveChanges();

                        order.ProductInfo = product;
                    }

                    // Завершаем транзакцию
                    transaction.Commit();
                }
            }
            else if(order.ProductInfo == null && !dataColumn.TryGetValue("Артикул", out var nullProductArticle))
            {
                var product = new Product();

                _context.Products.Add(product);
                _context.SaveChanges();

                order.ProductInfo = product;
            }

            order.DeliveryCity = dataColumn.TryGetValue("Город доставки", out var deliveryCity) ? deliveryCity?.ToString() : null;

            if (dataColumn.TryGetValue("Статус клинта", out var clientStauts))
            {
                order.Status = clientStauts;
            }
            else if (selectedClientStatus != null)
            {
                order.Status = selectedClientStatus;
            }

            if (order.AppStatus == null)
            {
                string statusName = "Не указан";

                using (var transaction = _context.Database.BeginTransaction())
                {
                    // Повторная проверка наличия статуса по имени внутри транзакции
                    AppStatus appStatus = _context.AppStatuses.FirstOrDefault(c => c.Name == statusName);

                    if (appStatus != null)
                    {
                        order.AppStatus = appStatus;
                    }
                    else
                    {
                        var newStatus = new AppStatus { Name = statusName };

                        _context.AppStatuses.Add(newStatus);
                        _context.SaveChanges();

                        order.AppStatus = newStatus;
                    }

                    // Завершаем транзакцию
                    transaction.Commit();
                }
            }

            if (order.Supplier == null && selectedSupplier.Id == 0)
            {
                string supplierName = "Не указан";

                using (var transaction = _context.Database.BeginTransaction())
                {
                    // Повторная проверка наличия поставщика по имени внутри транзакции
                    Supplier supplier = _context.Suppliers.FirstOrDefault(c => c.Name == supplierName);

                    if (supplier != null)
                    {
                        order.Supplier = supplier;
                    }
                    else
                    {
                        var newSupplier = new Supplier { Name = supplierName };

                        _context.Suppliers.Add(newSupplier);
                        _context.SaveChanges();

                        order.Supplier = newSupplier;
                    }

                    // Завершаем транзакцию
                    transaction.Commit();
                }
            }
            else if (selectedSupplier.Id != 0)
            {
                order.Supplier = _context.Suppliers.FirstOrDefault(m => m.Id == selectedSupplier.Id);
            }

            order.Price = dataColumn.TryGetValue("Цена", out var priceStr) &&
                                   decimal.TryParse(DelSubStr(priceStr), style, culture, out var price)
                                   ? price
                                   : (decimal?)null;

            if (dataColumn.TryGetValue("Сумма отправления", out var shipmentAmountStr) &&
                       decimal.TryParse(DelSubStr(shipmentAmountStr), style, culture, out var shipmentAmount))
            {
                order.ShipmentAmount = shipmentAmount;
            }
            else if (order.Price != (decimal?)null && order.Quantity != (int?)null)
            {
                order.ShipmentAmount = order.Price * order.Quantity;
            }

            order.ProductInfo.CurrentPriceWithDiscount = dataColumn.TryGetValue("Цена сайта", out var priceWithDiscountStr) &&
                                   decimal.TryParse(priceWithDiscountStr, style, culture, out var priceWithDiscount)
                                   ? priceWithDiscount
                                   : 0;

            order.MaxOzonCommission = dataColumn.TryGetValue("Максимальная комиссия", out var maxOzonCommissionStr) &&
                                   decimal.TryParse(maxOzonCommissionStr, style, culture, out var maxOzonCommission)
                                   ? maxOzonCommission
                                   : 0;

            order.MinOzonCommission = dataColumn.TryGetValue("Минимальная комиссия", out var minOzonCommissionStr) &&
                                   decimal.TryParse(minOzonCommissionStr, style, culture, out var minOzonCommission)
                                   ? minOzonCommission
                                   : 0;

            order.PurchasePrice = dataColumn.TryGetValue("Цена закупки", out var purchasePriceStr) &&
                                   decimal.TryParse(purchasePriceStr, style, culture, out var purchasePrice)
                                   ? purchasePrice
                                   : 0;

            order = SetCorrectProductKey(order);

            order.FromFile = true;

            _context.SaveChanges();

            return order;
        }

        private Order SetCorrectProductKey(Order order)
        {
            string pattern = @"^[A-Za-z0-9\-[\]]+=[A-Za-z0-9\[\]]+$";

            Regex regex = new Regex(pattern);
            Match match = regex.Match(order.ProductKey);

            if (match.Success)
            {
                order.Article = Regex.Replace(order.ProductKey.Substring(0, order.ProductKey.IndexOf('=')), "[^A-Za-z0-9]", "");
                return order;
            }

            string cleanedArticle = Regex.Replace(order.ProductKey, "[^A-Za-z0-9]", "");
            string key = order.Manufacturer != null && order.Manufacturer.Code != null ? $"={order.Manufacturer.Code}" : "=";

            order.Article = order.ProductKey;
            order.ProductKey = cleanedArticle + key;

            return order;
        }

        private string SetCorrectStatus(string status)
        {
            if(status == null)
            {
                return null;
            }

            if (status == "Отменен")
            {
                status = "Отменён";
            }
            return status;
        }

        private (decimal, decimal, string, string, decimal, decimal) CalculateСommissions(JToken jsonOrder, Order order)
        {
            JToken productPrices = jsonOrder["productPricesWithArticle"];
            JToken productСommissions = productPrices["commissions"];
            string regionCode = _releaseManager.GetRegionCode();

            decimal priceWithDiscount = ConvertNumberByRegion(productPrices["price"]["marketing_price"].ToString(), regionCode);
            decimal price = ConvertNumberByRegion(productPrices["price"]["marketing_seller_price"].ToString(), regionCode); 

            var product = jsonOrder["productWarehousesAndCitysWithNumber"]?["products"]
            ?.FirstOrDefault(p => p?["offer_id"]?.ToString() == order.ProductKey);

            if (product != null && price == 0)
            {
                string priceStr = product["price"]?.ToString();
                price = ConvertNumberByRegion(priceStr, regionCode);
            }

            decimal percentFbs = ConvertNumberByRegion(productСommissions["sales_percent_fbs"].ToString(), regionCode);
            decimal acquiring = ConvertNumberByRegion(productPrices["acquiring"].ToString(), regionCode);
            decimal delivToCustomer = ConvertNumberByRegion(productСommissions["fbs_deliv_to_customer_amount"].ToString(), regionCode);
            decimal fbsFirstMileMax = ConvertNumberByRegion(productСommissions["fbs_first_mile_max_amount"].ToString(), regionCode);
            decimal fbsDirectFlowTransMax = ConvertNumberByRegion(productСommissions["fbs_direct_flow_trans_max_amount"].ToString(), regionCode);
            decimal fbsFirstMileMin = ConvertNumberByRegion(productСommissions["fbs_first_mile_min_amount"].ToString(), regionCode);
            decimal fbsDirectFlowTransMin = ConvertNumberByRegion(productСommissions["fbs_direct_flow_trans_min_amount"].ToString(), regionCode);

            decimal salesСommission = price / 100 * percentFbs;
            decimal standaertSum = acquiring + delivToCustomer + salesСommission;

            decimal maxСommission = standaertSum + fbsFirstMileMax + fbsDirectFlowTransMax;
            decimal minCommission = standaertSum + fbsFirstMileMin + fbsDirectFlowTransMin;


            string sb1 = $" = Комиссия за продажу ({salesСommission}) + Максимальная комиссия за эквайринг({acquiring}) +" +
                          $"Последняя миля (FBS) ({delivToCustomer}) + Максимальная комиссия за обработку отправления (FBS) — 25 рублей ({fbsFirstMileMax}) +" +
                          $"Магистраль до (FBS) ({fbsDirectFlowTransMax})";

            string sb2 = $" = Комиссия за продажу ({salesСommission}) + Максимальная комиссия за эквайринг({acquiring}) +" +
                          $"Последняя миля (FBS) ({delivToCustomer}) + Минимальная комиссия за обработку отправления (FBS) — 0 рублей ({fbsFirstMileMin}) +" +
                          $"Магистраль от (FBS) ({fbsDirectFlowTransMin})";

            var (maxComment, minComment, min, max) = (sb1, sb2, minCommission, maxСommission);

            return (price, priceWithDiscount, maxComment, minComment, min, max);
        }



        public decimal ConvertNumberByRegion(string number, string regionCode)
        {

            if (regionCode == "en")
            {
                return decimal.Parse(number.ToString().Replace(',', '.'));
            }
            else if (regionCode == "ru")
            {
                return decimal.Parse(number.ToString().Replace('.', ','));
            }
            else
            {
                return decimal.Parse(number.ToString().Replace('.', ','));
            }
        }
        private string DelSubStr(string str)
        {
            if (str.StartsWith("'"))
            {
                return str.Substring(1);
            }
            return str;
        }


    }
}