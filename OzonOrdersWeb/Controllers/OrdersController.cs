using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using OServcies.FiltersServcies.FilterModels;
using OzonDomains;
using OzonDomains.Models;
using OzonOrdersWeb.Services.Cookies;
using OzonOrdersWeb.ViewModels.OrderViewModels;
using OzonRepositories.Context;
using OzonRepositories.Context.Identity;
using Servcies.ApiServcies.DropBoxApi;
using Servcies.ApiServcies.InterpartsApi;
using Servcies.ApiServcies.OzonApi;
using Servcies.ApiServcies.YandexApi;
using Servcies.CacheServcies.Cache.OzonOrdersCache;
using Servcies.DataServcies;
using Servcies.DataServcies.DTO;
using Servcies.FiltersServcies.DataFilterManagers;
using Servcies.FiltersServcies.SortModels;
using Servcies.ParserServcies;
using Servcies.ParserServcies.FielParsers;
using Servcies.ParserServcies.HelpDictEnum;
using Servcies.PriceСlculationServcies;
using Servcies.ReleasServcies.ReleaseManager;
using Servcies.TransactionUtilsServcies;
using Services.CacheServcies.Cache;
using Services.CacheServcies.Cache.OzonOrdersCache;
using System.IO.Compression;


//TODO: Добавить прямой доступ к бд по ссылке в шапке сайта
namespace OzonOrdersWeb.Controllers
{
    [Authorize(Roles = "User,Admin")]
    public class OrdersController : Controller, ISortLogicContriller<Order, OrderSortState>
    {
        private readonly OzonOrderContext _context;
        private readonly IWebHostEnvironment _appEnvironment;
        private readonly OrdersDataServcies _orderServcies;
        private readonly AppStatusDataServcies _appStatusServcies;
        private readonly SupplierDataServcies _supplierDataServcies;
        private readonly WarehouseDataServcies _warehouseDataServcies;
        private readonly OrderCaster _orderCaster;
        private readonly OrderCache _cache;
        private readonly CookiesManeger _cookies;
        private readonly OrderDataFilterManager _dataFilterManager;
        private readonly CacheUpdater<Order> _cacheUpdater;
        private readonly OzonJsonDataBuilder _jsonDataBuilder;
        private readonly OrderHistoryDataServcies _orderHistory;
        private readonly ReleaseManager _releaseManager;
        private readonly TransactionDataServcies _transactionDataServcies;
        private readonly TransactionCache _transactionCache;
        private readonly OrderToSupplierTransactionManager _orderToSupplierTransaction;
        private readonly DuplicateOrdersServcies _duplicateOrdersServcies;
        private readonly OzonClientServcies _ozonClientServcies;
        private readonly UserManager<CustomIdentityUser> _userManager;
        private readonly ExcelParser _excelParser;
        private readonly ColumnMappingDataServcies _columnMappingDataServcies;
        private readonly CurrencyRateFetcher _currencyRateFetcher;
        private readonly DropboxApiClient _dropboxApiClient;
        private readonly ManufacturerDataService _manufacturerDataService;
        private readonly ExcelExporter _excelExporter;
        private readonly UserAccessDataServices _userAccessDataServices;
        private readonly YandexDataManager _yandexDataManager;
        private readonly ProductsDataServcies _productsDataServcies;
        public readonly InterpartsApiDataManager _interpartsApiDataManager;
        public OrdersController(OzonOrderContext context,
                                OrdersDataServcies orderRepository,
                                AppStatusDataServcies appStatusDataServcies,
                                SupplierDataServcies supplierDataServcies,
                                WarehouseDataServcies warehouseDataServcies,
                                OrderCaster orderCaster,
                                IWebHostEnvironment webHostEnvironment,
                                OrderCache orderCache,
                                CookiesManeger cookiesManeger,
                                OrderDataFilterManager dataFilterManager,
                                CacheUpdater<Order> cacheUpdater,
                                OzonJsonDataBuilder jsonDataBuilder,
                                OrderHistoryDataServcies orderHistory,
                                ReleaseManager releaseManager,
                                TransactionDataServcies transactionDataServcies,
                                TransactionCache transactionCache,
                                OrderToSupplierTransactionManager orderToSupplierTransactionManager,
                                DuplicateOrdersServcies duplicateOrdersServcies,
                                OzonClientServcies ozonClientServcies,
                                UserManager<CustomIdentityUser> userManager,
                                ExcelParser excelParser,
                                ColumnMappingDataServcies columnMappingDataServcies,
                                CurrencyRateFetcher currencyRateFetcher,
                                DropboxApiClient dropboxApiClient,
                                ManufacturerDataService manufacturerDataService,
                                ExcelExporter excelExporter,
                                UserAccessDataServices userAccessDataServices,
                                YandexDataManager yandexDataManager,
                                ProductsDataServcies productsDataServcies,
                                InterpartsApiDataManager interpartsApiDataManager)
        {
            _context = context;
            _orderServcies = orderRepository;
            _appStatusServcies = appStatusDataServcies;
            _supplierDataServcies = supplierDataServcies;
            _warehouseDataServcies = warehouseDataServcies;
            _orderCaster = orderCaster;
            _appEnvironment = webHostEnvironment;
            _cache = orderCache;
            _cookies = cookiesManeger;
            _dataFilterManager = dataFilterManager;
            _cacheUpdater = cacheUpdater;
            _jsonDataBuilder = jsonDataBuilder;
            _orderHistory = orderHistory;
            _releaseManager = releaseManager;
            _transactionDataServcies = transactionDataServcies;
            _transactionCache = transactionCache;
            _orderToSupplierTransaction = orderToSupplierTransactionManager;
            _duplicateOrdersServcies = duplicateOrdersServcies;
            _ozonClientServcies = ozonClientServcies;
            _userManager = userManager;
            _excelParser = excelParser;
            _columnMappingDataServcies = columnMappingDataServcies;
            _currencyRateFetcher = currencyRateFetcher;
            _dropboxApiClient = dropboxApiClient;
            _manufacturerDataService = manufacturerDataService;
            _excelExporter = excelExporter;
            _userAccessDataServices = userAccessDataServices;
            _yandexDataManager = yandexDataManager;
            _productsDataServcies = productsDataServcies;
            _interpartsApiDataManager = interpartsApiDataManager;
        }

        public async Task<IActionResult> Index(OrderSortState sortOrder = OrderSortState.StandardState, int page = 1)
        {
            SaveSortStateCookie(sortOrder);

            List<Order> ordersFromCache = await _cache.Get(page);

            await SetFilterLists(ordersFromCache);
            SetInfoMessage();

            string? filterDataString = HttpContext.Request.Cookies["FilterData"];
            var filterData = new OrderFilterModel();
            if (!string.IsNullOrEmpty(filterDataString))
            {
                filterData = JsonConvert.DeserializeObject<OrderFilterModel>(filterDataString);
                ordersFromCache = await _dataFilterManager.FilterByFilterData(ordersFromCache, filterData);
            }


            string buttonState = Request.Cookies["ButtonState"];
            ViewData["ButtonState"] = buttonState;
            ordersFromCache = await _dataFilterManager.FilterByRadioButton(ordersFromCache, buttonState);

            if (sortOrder == OrderSortState.StandardState && GetSortStateCookie() != null)
            {
                sortOrder = GetSortStateCookie();
            }

            SetSortOrderViewData(sortOrder);
            ordersFromCache = (await ApplySortOrder(ordersFromCache, sortOrder)).ToList();



            int pageSize = Int32.TryParse(Request.Cookies["PageSize"], out var size)
                ? size
                : 15;

            var returnableCount = await _orderServcies.GetReturnableCount();



            var pageViewModel = new OrderPageViewModel<Order, OrderFilterModel>(ordersFromCache, page, pageSize, filterData, returnableCount)
            {
                UniqueArticles = await _orderServcies.GetUniqueArticles(),
                UniqueDeliveryCitys = await _orderServcies.GetUniqueDeliveryCities(),
                UniqueNumbers = await _orderServcies.GetUniqueShipmentNumbers(),

                User = await _userManager.GetUserAsync(User),
            };

            if (pageViewModel.User != null && pageViewModel.User.UserAccessId != null)
            {
                pageViewModel.User.UserAccess = _context.UserAccess.Find(pageViewModel.User.UserAccessId);
            }

            var i = ordersFromCache.ToList().Where(o => o.FromFile == true).ToList();

            return View(pageViewModel);
        }



        [HttpPost]
        public async Task<IActionResult> Index(OrderFilterModel filterData, int page = 1, string buttonState = "")
        {
            List<Order> ordersFromCache = await _cache.Get(page);

            await SetFilterLists(ordersFromCache);

            ViewData["ButtonState"] = buttonState;

            ordersFromCache = await _dataFilterManager.FilterByFilterData(ordersFromCache, filterData);

            var serializedFilterData = JsonConvert.SerializeObject(filterData);
            HttpContext.Response.Cookies.Append("FilterData", serializedFilterData);

            Response.Cookies.Append("ButtonState", buttonState);
            ordersFromCache = await _dataFilterManager.FilterByRadioButton(ordersFromCache, buttonState);

            int pageSize = Int32.TryParse(Request.Cookies["PageSize"], out var size)
                ? size
                : 15;

            var returnableCount = await _orderServcies.GetReturnableCount();

            var pageViewModel = new OrderPageViewModel<Order, OrderFilterModel>(ordersFromCache, page, pageSize, filterData, returnableCount)
            {
                UniqueArticles = await _orderServcies.GetUniqueArticles(),
                UniqueDeliveryCitys = await _orderServcies.GetUniqueDeliveryCities(),
                UniqueNumbers = await _orderServcies.GetUniqueShipmentNumbers(),

                User = await _userManager.GetUserAsync(User),
            };

            if (pageViewModel.User.UserAccessId != null)
            {
                pageViewModel.User.UserAccess = _context.UserAccess.Find(pageViewModel.User.UserAccessId);
            }
            return View(pageViewModel);
        }

        public async Task<IActionResult> IndexV2(OrderSortState sortOrder = OrderSortState.StandardState, int page = 1)
        {
            SaveSortStateCookie(sortOrder);

            List<Order> ordersFromCache = await _cache.Get(page);

            await SetFilterLists(ordersFromCache);
            SetInfoMessage();

            string? filterDataString = HttpContext.Request.Cookies["FilterData"];
            var filterData = new OrderFilterModel();
            if (!string.IsNullOrEmpty(filterDataString))
            {
                filterData = JsonConvert.DeserializeObject<OrderFilterModel>(filterDataString);
                ordersFromCache = await _dataFilterManager.FilterByFilterData(ordersFromCache, filterData);
            }


            string buttonState = Request.Cookies["ButtonState"];
            ViewData["ButtonState"] = buttonState;
            ordersFromCache = await _dataFilterManager.FilterByRadioButton(ordersFromCache, buttonState);

            if (sortOrder == OrderSortState.StandardState && GetSortStateCookie() != null)
            {
                sortOrder = GetSortStateCookie();
            }

            SetSortOrderViewData(sortOrder);
            ordersFromCache = (await ApplySortOrder(ordersFromCache, sortOrder)).ToList();



            int pageSize = Int32.TryParse(Request.Cookies["PageSize"], out var size)
                ? size
                : 15;

            var returnableCount = await _orderServcies.GetReturnableCount();



            var pageViewModel = new OrderPageViewModel<Order, OrderFilterModel>(ordersFromCache, page, pageSize, filterData, returnableCount)
            {
                UniqueArticles = await _orderServcies.GetUniqueArticles(),
                UniqueDeliveryCitys = await _orderServcies.GetUniqueDeliveryCities(),
                UniqueNumbers = await _orderServcies.GetUniqueShipmentNumbers(),

                User = await _userManager.GetUserAsync(User),
            };

            if (pageViewModel.User != null && pageViewModel.User.UserAccessId != null)
            {
                pageViewModel.User.UserAccess = _context.UserAccess.Find(pageViewModel.User.UserAccessId);
            }

            var i = ordersFromCache.ToList().Where(o => o.FromFile == true).ToList();

            return View(pageViewModel);
        }



        [HttpPost]
        public async Task<IActionResult> Indexv2(OrderFilterModel filterData, int page = 1, string buttonState = "")
        {
            List<Order> ordersFromCache = await _cache.Get(page);

            await SetFilterLists(ordersFromCache);

            ViewData["ButtonState"] = buttonState;

            ordersFromCache = await _dataFilterManager.FilterByFilterData(ordersFromCache, filterData);

            var serializedFilterData = JsonConvert.SerializeObject(filterData);
            HttpContext.Response.Cookies.Append("FilterData", serializedFilterData);

            Response.Cookies.Append("ButtonState", buttonState);
            ordersFromCache = await _dataFilterManager.FilterByRadioButton(ordersFromCache, buttonState);

            int pageSize = Int32.TryParse(Request.Cookies["PageSize"], out var size)
                ? size
                : 15;

            var returnableCount = await _orderServcies.GetReturnableCount();

            var pageViewModel = new OrderPageViewModel<Order, OrderFilterModel>(ordersFromCache, page, pageSize, filterData, returnableCount)
            {
                UniqueArticles = await _orderServcies.GetUniqueArticles(),
                UniqueDeliveryCitys = await _orderServcies.GetUniqueDeliveryCities(),
                UniqueNumbers = await _orderServcies.GetUniqueShipmentNumbers(),

                User = await _userManager.GetUserAsync(User),
            };

            if (pageViewModel.User.UserAccessId != null)
            {
                pageViewModel.User.UserAccess = _context.UserAccess.Find(pageViewModel.User.UserAccessId);
            }
            return View(pageViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DelSortStateCookie()
        {
            Response.Cookies.Delete("SortState");
            Response.Cookies.Delete("HighlightedColumn");
            Response.Cookies.Append("SortState", OrderSortState.StandardState.ToString());
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DelSortStateCookieForV2()
        {
            Response.Cookies.Delete("SortState");
            Response.Cookies.Delete("HighlightedColumn");
            Response.Cookies.Append("SortState", OrderSortState.StandardState.ToString());
            return RedirectToAction(nameof(IndexV2));
        }

        [HttpPost]
        public async Task<IActionResult> SetPageSize(int size)
        {
            if (size > 100)
            {
                size = 100;
            }
            Response.Cookies.Append("PageSize", size.ToString());
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> SetPageSizeForV2(int size)
        {
            if (size > 100)
            {
                size = 100;
            }
            Response.Cookies.Append("PageSize", size.ToString());
            return RedirectToAction(nameof(IndexV2));
        }



        public async Task<IActionResult> Update()
        {
            await _cacheUpdater.Update(_cache);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UpdateV2()
        {
            await _cacheUpdater.Update(_cache);
            return RedirectToAction(nameof(IndexV2));
        }

        public async Task<IActionResult> Upload()
        {
            if (TempData.ContainsKey("ErorrResult") && TempData["ErorrResult"] != null)
            {
                string result = (string)TempData["ErorrResult"];
                ViewData["ErorrResult"] = result;
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(int period, ClientType clientType)
        {
            try
            {
                List<OzonClient> ozonClients = (await _ozonClientServcies.GetOzonClients()).Where(c => c.ClientType == ClientType.OZON).ToList();
                List<OzonClient> yandexClients = (await _ozonClientServcies.GetOzonClients()).Where(c => c.ClientType == ClientType.YANDEX).ToList();

                int[] result = new int[2];
                int timeZone = _releaseManager.GetTimeZone();
                var start = DateTime.Now.AddHours(-period * 24 + timeZone);
                var end = DateTime.Now.AddHours(timeZone);
                var clientStatus = "<br>Результат загрузки по клиентам<br>";

                if (clientType == ClientType.ALL || clientType == ClientType.YANDEX)
                {
                    clientStatus += await UploadOrders(yandexClients, start, end, ClientType.YANDEX, result);
                }

                if (clientType == ClientType.ALL || clientType == ClientType.OZON)
                {
                    clientStatus += await UploadOrders(ozonClients, start, end, ClientType.OZON, result);
                }

                TempData["UploadResult"] = result;
                TempData["UploadResultPeriod"] = $"{start:dd.MM.yyyy HH:mm:ss} - {end:dd.MM.yyyy HH:mm:ss}<br>{clientStatus}";
                await _cacheUpdater.Update(_cache);
                _duplicateOrdersServcies.DeleteDuplicateOrders();
            }
            catch (Exception ex)
            {
                throw new Exception($"Произошла ошибка при загрузке данных: {ex.Message}");
            }
            ClearSelectedIdsSession();
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> UploadOrders(List<OzonClient> clients, DateTime start, DateTime end, ClientType clientType, int[] result)
        {
            var clientStatus = string.Empty;

            foreach (var client in clients)
            {
                try
                {
                    if (clientType == ClientType.YANDEX)
                    {
                        _yandexDataManager.SetClient(client.DecryptClientId, client.DecryptApiKey);
                        var jsonData = await _yandexDataManager.GetOrders(start, end);
                        var orders = await _orderCaster.YandexToOrders(jsonData);
                        orders = orders.Select(order => { order.OzonClient = client; return order; }).ToList();
                        var uploadResult = await _orderServcies.AddOrders(orders);
                        result[0] += uploadResult[0];
                        result[1] += uploadResult[1];
                    }
                    else if (clientType == ClientType.OZON)
                    {
                        _jsonDataBuilder.SetClient(client.DecryptClientId, client.DecryptApiKey);
                        var jsonData = await _jsonDataBuilder.BuildData(start, end);
                        var orders = await _orderCaster.JsonToOrders(jsonData);
                        orders = orders.Select(order => { order.OzonClient = client; return order; }).ToList();
                        var uploadResult = await _orderServcies.AddOrders(orders);
                        result[0] += uploadResult[0];
                        result[1] += uploadResult[1];
                    }

                    clientStatus += $"{client.Name}: отчёт успешно создан<br>";
                }
                catch (Exception ex)
                {
                    clientStatus += $"{client.Name}: {ex.Message}<br>";
                    continue;
                }
            }

            return clientStatus;
        }



        [HttpPost]
        public async Task<IActionResult> UploadExcelFile(IFormFile file, int startRow = 1, int startColumn = 1, char delimiter = ';')
        {
            if (file != null && file.Length > 0)
            {
                string tempDir = Path.GetTempPath();
                string tempFilePath = Path.Combine(tempDir, Guid.NewGuid().ToString() + Path.GetExtension(file.FileName));

                try
                {
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    List<Dictionary<string, string>> tableData = new List<Dictionary<string, string>>();
                    List<string> tableHeaders = [];
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                        {
                            if (file.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                            {
                                await fileStream.CopyToAsync(memoryStream);
                            }
                            if (file.ContentType == "text/csv")
                            {
                                await _excelParser.ConvertCsvToExcel(fileStream, delimiter).CopyToAsync(memoryStream);
                            }

                        }
                        memoryStream.Position = 0;

                        tableHeaders = _excelParser.GetTableHeaders(memoryStream, startRow: startRow, startColumn: startColumn);
                        tableData = _excelParser.GetTableData(memoryStream, startRow: startRow, startColumn: startColumn);

                    }
                    string dropBoxFilePath = $"/ExcelFiles/{DateTime.Now.ToString("dd.MM.yyyy")}";
                    await _dropboxApiClient.UploadFileAsync(tempFilePath, dropBoxFilePath, file.FileName);

                    if (System.IO.File.Exists(tempFilePath))
                    {
                        System.IO.File.Delete(tempFilePath);
                    }

                    HttpContext.Session.SetString("TableHeaders", JsonConvert.SerializeObject(tableHeaders));
                    HttpContext.Session.SetString("TableData", JsonConvert.SerializeObject(tableData));
                    HttpContext.Session.SetString("FilePath", JsonConvert.SerializeObject(dropBoxFilePath));
                    HttpContext.Session.SetString("FileName", JsonConvert.SerializeObject(file.FileName));

                    return RedirectToAction(nameof(PrepaExcelTable));
                }
                catch (Exception ex)
                {
                    TempData["ErorrResult"] = $"Ошибка при загрузке файла.";
                    return RedirectToAction(nameof(Upload));
                }
            }
            TempData["ErorrResult"] = $"Файл не был выбран.";
            return RedirectToAction(nameof(Upload));
        }

        public async Task<IActionResult> PrepaExcelTable()
        {
            var tableHeaders = JsonConvert.DeserializeObject<List<string>>(HttpContext.Session.GetString("TableHeaders"));
            var tableData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(HttpContext.Session.GetString("TableData"));
            var filrPath = HttpContext.Session.GetString("FilePath")?.Trim('"');
            var fileName = HttpContext.Session.GetString("FileName")?.Trim('"');

            var model = new ExelDataViewModel
            {
                TableHeaders = tableHeaders,
                TableData = tableData,
                SavedMappings = await _columnMappingDataServcies.GetColumnMappings(),
                FilePath = filrPath,
                FileName = fileName,
                OzonClients = await _ozonClientServcies.GetOzonClients(),
                Statuses = new SelectList((await _cache.Get()).Select(s => s.Status).Distinct().Select(s => new { Name = s }).ToList().OrderBy(a => a.Name), "Name", "Name"),
                Manufacturers = (await _manufacturerDataService.GetManufacturers()).Where(m => m.Name != null).ToList(),
                Warehouses = await _warehouseDataServcies.GetWarehouses(),
                Suppliers = (await _supplierDataServcies.GetSuppliers()).OrderBy(a => a.Name).ToList(),
                CurrencyCodes = [(CurrencyCode.RUB, "RUB"),
                    (CurrencyCode.USD, "USD"),
                    (CurrencyCode.EUR, "EUR"),
                    (CurrencyCode.BYN, "BYN")],
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CheckFile(ExelDataViewModel model, string saveName, bool notFull)
        {
            model.TableData = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(HttpContext.Session.GetString("TableData"));

            model.TableData = _excelParser.UpdateTableToStandartColumns(model.TableData, model.ColumnMappings);

            if (saveName != null)
            {
                try
                {

                    ColumnMapping newMapping = new ColumnMapping()
                    {
                        MappingName = saveName,
                        ColumnMappings = model.ColumnMappings,
                        SelectedClientId = model.SelectedClient != null && model.SelectedClient.Id != 0 ? model.SelectedClient.Id : null,
                        SelectedCurrencyCode = model.SelectedCurrencyCode,
                        SelectedManufacturerId = model.SelectedManufacturer != null && model.SelectedManufacturer.Id != -1 && model.SelectedManufacturer.Id != 0 ? model.SelectedManufacturer.Id : null,
                        SelectedWarehouseId = model.SelectedWarehouse != null && model.SelectedWarehouse.Id != 0 ? model.SelectedWarehouse.Id : null,
                        SelectedSupplierId = model.SelectedSupplier != null && model.SelectedSupplier.Id != 0 ? model.SelectedSupplier.Id : null,
                        SelectedStatus = model.SelectedStatus,
                        ManufacturerFromArticle = model.SelectedManufacturer != null && model.SelectedManufacturer.Id == -1 ? true : false,
                    };

                    await _columnMappingDataServcies.AddColumnMapping(newMapping);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Произошла ошибка при cохранении состояния сопоставлений столбцов: {ex.Message}");
                }
            }

            List<string> allOzonClients = (await _ozonClientServcies.GetOzonClients()).Select(c => c.Name).ToList();
            List<string> allStatuses = (await _orderServcies.GetOrders()).Select(s => s.Status).Distinct().ToList();
            List<string> allWarehouses = (await _warehouseDataServcies.GetWarehouses()).Select(w => w.Name).ToList();

            List<string> newOzonClients = [];
            List<string> newStatuses = [];
            List<string> newWarehouses = [];

            foreach (var row in model.TableData)
            {
                foreach (var kvp in row)
                {
                    string key = kvp.Key;
                    string value = kvp.Value;

                    if (value != "")
                    {
                        if (key == "Клиент" && !allOzonClients.Contains(value) && !newOzonClients.Contains(value))
                        {
                            newOzonClients.Add(value);
                        }
                        if (key == "Статус клинта" && !allStatuses.Contains(value) && !newStatuses.Contains(value))
                        {
                            newStatuses.Add(value);
                        }
                        if (key == "Склад отгрузки" && !allWarehouses.Contains(value) && !newWarehouses.Contains(value))
                        {
                            newWarehouses.Add(value);
                        }
                    }
                }
            }

            HttpContext.Session.SetString("ExelDataViewModel", JsonConvert.SerializeObject(model));
            HttpContext.Session.SetString("NotFull", JsonConvert.SerializeObject(notFull));

            if (newOzonClients.Count > 0 || newStatuses.Count > 0 || newWarehouses.Count > 0)
            {
                return View(new CheckUniqueValueViewModel()
                {
                    NewOzonClients = newOzonClients,
                    NewStatuses = newStatuses,
                    NewWarehouses = newWarehouses
                });
            }
            else
            {
                return RedirectToAction(nameof(UploadExcelOrders));
            }
        }

        [HttpPost]
        public IActionResult NewValuesProcessing(CheckUniqueValueViewModel newValues)
        {
            var modelJson = HttpContext.Session.GetString("ExelDataViewModel");
            if (modelJson != null)
            {
                ExelDataViewModel model = JsonConvert.DeserializeObject<ExelDataViewModel>(modelJson);

                foreach (var row in model.TableData)
                {

                    foreach (var key in row.Keys.ToList())
                    {
                        string value = row[key];

                        if (newValues.NewOzonClientsDict.TryGetValue(value, out bool isAcceptedOzonClient) && !isAcceptedOzonClient)
                        {
                            row[key] = "Не указан";
                        }

                        else if (newValues.NewStatusesDict.TryGetValue(value, out bool isAcceptedStatus) && !isAcceptedStatus)
                        {
                            row[key] = "Не указан";
                        }
                        else if (newValues.NewWarehousesDict.TryGetValue(value, out bool isAcceptedWarehouse) && !isAcceptedWarehouse)
                        {
                            row[key] = "Не указан";
                        }
                    }
                }

                HttpContext.Session.SetString("ExelDataViewModel", JsonConvert.SerializeObject(model));

                return RedirectToAction(nameof(UploadExcelOrders));
            }

            return RedirectToAction(nameof(UploadExcelOrders));
        }

        public async Task<IActionResult> UploadExcelOrders()
        {

            try
            {
                var notFullJson = HttpContext.Session.GetString("NotFull");
                if (notFullJson != null)
                {
                    bool notFull = JsonConvert.DeserializeObject<bool>(notFullJson);
                    if (notFull)
                    {
                        return RedirectToAction(nameof(SetNotFullOrdersData));
                    }
                }

                var modelJson = HttpContext.Session.GetString("ExelDataViewModel");
                if (modelJson != null)
                {
                    ExelDataViewModel model = JsonConvert.DeserializeObject<ExelDataViewModel>(modelJson);

                    int[] result = [0, 0];

                    List<Order> orders = await _orderCaster.ExcelToOrders(model.TableData,
                                                                          model.SelectedClient,
                                                                          model.SelectedManufacturer,
                                                                          model.SelectedWarehouse,
                                                                          model.SelectedSupplier,
                                                                          model.SelectedStatus,
                                                                          model.SelectedCurrencyCode,
                                                                          model.SelectedShippingDate,
                                                                          model.SelectedProcessingDate);

                    orders = await _orderCaster.SetFileDataAsync(orders, model.FilePath, model.FileName);

                    var uploadResult = await _orderServcies.AddOrders(orders);
                    result[0] += uploadResult[0];
                    result[1] += uploadResult[1];


                    TempData["UploadResult"] = result;
                    await _cacheUpdater.Update(_cache);
                    _duplicateOrdersServcies.DeleteDuplicateOrders();
                    ClearSelectedIdsSession();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Произошла ошибка при загрузке данных: {ex.Message}");
            }
            return RedirectToAction(nameof(IndexV2));
        }

        public async Task<IActionResult> SetNotFullOrdersData()
        {
            var modelJson = HttpContext.Session.GetString("ExelDataViewModel");
            if (modelJson != null)
            {
                ExelDataViewModel model = JsonConvert.DeserializeObject<ExelDataViewModel>(modelJson);
                List<Order> orders = await _orderCaster.ExcelToOrders(model.TableData,
                                                                      model.SelectedClient,
                                                                      model.SelectedManufacturer,
                                                                      model.SelectedWarehouse,
                                                                      model.SelectedSupplier,
                                                                      model.SelectedStatus,
                                                                      model.SelectedCurrencyCode,
                                                                      model.SelectedShippingDate,
                                                                      model.SelectedProcessingDate);

                orders = await _orderCaster.SetFileDataAsync(orders, model.FilePath, model.FileName);
                orders = await _orderServcies.SetNumberInExcel(orders);

                orders = await _orderServcies.CalculateCostPriceForNotFullOrders(orders);

                NotFullOrdersModel notFullOrdersModel = await _orderServcies.GetNotFullOrdersModel(orders);
                List<Order> ordersToUpdateStatus = [];
                string ordersToUpdateStatusInfo = string.Empty;

                foreach (var orderGroup in notFullOrdersModel.OrdersWithMultipleMatches)
                {
                    Order mainOrder = orderGroup.Key;
                    List<Order> matchingOrders = orderGroup.Value;

                    foreach (Order order in matchingOrders)
                    {
                        order.OzonClient = order.OzonClient != null ? await _ozonClientServcies.GetOzonClientAsync(order.OzonClient.Id) : null;
                        if (order.OzonClient != null && order.OzonClient.ClientType == ClientType.OZON)
                        {
                            _jsonDataBuilder.SetClient(order.OzonClient.DecryptClientId, order.OzonClient.DecryptApiKey);

                            string newStatus = await _jsonDataBuilder.GetProductSatatus(order.ShipmentNumber);
                            if (newStatus != string.Empty && order.Status.ToLower() != OzonStatus.OrderStatuses[newStatus].ToLower())
                            {
                                ordersToUpdateStatusInfo += $"{order.ShipmentNumber} : {order.Status} → {OzonStatus.OrderStatuses[newStatus]}<br/>";
                                order.Status = OzonStatus.OrderStatuses[newStatus];
                                ordersToUpdateStatus.Add(order);
                            }
                        }
                    }
                }

                foreach (Order order in notFullOrdersModel.OrdersWithOneMatches.Values)
                {
                    order.OzonClient = order.OzonClient != null ? await _ozonClientServcies.GetOzonClientAsync(order.OzonClient.Id) : null;
                    if (order.OzonClient != null && order.OzonClient.ClientType == ClientType.OZON)
                    {
                        _jsonDataBuilder.SetClient(order.OzonClient.DecryptClientId, order.OzonClient.DecryptApiKey);

                        string newStatus = await _jsonDataBuilder.GetProductSatatus(order.ShipmentNumber);
                        if (newStatus != string.Empty && order.Status.ToLower() != OzonStatus.OrderStatuses[newStatus].ToLower())
                        {
                            ordersToUpdateStatusInfo += $"{order.ShipmentNumber} : {order.Status} → {OzonStatus.OrderStatuses[newStatus]}<br/>";
                            order.Status = OzonStatus.OrderStatuses[newStatus];
                            ordersToUpdateStatus.Add(order);
                        }
                    }
                }
                int[] updateResult = await _orderServcies.AddOrders(ordersToUpdateStatus);
                if (updateResult[1] != 0)
                {
                    ViewData["UpdateStatusResult"] = $"Статусы обновлены в {updateResult[1]} заказах<br>";
                    ViewData["UpdateStatusResultInfo"] = ordersToUpdateStatusInfo;
                }

                var SelectedAppStatus = (await _appStatusServcies.GetAppStatusAsync(new AppStatus() { Name = "Отгружен постащиком" })) != null ?
                                        (await _appStatusServcies.GetAppStatusAsync(new AppStatus() { Name = "Отгружен постащиком" })) :
                                        (await _appStatusServcies.GetAppStatusAsync(new AppStatus() { Name = "Не указан" }));

                return View(new NotFullOrdersViewModel()
                {
                    UniqueOrders = notFullOrdersModel.UniqueOrders,
                    OrdersWithMultipleMatches = notFullOrdersModel.OrdersWithMultipleMatches,
                    OrdersWithOneMatches = notFullOrdersModel.OrdersWithOneMatches,
                    OzonClients = (await _ozonClientServcies.GetOzonClients()).OrderBy(o => o.Name).ToList(),
                    Suppliers = (await _supplierDataServcies.GetSuppliers()).OrderBy(o => o.Name).ToList(),
                    AppStatuses = (await _appStatusServcies.GetAppStatuses()).OrderBy(o => o.Name).ToList(),
                    Manufacturers = (await _manufacturerDataService.GetManufacturers()).OrderBy(o => o.Name).ToList(),
                    Warehouse = (await _warehouseDataServcies.GetWarehouses()).OrderBy(o => o.Name).ToList(),
                    SelectedAppStatus = (await _appStatusServcies.GetAppStatusAsync(new AppStatus() { Name = "Отгружен постащиком" })) != null ?
                                        (await _appStatusServcies.GetAppStatusAsync(new AppStatus() { Name = "Отгружен постащиком" })) :
                                        (await _appStatusServcies.GetAppStatusAsync(new AppStatus() { Name = "Не указан" }))

                });
            }
            else
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetNotFullOrdersData(List<ResultNotFullOrder> NotFullOrders)
        {
            if (NotFullOrders == null || !NotFullOrders.Any())
            {
                return BadRequest("No orders.");
            }

            foreach (var notFullOrder in NotFullOrders)
            {
                await _orderServcies.ProcessingNotFullOrder(notFullOrder.Order, notFullOrder.OrderIds);
            }
            return RedirectToAction("IndexV2");
        }

        public async Task<IActionResult> ViewFile(string filePath, string fileName)
        {

            return Redirect(await _dropboxApiClient.GetViewLinkAsync($"{filePath}/{fileName}"));
        }

        public async Task<IActionResult> Details(int id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var order = await _orderServcies.GetOrder(id);
            if (order == null)
            {
                return NotFound();
            }

            return View(new DetailsOrderViewModel()
            {
                Order = order,
            });
        }

        public async Task<IActionResult> MultiplayEdit(string ids, int page)
        {
            ViewBag.AppStatuses = new SelectList((await _appStatusServcies.GetAppStatuses()).Where(s => s.Name != "Заказан поставщику").OrderBy(a => a.Name), "Id", "Name");

            int[] idArray = ids.Split(',').Select(int.Parse).ToArray();

            List<Order> ordersToEdit = new List<Order>();

            foreach (int id in idArray)
            {
                ordersToEdit.Add(await _orderServcies.GetOrder(id));
            }

            var pageViewModel = new MultiplayEditOrderViewModel()
            {
                Orders = ordersToEdit,
                RedirectPage = page,
                User = await _userManager.GetUserAsync(User),
                Suppliers = (await _supplierDataServcies.GetSuppliers()).OrderBy(s => s.Name).ToList(),
                RateEUR = await _currencyRateFetcher.GetEURRateAsync(),
                RateUSD = await _currencyRateFetcher.GetUSDRateAsync(),
                RateBYN = await _currencyRateFetcher.GetBYNRateAsync(),
            };

            if (pageViewModel.User.UserAccessId != null)
            {
                pageViewModel.User.UserAccess = _context.UserAccess.Find(pageViewModel.User.UserAccessId);
            }
            return View(pageViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MultiplayEdit(List<Order> orders, int redirectPage)
        {
            if (orders == null)
            {
                TempData["ErorrResult"] = $"Не удалось изменить выбранные заказы.<br>Было передано слишком большое количество записей.";
                return RedirectToAction("Index");
            }

            ViewBag.AppStatuses = new SelectList((await _appStatusServcies.GetAppStatuses()).Where(s => s.Name != "Заказан поставщику").OrderBy(a => a.Name), "Id", "Name");

            var pageViewModel = new MultiplayEditOrderViewModel()
            {
                Orders = orders,
                RedirectPage = redirectPage,
                User = await _userManager.GetUserAsync(User),
                Suppliers = (await _supplierDataServcies.GetSuppliers()).OrderBy(s => s.Name).ToList(),
                RateEUR = await _currencyRateFetcher.GetEURRateAsync(),
                RateUSD = await _currencyRateFetcher.GetUSDRateAsync(),
                RateBYN = await _currencyRateFetcher.GetBYNRateAsync(),
            };

            if (pageViewModel.User.UserAccessId != null)
            {
                pageViewModel.User.UserAccess = _context.UserAccess.Find(pageViewModel.User.UserAccessId);
            }

            try
            {
                int editOrdersCount = await _orderServcies.MultiplayEditOrder(orders);
                await _cache.Update();

                ClearSelectedIdsSession();
                return RedirectToAction(nameof(Index), new { sortOrder = GetSortStateCookie(), page = redirectPage });
            }
            catch (Exception ex)
            {
                pageViewModel.ErrorMessage = ex.Message;
                return View(pageViewModel);
            }
        }


        public async Task<IActionResult> MultiplayEditV2(string ids, int page)
        {
            var appStatuses = (await _appStatusServcies.GetAppStatuses()).OrderBy(a => a.Name).ToList();
            var unassignedStatus = appStatuses.FirstOrDefault(s => s.Name == "Не указан");
            if (unassignedStatus != null)
            {
                appStatuses.Remove(unassignedStatus);
                appStatuses.Insert(0, unassignedStatus);
            }
            ViewBag.AppStatuses = new SelectList(appStatuses.Where(s => s.Name != "Заказан поставщику"), "Id", "Name");

            int[] idArray = ids.Split(',').Select(int.Parse).ToArray();

            List<Order> ordersToEdit = new List<Order>();

            foreach (int id in idArray)
            {
                ordersToEdit.Add(await _orderServcies.GetOrder(id));
            }

            var pageViewModel = new MultiplayEditOrderViewModel()
            {
                Orders = ordersToEdit,
                RedirectPage = page,
                User = await _userManager.GetUserAsync(User),
                Suppliers = (await _supplierDataServcies.GetSuppliers()).OrderBy(s => s.Name).ToList(),
                RateEUR = await _currencyRateFetcher.GetEURRateAsync(),
                RateUSD = await _currencyRateFetcher.GetUSDRateAsync(),
                RateBYN = await _currencyRateFetcher.GetBYNRateAsync(),
                UniqueArticles = await _orderServcies.GetUniqueArticles(),
                UniqueDeliveryCitys = await _orderServcies.GetUniqueDeliveryCities(),
                UniqueNumbers = await _orderServcies.GetUniqueShipmentNumbers(),
            };

            if (pageViewModel.User.UserAccessId != null)
            {
                pageViewModel.User.UserAccess = _context.UserAccess.Find(pageViewModel.User.UserAccessId);
            }
            return View(pageViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MultiplayEditV2(List<Order> orders, int redirectPage)
        {
            if (orders == null)
            {
                TempData["ErorrResult"] = $"Не удалось изменить выбранные заказы.<br>Было передано слишком большое количество записей.";
                return RedirectToAction("IndexV2");
            }

            var appStatuses = (await _appStatusServcies.GetAppStatuses()).OrderBy(a => a.Name).ToList();
            var unassignedStatus = appStatuses.FirstOrDefault(s => s.Name == "Не указан");
            if (unassignedStatus != null)
            {
                appStatuses.Remove(unassignedStatus);
                appStatuses.Insert(0, unassignedStatus);
            }
            ViewBag.AppStatuses = new SelectList(appStatuses.Where(s => s.Name != "Заказан поставщику"), "Id", "Name");

            var pageViewModel = new MultiplayEditOrderViewModel()
            {
                Orders = orders,
                RedirectPage = redirectPage,
                User = await _userManager.GetUserAsync(User),
                Suppliers = (await _supplierDataServcies.GetSuppliers()).OrderBy(s => s.Name).ToList(),
                RateEUR = await _currencyRateFetcher.GetEURRateAsync(),
                RateUSD = await _currencyRateFetcher.GetUSDRateAsync(),
                RateBYN = await _currencyRateFetcher.GetBYNRateAsync(),
                UniqueArticles = await _orderServcies.GetUniqueArticles(),
                UniqueDeliveryCitys = await _orderServcies.GetUniqueDeliveryCities(),
                UniqueNumbers = await _orderServcies.GetUniqueShipmentNumbers(),
            };

            if (pageViewModel.User.UserAccessId != null)
            {
                pageViewModel.User.UserAccess = _context.UserAccess.Find(pageViewModel.User.UserAccessId);
            }
            try
            {
                int editOrdersCount = await _orderServcies.MultiplayEditOrder(orders);
                await _cache.Update();

                ClearSelectedIdsSession();
                return RedirectToAction(nameof(IndexV2), new { sortOrder = GetSortStateCookie(), page = redirectPage });
            }
            catch (Exception ex)
            {
                pageViewModel.ErrorMessage = ex.Message;
                return View(pageViewModel);
            }
        }

        public async Task<IActionResult> Delete(int id, int page)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                int result = await _orderServcies.DeleteOrder(id);

                if (result == 0)
                {
                    TempData["ErorrResult"] = $"Для удаления доступны только заказы со статусом '<b>Не указан</b>'.";
                }
                else if (result == 1)
                {
                    await _cache.RemoveOrderFromCache(id);
                    ClearSelectedIdsSession();
                    TempData["TransactionResult"] = $"Заказа успешно удален.";
                }
                else
                {
                    TempData["ErorrResult"] = $"Ошибка при удалении заказа.";
                }
            }
            catch
            {
                TempData["ErorrResult"] = $"Ошибка при удалении заказа.";
            }
            return RedirectToAction(nameof(Index), new { sortOrder = GetSortStateCookie(), page = page });
        }

        public async Task<IActionResult> DeleteV2(int id, int page)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                int result = await _orderServcies.DeleteOrder(id);

                if (result == 0)
                {
                    TempData["ErorrResult"] = $"Для удаления доступны только заказы со статусом '<b>Не указан</b>'.";
                }
                else if (result == 1)
                {
                    await _cache.RemoveOrderFromCache(id);
                    ClearSelectedIdsSession();
                    TempData["TransactionResult"] = $"Заказа успешно удален.";
                }
                else
                {
                    TempData["ErorrResult"] = $"Ошибка при удалении заказа.";
                }
            }
            catch
            {
                TempData["ErorrResult"] = $"Ошибка при удалении заказа.";
            }
            return RedirectToAction(nameof(IndexV2), new { sortOrder = GetSortStateCookie(), page = page });
        }

        public async Task<IActionResult> MultiplayDeleteOrders(string ids, int page)
        {
            if (ids == null)
            {
                TempData["ErorrResult"] = $"Не удалось удалить выбранные заказы.<br>Было передано слишком большое количество записей.";
                return RedirectToAction("Index");
            }

            int[] idArray = ids.Split(',').Select(int.Parse).ToArray();

            try
            {
                int result = await _orderServcies.MultiplayDeleteOrders(idArray);

                if (result == 0)
                {
                    TempData["ErorrResult"] = $"Для удаления доступны только заказы со статусом '<b>Не указан</b>'.";
                }
                else if (result > 0)
                {
                    await _cache.RemoveOrdersFromCache(idArray);
                    ClearSelectedIdsSession();
                    TempData["TransactionResult"] = $"Удалено <b>{result}</b> заказов.";
                }
                else
                {
                    TempData["ErorrResult"] = $"Ошибка при удалении заказа.";
                }
            }
            catch
            {
                TempData["ErorrResult"] = $"Ошибка при удалении заказа.";
            }

            return RedirectToAction(nameof(Index), new { sortOrder = GetSortStateCookie(), page = page });
        }

        public async Task<IActionResult> DeleteOrderPage(List<int> ordersIdsToDelete)
        {
            List<Order> ordersToDelete = [];
            foreach (int id in ordersIdsToDelete)
            {
                ordersToDelete.Add(await _orderServcies.GetOrder(id));
            }
            return View(new DeleteOrderViewModel() { IdsToDelete = ordersIdsToDelete, OrdersToDelete = ordersToDelete });
        }
        public async Task<IActionResult> MultiplayDeleteOrdersV2(string ids, int page)
        {
            if (ids == null)
            {
                TempData["ErorrResult"] = $"Не удалось удалить выбранные заказы.<br>Было передано слишком большое количество записей.";
                return RedirectToAction("Index");
            }

            int[] idArray = ids.Split(',').Select(int.Parse).ToArray();

            try
            {
                int result = await _orderServcies.MultiplayDeleteOrders(idArray);

                if (result == 0)
                {
                    TempData["ErorrResult"] = $"Для удаления доступны только заказы со статусом '<b>Не указан</b>'.";
                }
                else if (result > 0)
                {
                    await _cache.RemoveOrdersFromCache(idArray);
                    ClearSelectedIdsSession();
                    TempData["TransactionResult"] = $"Удалено <b>{result}</b> заказов.";
                }
                else
                {
                    TempData["ErorrResult"] = $"Ошибка при удалении заказа.";
                }
            }
            catch
            {
                TempData["ErorrResult"] = $"Ошибка при удалении заказа.";
            }

            return RedirectToAction(nameof(IndexV2), new { sortOrder = GetSortStateCookie(), page = page });
        }

        public async Task<IActionResult> GetHistory(int id)
        {
            var histList = _orderHistory.GetFullOrderHistory(id);

            return Json(histList);
        }

        [HttpPost]
        public async Task<IActionResult> Recover(int id, DateTime start, DateTime end)
        {
            _orderHistory.OrderRecoverUpdate(id, start, end);
            _cache.UpdateCacheIncrementally(id);
            ClearSelectedIdsSession();
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> ConfirmAccepted(int id, string ids, int page)
        {
            if (ids == null || ids.Length == 0)
            {
                _orderServcies.ConfirmAccepted(id);
                _cache.UpdateCacheIncrementally(id);
            }
            else
            {
                int[] idArray = ids.Split(',').Select(int.Parse).ToArray();
                foreach (var concretId in idArray)
                {
                    _orderServcies.ConfirmAccepted(concretId);
                    _cache.UpdateCacheIncrementally(concretId);
                }
                if (Request.Cookies["selectedIds"] != null)
                {
                    Response.Cookies.Delete("selectedIds");
                }
            }
            return RedirectToAction(nameof(Index), new { sortOrder = GetSortStateCookie(), page = page });
        }

        public async Task<IActionResult> ConfirmAcceptedV2(int id, string ids, int page)
        {
            if (ids == null || ids.Length == 0)
            {
                _orderServcies.ConfirmAccepted(id);
                _cache.UpdateCacheIncrementally(id);
            }
            else
            {
                int[] idArray = ids.Split(',').Select(int.Parse).ToArray();
                foreach (var concretId in idArray)
                {
                    _orderServcies.ConfirmAccepted(concretId);
                    _cache.UpdateCacheIncrementally(concretId);
                }
                if (Request.Cookies["selectedIds"] != null)
                {
                    Response.Cookies.Delete("selectedIds");
                }
            }
            return RedirectToAction(nameof(IndexV2), new { sortOrder = GetSortStateCookie(), page = page });
        }


        public async Task<IActionResult> CreateOrderToSupplierTransaction(string ids, int page)
        {
            var filteredStatuses = _context.AppStatuses.Where(status => status.Name == "Отменен").ToList();
            ViewBag.AppStatuses = new SelectList(filteredStatuses, "Id", "Name");

            int[] idArray = ids.Split(',').Select(int.Parse).ToArray();
            List<Order> ordersToTransaction = new List<Order>();


            var appStatus = await _appStatusServcies.GetAppStatusAsync(new AppStatus() { Name = "Заказан поставщику" });

            if (appStatus == null)
            {
                AppStatus newStatus = new AppStatus()
                {
                    Name = "Заказан поставщику"
                };
                _context.AppStatuses.Add(newStatus);
                await _context.SaveChangesAsync();
            }

            foreach (var orderId in idArray)
            {
                var concretOrder = await _orderServcies.GetOrder(orderId);

                if (concretOrder.AppStatus.Name != "Заказан поставщику" && concretOrder.Status != "Отменен покупателем")
                {
                    concretOrder.AppStatus = appStatus;
                    ordersToTransaction.Add(concretOrder);
                }

            }

            var pageViewModel = new MultiplayEditOrderViewModel()
            {
                RedirectPage = page,
                Orders = ordersToTransaction.OrderBy(ot => ot.ShipmentNumber).ToList(),
                User = await _userManager.GetUserAsync(User),
                Suppliers = (await _supplierDataServcies.GetSuppliers()).OrderBy(s => s.Name).ToList(),
                RateEUR = await _currencyRateFetcher.GetEURRateAsync(),
                RateUSD = await _currencyRateFetcher.GetUSDRateAsync(),
                RateBYN = await _currencyRateFetcher.GetBYNRateAsync(),
            };

            if (pageViewModel.User.UserAccessId != null)
            {
                pageViewModel.User.UserAccess = _context.UserAccess.Find(pageViewModel.User.UserAccessId);
            }

            return View(pageViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrderToSupplierTransaction(List<Order> orders, string userName, string comment, int page, string deletedOrders)
        {
            if (orders == null)
            {
                TempData["ErorrResult"] = $"Не удалось провести выбранные заказы.<br>Было передано слишком большое количество записей.";
                return RedirectToAction("Index");
            }
            else
            {
                if (deletedOrders != null)
                {
                    var deletedOrderIds = deletedOrders.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                    orders = orders.Where(o => !deletedOrderIds.Contains(o.Id)).ToList();
                }
            }

            try
            {
                if (orders.Count != 0)
                {
                    DateTime createAt = DateTime.Now;
                    var changeCount = 0;
                    var dateTime = "";
                    List<Order> ordersToUpdate = new List<Order>();
                    foreach (var order in orders)
                    {
                        ordersToUpdate.Add(await _orderServcies.TransactOrder(order));
                    }

                    var appStatus = await _appStatusServcies.GetAppStatusAsync(new AppStatus() { Name = "Заказан поставщику" });
                    List<Order> ordersToTransaction = ordersToUpdate.Where(o => o.AppStatus.Id == appStatus.Id).ToList();

                    (changeCount, dateTime) = await _orderToSupplierTransaction.CreateTransaction(ordersToTransaction,
                                                                                                  userName,
                                                                                                  createAt,
                                                                                                  comment);
                    int cancelCount = ordersToUpdate.Where(o => o.AppStatus.Name == "Отменен").Count();

                    if (changeCount > 0)
                    {
                        await _cacheUpdater.Update(_cache);
                        TempData["TransactionResult"] = $"Заказы добавлены в журнал<br/>" +
                                                        $"<b>{changeCount}</b> заказам изменен статус на '<b>Заказан поставщику</b>', <b>{cancelCount}</b> заказов были отменены." +
                                                        $"<br/>Время<b>: {dateTime}</b>" +
                                                        $"<br/>Пользователь<b>: {userName}</b>" +
                                                        $"<br/>Комментарий: {comment}";
                    }

                    await _transactionCache.Update();
                }

                ClearSelectedIdsSession();
                return Json(new { redirectTo = Url.Action(nameof(Index), new { sortOrder = GetSortStateCookie(), page = page }) });
            }
            catch (Exception ex)
            {
                TempData["ErorrResult"] = $"Не удалось провести выбранные заказы ({ex.Message})";
                return Json(new { redirectTo = Url.Action(nameof(Index), new { sortOrder = GetSortStateCookie(), page = page }) });
            }
        }

        public async Task<IActionResult> CreateOrderToSupplierTransactionV2(string ids, int page)
        {
            var filteredStatuses = _context.AppStatuses.Where(status => status.Name == "Отменен").ToList();
            ViewBag.AppStatuses = new SelectList(filteredStatuses, "Id", "Name");

            int[] idArray = ids.Split(',').Select(int.Parse).ToArray();
            List<Order> ordersToTransaction = new List<Order>();


            var appStatus = await _appStatusServcies.GetAppStatusAsync(new AppStatus() { Name = "Заказан поставщику" });

            if (appStatus == null)
            {
                AppStatus newStatus = new AppStatus()
                {
                    Name = "Заказан поставщику"
                };
                _context.AppStatuses.Add(newStatus);
                await _context.SaveChangesAsync();
            }

            foreach (var orderId in idArray)
            {
                var concretOrder = await _orderServcies.GetOrder(orderId);

                if (concretOrder.AppStatus.Name != "Заказан поставщику" && concretOrder.Status != "Отменен покупателем")
                {
                    concretOrder.AppStatus = appStatus;
                    string code = concretOrder.Article;
                    string brand = await _interpartsApiDataManager.GetbBrandByCode(code);
                    if(brand != null)
                    {
                        concretOrder.SupplierNameAndDirection = await _interpartsApiDataManager.GetbSupplierNameAndDirection(code, brand);
                    }
                    ordersToTransaction.Add(concretOrder);
                }

            }

            var pageViewModel = new MultiplayEditOrderViewModel()
            {
                RedirectPage = page,
                Orders = ordersToTransaction.OrderBy(ot => ot.ShipmentNumber).ToList(),
                User = await _userManager.GetUserAsync(User),
                Suppliers = (await _supplierDataServcies.GetSuppliers()).OrderBy(s => s.Name).ToList(),
                RateEUR = await _currencyRateFetcher.GetEURRateAsync(),
                RateUSD = await _currencyRateFetcher.GetUSDRateAsync(),
                RateBYN = await _currencyRateFetcher.GetBYNRateAsync(),
                UniqueArticles = await _orderServcies.GetUniqueArticles(),
                UniqueDeliveryCitys = await _orderServcies.GetUniqueDeliveryCities(),
                UniqueNumbers = await _orderServcies.GetUniqueShipmentNumbers(),
            };

            if (pageViewModel.User.UserAccessId != null)
            {
                pageViewModel.User.UserAccess = _context.UserAccess.Find(pageViewModel.User.UserAccessId);
            }

            return View(pageViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrderToSupplierTransactionV2(List<Order> orders, string userName, string comment, int page, string deletedOrders)
        {
            if (orders == null)
            {
                TempData["ErorrResult"] = $"Не удалось провести выбранные заказы.<br>Было передано слишком большое количество записей.";
                return RedirectToAction("Index");
            }
            else
            {
                if (deletedOrders != null)
                {
                    var deletedOrderIds = deletedOrders.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                    orders = orders.Where(o => !deletedOrderIds.Contains(o.Id)).ToList();
                }
            }

            try
            {
                if (orders.Count != 0)
                {
                    DateTime createAt = DateTime.Now;
                    var changeCount = 0;
                    var dateTime = "";
                    List<Order> ordersToUpdate = new List<Order>();
                    List<string> ordersNotFoundInOzone = [];
                    foreach (var order in orders)
                    {
                        order.OzonClient = await _ozonClientServcies.GetOzonClientAsync(order.OzonClient.Id);
                        if (order.OzonClient != null && order.OzonClient.ClientType == ClientType.OZON)
                        {
                            _jsonDataBuilder.SetClient(order.OzonClient.DecryptClientId, order.OzonClient.DecryptApiKey);
                            if (await _jsonDataBuilder.CheckOrderForExistence(order.ShipmentNumber))
                            {
                                ordersToUpdate.Add(await _orderServcies.TransactOrder(order));
                            }
                            else
                            {
                                ordersNotFoundInOzone.Add(order.ShipmentNumber);
                            }
                        }
                        else
                        {
                            ordersToUpdate.Add(await _orderServcies.TransactOrder(order));
                        }
                    }

                    var appStatus = await _appStatusServcies.GetAppStatusAsync(new AppStatus() { Name = "Заказан поставщику" });
                    List<Order> ordersToTransaction = ordersToUpdate.Where(o => o.AppStatus.Id == appStatus.Id).ToList();

                    (changeCount, dateTime) = await _orderToSupplierTransaction.CreateTransaction(ordersToTransaction,
                                                                                                  userName,
                                                                                                  createAt,
                                                                                                  comment);
                    int cancelCount = ordersToUpdate.Where(o => o.AppStatus.Name == "Отменен").Count();

                    if (changeCount > 0)
                    {
                        await _cacheUpdater.Update(_cache);
                        TempData["TransactionResult"] = $"Заказы добавлены в журнал<br/>" +
                                                        $"<b>{changeCount}</b> заказам изменен статус на '<b>Заказан поставщику</b>', <b>{cancelCount}</b> заказов были отменены." +
                                                        $"<br/>Время<b>: {dateTime}</b>" +
                                                        $"<br/>Пользователь<b>: {userName}</b>" +
                                                        $"<br/>Комментарий: {comment}";

                    }
                    await _transactionCache.Update();

                    if (ordersNotFoundInOzone.Count > 0)
                    {
                        TempData["OrdersNotFoundInOzone"] = $"Заказы не найдены в системе Ozon - {ordersNotFoundInOzone.Aggregate((x, y) => x + ", " + y)}";
                    }
                }

                ClearSelectedIdsSession();
                return Json(new { redirectTo = Url.Action(nameof(IndexV2), new { sortOrder = GetSortStateCookie(), page = page }) });
            }
            catch (Exception ex)
            {
                TempData["ErorrResult"] = $"Не удалось провести выбранные заказы ({ex.Message})";
                return Json(new { redirectTo = Url.Action(nameof(IndexV2), new { sortOrder = GetSortStateCookie(), page = page }) });

            }

        }

        public async Task<IActionResult> PrintOrdersExcel(string ids, int page)
        {
            int[] idArray = ids.Split(',').Select(int.Parse).ToArray();
            List<Order> ordersToExcel = new List<Order>();

            try
            {
                foreach (var orderId in idArray)
                {
                    ordersToExcel.Add(await _orderServcies.GetOrder(orderId));
                }

                var user = await _userManager.GetUserAsync(User);

                UserAccess userColumns = await _userAccessDataServices.GetUserAccessAsync(user.UserAccessId.Value);

                byte[] excelFile = _excelExporter.ExportToExcel(ordersToExcel, userColumns.AvailableOrderColumns);
                ClearSelectedIdsSession();

                return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Orders_{DateTime.Now:dd.MM.yyyy}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["ErorrResult"] = $"Не удалось распечатать выделенные заказы<br/>";
                return RedirectToAction(nameof(Index), new { sortOrder = GetSortStateCookie(), page = page });
            }
        }

        public async Task<IActionResult> PrintOrdersExcelV2(string ids, int page)
        {
            int[] idArray = ids.Split(',').Select(int.Parse).ToArray();
            List<Order> ordersToExcel = new List<Order>();

            try
            {
                foreach (var orderId in idArray)
                {
                    ordersToExcel.Add(await _orderServcies.GetOrder(orderId));
                }

                var user = await _userManager.GetUserAsync(User);

                UserAccess userColumns = await _userAccessDataServices.GetUserAccessAsync(user.UserAccessId.Value);

                byte[] excelFile = _excelExporter.ExportToExcel(ordersToExcel, userColumns.AvailableOrderColumns);
                ClearSelectedIdsSession();

                return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Orders_{DateTime.Now:dd.MM.yyyy}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["ErorrResult"] = $"Не удалось распечатать выделенные заказы<br/>";
                return RedirectToAction(nameof(Index), new { sortOrder = GetSortStateCookie(), page = page });
            }
        }

        public async Task<IActionResult> PrintOrdersLable(string ids, int page)
        {
            List<OzonClient> clients = await _ozonClientServcies.GetOzonClients();
            int[] idArray = ids.Split(',').Select(int.Parse).ToArray();
            List<Order> ordersLables = new List<Order>();
            Dictionary<string, byte[]> lables = new Dictionary<string, byte[]>();
            int continueLablesCuont = 0;

            foreach (var orderId in idArray)
            {
                ordersLables.Add(await _orderServcies.GetOrder(orderId));
            }

            foreach (var order in ordersLables)
            {
                try
                {
                    var client = clients.FirstOrDefault(c => c.ClientId == order.OzonClient.ClientId);
                    _jsonDataBuilder.SetClient(client.DecryptClientId, client.DecryptApiKey);
                    lables.Add(order.ShipmentNumber, await _jsonDataBuilder.GetOrderLabel(order.ShipmentNumber));
                }
                catch (Exception ex)
                {
                    continueLablesCuont++;
                    continue;
                }
            }

            var result = SendFilesToUser(lables);
            if (result == null)
            {
                if (Request.Cookies["selectedIds"] != null)
                {
                    Response.Cookies.Delete("selectedIds");
                }
                TempData["ErorrResult"] = $"Не удалось распечатать этикетку для заказов: {string.Join(", ", ordersLables.Select(o => o.ShipmentNumber))}<br/>" +
                                          $"Печать этикетки возможна только для заказов со статусом <b>'Ожидает отгрузки'</b>";
                return RedirectToAction(nameof(Index), new { sortOrder = GetSortStateCookie(), page = page });
            }
            ClearSelectedIdsSession();
            return result;
        }

        public async Task<IActionResult> PrintOrdersLableV2(string ids, int page)
        {
            List<OzonClient> clients = await _ozonClientServcies.GetOzonClients();
            int[] idArray = ids.Split(',').Select(int.Parse).ToArray();
            List<Order> ordersLables = new List<Order>();
            Dictionary<string, byte[]> lables = new Dictionary<string, byte[]>();
            int continueLablesCuont = 0;

            foreach (var orderId in idArray)
            {
                ordersLables.Add(await _orderServcies.GetOrder(orderId));
            }

            foreach (var order in ordersLables)
            {
                try
                {
                    var client = clients.FirstOrDefault(c => c.ClientId == order.OzonClient.ClientId);
                    _jsonDataBuilder.SetClient(client.DecryptClientId, client.DecryptApiKey);
                    lables.Add(order.ShipmentNumber, await _jsonDataBuilder.GetOrderLabel(order.ShipmentNumber));
                }
                catch (Exception ex)
                {
                    continueLablesCuont++;
                    continue;
                }
            }

            var result = SendFilesToUser(lables);
            if (result == null)
            {
                if (Request.Cookies["selectedIds"] != null)
                {
                    Response.Cookies.Delete("selectedIds");
                }
                TempData["ErorrResult"] = $"Не удалось распечатать этикетку для заказов: {string.Join(", ", ordersLables.Select(o => o.ShipmentNumber))}<br/>" +
                                          $"Печать этикетки возможна только для заказов со статусом <b>'Ожидает отгрузки'</b>";
                return RedirectToAction(nameof(IndexV2), new { sortOrder = GetSortStateCookie(), page = page });
            }
            ClearSelectedIdsSession();
            return result;
        }

        private IActionResult SendFilesToUser(Dictionary<string, byte[]> files)
        {
            List<FileContentResult> fileResults = new List<FileContentResult>();

            foreach (var file in files)
            {
                var fileResult = new FileContentResult(file.Value, "application/pdf")
                {
                    FileDownloadName = $"{file.Key}.pdf"
                };

                fileResults.Add(fileResult);
            }

            if (fileResults.Count == 0)
            {
                return null;
            }

            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDirectory);

            List<string> tempFiles = new List<string>();
            foreach (var fileResult in fileResults)
            {
                string tempFilePath = Path.Combine(tempDirectory, fileResult.FileDownloadName);
                System.IO.File.WriteAllBytes(tempFilePath, fileResult.FileContents);
                tempFiles.Add(tempFilePath);
            }

            string zipFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
            ZipFile.CreateFromDirectory(tempDirectory, zipFilePath);

            Directory.Delete(tempDirectory, true);

            return PhysicalFile(zipFilePath, "application/zip", $"labels_{DateTime.Now:dd.MM.yyyy}.zip");
        }


        [Authorize(Roles = "Admin")]
        public IActionResult DeleteDuplicateOrders()
        {
            int? duplicateCount;
            int? deletedRowsCount;

            (duplicateCount, deletedRowsCount) = _duplicateOrdersServcies.DeleteDuplicateOrders();
            ClearSelectedIdsSession();
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> OrderExists(int id)
        {
            return await _orderServcies.GetOrder(id) != null;
        }


        public void SetSortOrderViewData(OrderSortState sortOrder)
        {
            ViewData["ShipmentNumberSort"] = sortOrder == OrderSortState.ShipmentNumberAsc ? OrderSortState.ShipmentNumberDesc : OrderSortState.ShipmentNumberAsc;
            ViewData["ProcessingDateSort"] = sortOrder == OrderSortState.ProcessingDateAsc ? OrderSortState.ProcessingDateDesc : OrderSortState.ProcessingDateAsc;
            ViewData["ShippingDateSort"] = sortOrder == OrderSortState.ShippingDateAsc ? OrderSortState.ShippingDateDesc : OrderSortState.ShippingDateAsc;
            ViewData["StatusSort"] = sortOrder == OrderSortState.StatusAsc ? OrderSortState.StatusDesc : OrderSortState.StatusAsc;
            ViewData["CurrentPriceSort"] = sortOrder == OrderSortState.CurrentPriceAsc ? OrderSortState.CurrentPriceDesc : OrderSortState.CurrentPriceAsc;
            ViewData["AppStatusIdSort"] = sortOrder == OrderSortState.AppStatusIdAsc ? OrderSortState.AppStatusIdDesc : OrderSortState.AppStatusIdAsc;
            ViewData["ShipmentAmountSort"] = sortOrder == OrderSortState.ShipmentAmountAsc ? OrderSortState.ShipmentAmountDesc : OrderSortState.ShipmentAmountAsc;
            ViewData["ProductNameSort"] = sortOrder == OrderSortState.ProductNameAsc ? OrderSortState.ProductNameDesc : OrderSortState.ProductNameAsc;
            ViewData["ArticleSort"] = sortOrder == OrderSortState.ArticleAsc ? OrderSortState.ArticleDesc : OrderSortState.ArticleAsc;
            ViewData["PriceSort"] = sortOrder == OrderSortState.PriceAsc ? OrderSortState.PriceDesc : OrderSortState.PriceAsc;
            ViewData["QuantitySort"] = sortOrder == OrderSortState.QuantityAsc ? OrderSortState.QuantityDesc : OrderSortState.QuantityAsc;
            ViewData["ShipmentWarehouseSort"] = sortOrder == OrderSortState.ShipmentWarehouseIdAsc ? OrderSortState.ShipmentWarehouseIdDesc : OrderSortState.ShipmentWarehouseIdAsc;
            ViewData["SupplierSort"] = sortOrder == OrderSortState.SupplierAsc ? OrderSortState.SupplierDesc : OrderSortState.SupplierAsc;
            ViewData["PurchasePriceSort"] = sortOrder == OrderSortState.PurchasePriceAsc ? OrderSortState.PurchasePriceDesc : OrderSortState.PurchasePriceAsc;
            ViewData["ProductInfoIdSort"] = sortOrder == OrderSortState.ProductInfoIdAsc ? OrderSortState.ProductInfoIdDesc : OrderSortState.ProductInfoIdAsc;
            ViewData["OzonCommissionSort"] = sortOrder == OrderSortState.MinOzonCommissionAsc ? OrderSortState.MinOzonCommissionDesc : OrderSortState.MinOzonCommissionAsc;
            ViewData["VolumeSort"] = sortOrder == OrderSortState.VolumeAsc ? OrderSortState.VolumeDesc : OrderSortState.VolumeAsc;
            ViewData["ProfitSort"] = sortOrder == OrderSortState.ProfitAsc ? OrderSortState.ProfitDesc : OrderSortState.ProfitAsc;
            ViewData["DiscountSort"] = sortOrder == OrderSortState.DiscountAsc ? OrderSortState.DiscountDesc : OrderSortState.DiscountAsc;
            ViewData["DeliveryCitySort"] = sortOrder == OrderSortState.DeliveryCityAsc ? OrderSortState.DeliveryCityDesc : OrderSortState.DeliveryCityAsc;
            ViewData["CategorySort"] = sortOrder == OrderSortState.CategoryAsc ? OrderSortState.CategoryDesc : OrderSortState.CategoryAsc;
            ViewData["OrderNumberToSupplierSort"] = sortOrder == OrderSortState.OrderNumberToSupplierAsc ? OrderSortState.OrderNumberToSupplierDesc : OrderSortState.OrderNumberToSupplierAsc;
            ViewData["OzonClient"] = sortOrder == OrderSortState.OzonClientAsc ? OrderSortState.OzonClientDesc : OrderSortState.OzonClientAsc;
            ViewData["ManufacturerSort"] = sortOrder == OrderSortState.ManufacturerAsc ? OrderSortState.ManufacturerDesc : OrderSortState.ManufacturerAsc;
            ViewData["FromFileSort"] = sortOrder == OrderSortState.FromFileAsc ? OrderSortState.FromFileDesc : OrderSortState.FromFileAsc;
            ViewData["DeliveryPeriodSort"] = sortOrder == OrderSortState.DeliveryPeriodAsc ? OrderSortState.DeliveryPeriodDesc : OrderSortState.DeliveryPeriodAsc;
            ViewData["CostPriceсеSort"] = sortOrder == OrderSortState.CostPriceAsc ? OrderSortState.CostPriceDesc : OrderSortState.CostPriceAsc;
            ViewData["TimeLeftSort"] = sortOrder == OrderSortState.TimeLeftAsc ? OrderSortState.TimeLeftDesc : OrderSortState.TimeLeftAsc;

        }

        public async Task<IEnumerable<Order>> ApplySortOrder(IEnumerable<Order> orders, OrderSortState sortOrder)
        {
            return sortOrder switch
            {
                OrderSortState.ShipmentNumberAsc => orders.OrderBy(o => o.ShipmentNumber),
                OrderSortState.ShipmentNumberDesc => orders.OrderByDescending(o => o.ShipmentNumber),

                OrderSortState.ProcessingDateAsc => orders.OrderBy(o => o.ProcessingDate),
                OrderSortState.ProcessingDateDesc => orders.OrderByDescending(o => o.ProcessingDate),

                OrderSortState.ShippingDateAsc => orders.OrderBy(o => o.ShippingDate),
                OrderSortState.ShippingDateDesc => orders.OrderByDescending(o => o.ShippingDate),

                OrderSortState.StatusAsc => orders.OrderBy(o => o.Status),
                OrderSortState.StatusDesc => orders.OrderByDescending(o => o.Status),

                OrderSortState.AppStatusIdAsc => orders.OrderBy(o => o.AppStatus.Name),
                OrderSortState.AppStatusIdDesc => orders.OrderByDescending(o => o.AppStatus.Name),

                OrderSortState.ShipmentAmountAsc => orders.OrderBy(o => o.ShipmentAmount),
                OrderSortState.ShipmentAmountDesc => orders.OrderByDescending(o => o.ShipmentAmount),

                OrderSortState.ProductNameAsc => orders.OrderBy(o => o.ProductName),
                OrderSortState.ProductNameDesc => orders.OrderByDescending(o => o.ProductName),

                OrderSortState.ArticleAsc => orders.OrderBy(o => o.ProductKey),
                OrderSortState.ArticleDesc => orders.OrderByDescending(o => o.ProductKey),

                OrderSortState.PriceAsc => orders.OrderBy(o => o.Price),
                OrderSortState.PriceDesc => orders.OrderByDescending(o => o.Price),

                OrderSortState.QuantityAsc => orders.OrderBy(o => o.Quantity),
                OrderSortState.QuantityDesc => orders.OrderByDescending(o => o.Quantity),

                OrderSortState.ShipmentWarehouseIdAsc => orders.OrderBy(o => o.ShipmentWarehouseId),
                OrderSortState.ShipmentWarehouseIdDesc => orders.OrderByDescending(o => o.ShipmentWarehouseId),

                OrderSortState.СurrencyIdAsc => orders.OrderBy(o => o.СurrencyId),
                OrderSortState.СurrencyIdDesc => orders.OrderByDescending(o => o.СurrencyId),

                OrderSortState.SupplierAsc => orders.OrderBy(o => o.Supplier.Name),
                OrderSortState.SupplierDesc => orders.OrderByDescending(o => o.Supplier.Name),

                OrderSortState.PurchasePriceAsc => orders.OrderBy(o => o.PurchasePrice),
                OrderSortState.PurchasePriceDesc => orders.OrderByDescending(o => o.PurchasePrice),

                OrderSortState.ProductInfoIdAsc => orders.OrderBy(o => o.ProductInfoId),
                OrderSortState.ProductInfoIdDesc => orders.OrderByDescending(o => o.ProductInfoId),

                OrderSortState.MinOzonCommissionAsc => orders.OrderBy(o => o.MinOzonCommission),
                OrderSortState.MinOzonCommissionDesc => orders.OrderByDescending(o => o.MinOzonCommission),

                OrderSortState.MaxOzonCommissionAsc => orders.OrderBy(o => o.MaxOzonCommission),
                OrderSortState.MaxOzonCommissionDesc => orders.OrderByDescending(o => o.MaxOzonCommission),

                OrderSortState.ProfitAsc => orders.OrderBy(o => o.MinProfit),
                OrderSortState.ProfitDesc => orders.OrderByDescending(o => o.MinProfit),

                OrderSortState.CurrentPriceAsc => orders.OrderBy(o => o.ProductInfo.CurrentPriceWithDiscount),
                OrderSortState.CurrentPriceDesc => orders.OrderByDescending(o => o.ProductInfo.CurrentPriceWithDiscount),

                OrderSortState.VolumeAsc => orders.OrderBy(o => o.ProductInfo.VolumetricWeight),
                OrderSortState.VolumeDesc => orders.OrderByDescending(o => o.ProductInfo.VolumetricWeight),

                OrderSortState.DiscountAsc => orders.OrderBy(o => o.MinDiscount),
                OrderSortState.DiscountDesc => orders.OrderByDescending(o => o.MinDiscount),

                OrderSortState.DeliveryCityAsc => orders.OrderBy(o => o.DeliveryCity),
                OrderSortState.DeliveryCityDesc => orders.OrderByDescending(o => o.DeliveryCity),

                OrderSortState.CategoryAsc => orders.OrderBy(o => o.NewCategory),
                OrderSortState.CategoryDesc => orders.OrderByDescending(o => o.NewCategory),

                OrderSortState.OrderNumberToSupplierAsc => orders.OrderBy(o => o.OrderNumberToSupplier),
                OrderSortState.OrderNumberToSupplierDesc => orders.OrderByDescending(o => o.OrderNumberToSupplier),

                OrderSortState.OzonClientAsc => orders.OrderBy(o => o.OzonClient?.Name ?? string.Empty),
                OrderSortState.OzonClientDesc => orders.OrderByDescending(o => o.OzonClient?.Name ?? string.Empty),

                OrderSortState.ManufacturerAsc => orders.OrderBy(o => o.Manufacturer?.Name ?? string.Empty),
                OrderSortState.ManufacturerDesc => orders.OrderByDescending(o => o.Manufacturer?.Name ?? string.Empty),

                OrderSortState.FromFileAsc => await _dataFilterManager.FilterFromFile(true),
                OrderSortState.FromFileDesc => await _dataFilterManager.FilterFromFile(false),

                OrderSortState.DeliveryPeriodAsc => orders.OrderBy(x => x.DeliveryPeriodDay),
                OrderSortState.DeliveryPeriodDesc => orders.OrderByDescending(x => x.DeliveryPeriodDay),

                OrderSortState.CostPriceAsc => orders.OrderBy(o => o.CostPrice),
                OrderSortState.CostPriceDesc => orders.OrderByDescending(o => o.CostPrice),

                OrderSortState.TimeLeftAsc => orders.OrderBy(o => o.TimeLeftDay),
                OrderSortState.TimeLeftDesc => orders.OrderByDescending(o => o.TimeLeftDay),

                _ => orders
            };
        }


        public async Task SetFilterLists(List<Order> data)
        {
            var appStatuses = (await _appStatusServcies.GetAppStatuses()).OrderBy(a => a.Name).ToList();
            var unassignedStatus = appStatuses.FirstOrDefault(s => s.Name == "Не указан");
            if (unassignedStatus != null)
            {
                appStatuses.Remove(unassignedStatus);
                appStatuses.Insert(0, unassignedStatus);
            }

            ViewBag.AppStatuses = new SelectList(appStatuses, "Name", "Name");
            ViewBag.Suppliers = new SelectList((await _supplierDataServcies.GetSuppliers()).OrderBy(a => a.Name), "Name", "Name");
            ViewBag.Clients = new SelectList((await _ozonClientServcies.GetOzonClients()).OrderBy(a => a.Name), "Name", "Name");
            ViewBag.Warehouses = new SelectList((await _warehouseDataServcies.GetWarehouses()).OrderBy(a => a.Name), "Name", "Name");
            ViewBag.Statuses = new SelectList(data.Select(s => s.Status).Distinct().Select(s => new { Name = s }).ToList().OrderBy(a => a.Name), "Name", "Name");
        }

        public void SaveSortStateCookie(OrderSortState sortOrder)
        {
            if (sortOrder != OrderSortState.StandardState)
            {
                Response.Cookies.Delete("SortState");
                Response.Cookies.Append("SortState", sortOrder.ToString());
            }
        }

        private OrderSortState GetSortStateCookie()
        {
            var sortStateCookie = Request.Cookies["SortState"];
            if (!string.IsNullOrEmpty(sortStateCookie) && Enum.TryParse<OrderSortState>(sortStateCookie, out var savedSortState))
            {
                return savedSortState;
            }
            return OrderSortState.StandardState;
        }

        private void SetInfoMessage()
        {
            if (TempData.ContainsKey("UploadResult") && TempData["UploadResult"] != null)
            {
                int[] result = (int[])TempData["UploadResult"];
                string period = (string)TempData["UploadResultPeriod"];
                ViewData["UploadResult"] = result;
                ViewData["UploadResultPeriod"] = period;
            }
            if (TempData.ContainsKey("TransactionResult") && TempData["TransactionResult"] != null)
            {
                string result = (string)TempData["TransactionResult"];
                ViewData["TransactionResult"] = result;
            }
            if (TempData.ContainsKey("ErorrResult") && TempData["ErorrResult"] != null)
            {
                string result = (string)TempData["ErorrResult"];
                ViewData["ErorrResult"] = result;
            }
            if (TempData.ContainsKey("OrdersNotFoundInOzone") && TempData["OrdersNotFoundInOzone"] != null)
            {
                string result = (string)TempData["OrdersNotFoundInOzone"];
                ViewData["OrdersNotFoundInOzone"] = result;
            }
        }

        //Session Ids
        [HttpPost]
        public IActionResult AddIdsToSession([FromBody] List<int> ids)
        {
            List<int> selectedIds = RetrieveSelectedIdsFromSession();
            foreach (var id in ids)
            {
                if (!selectedIds.Contains(id))
                {
                    selectedIds.Add(id);
                }
            }
            HttpContext.Session.SetString("selectedIds", JsonConvert.SerializeObject(selectedIds));
            return Ok();
        }

        [HttpPost]
        public IActionResult RemoveIdsFromSession([FromBody] List<int> ids)
        {
            List<int> selectedIds = RetrieveSelectedIdsFromSession();
            selectedIds = selectedIds.Where(id => !ids.Contains(id)).ToList();
            HttpContext.Session.SetString("selectedIds", JsonConvert.SerializeObject(selectedIds));
            return Ok();
        }

        [HttpPost]
        public IActionResult AddIdToSession(int id)
        {
            List<int> selectedIds = RetrieveSelectedIdsFromSession();
            if (!selectedIds.Contains(id))
            {
                selectedIds.Add(id);
                HttpContext.Session.SetString("selectedIds", JsonConvert.SerializeObject(selectedIds));
            }
            return Ok();
        }

        [HttpPost]
        public IActionResult RemoveIdFromSession(int id)
        {
            List<int> selectedIds = RetrieveSelectedIdsFromSession();
            if (selectedIds.Contains(id))
            {
                selectedIds.Remove(id);
                HttpContext.Session.SetString("selectedIds", JsonConvert.SerializeObject(selectedIds));
            }
            return Ok();
        }

        [HttpGet]
        public IActionResult GetSelectedIdsFromSession()
        {
            List<int> selectedIds = RetrieveSelectedIdsFromSession();
            return Json(selectedIds);
        }

        private List<int> RetrieveSelectedIdsFromSession()
        {
            var selectedIds = HttpContext.Session.GetString("selectedIds");
            if (string.IsNullOrEmpty(selectedIds))
            {
                return new List<int>();
            }
            return JsonConvert.DeserializeObject<List<int>>(selectedIds);
        }

        [HttpPost]
        public IActionResult ClearSelectedIdsSession()
        {
            // Заменяем список ID на пустой
            HttpContext.Session.SetString("selectedIds", JsonConvert.SerializeObject(new List<int>()));
            return Ok();
        }


    }
}
