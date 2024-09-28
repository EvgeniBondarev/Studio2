using Hangfire;
using Hangfire.Storage.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OzonDomains;
using OzonDomains.Models;
using OzonOrdersWeb.ViewModels;
using OzonOrdersWeb.ViewModels.JobViewModels;
using Servcies.ApiServcies.OzonApi;
using Servcies.ApiServcies.YandexApi;
using Servcies.DataServcies;
using Servcies.ParserServcies;
using Servcies.ReleasServcies.ReleaseManager;
using Services.CacheServcies.Cache;
using Services.CacheServcies.Cache.OzonOrdersCache;

namespace OzonOrdersWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class JobController : Controller
    {
        private readonly OrdersDataServcies _orderRepository;
        private readonly OrderCaster _orderCaster;
        private readonly OzonJsonDataBuilder _jsonDataBuilder;
        private readonly ReleaseManager _releaseManager;
        private readonly DuplicateOrdersServcies _duplicateOrdersServcies;
        private readonly OzonClientServcies _ozonClientServcies;
        private readonly YandexDataManager _yandexDataManager;

        public JobController(OrdersDataServcies ordersDataServcies,
                              OrderCaster orderCaster,
                              OzonJsonDataBuilder jsonDataBuilder,
                              ReleaseManager releaseManager,
                              DuplicateOrdersServcies duplicateOrdersServcies,
                              OrderCache orderCache,
                              CacheUpdater<Order> cacheUpdater,
                              OzonClientServcies ozonClientServcies,
                              YandexDataManager yandexDataManager)
        {
            _orderRepository = ordersDataServcies;
            _orderCaster = orderCaster;
            _jsonDataBuilder = jsonDataBuilder;
            _releaseManager = releaseManager;
            _duplicateOrdersServcies = duplicateOrdersServcies;
            _ozonClientServcies = ozonClientServcies;
            _yandexDataManager = yandexDataManager;
        }

        public IActionResult Index()
        {
            var api = JobStorage.Current.GetMonitoringApi();
            JobList<SucceededJobDto> succeededJobs = api.SucceededJobs(0, 50);

            var results = succeededJobs.Select(job => JsonConvert.DeserializeObject<dynamic>((string)job.Value.Result)).ToList();

            var jobResult = new JobResultViewModel()
            {
                Results = new List<string>()
            };

            foreach (var result in results)
            {
                if(result is string)
                {
                    jobResult.Results.Add((string)result);
                }  
            }


            return View(jobResult);
        }

        [HttpPost]
        public IActionResult SetOzonRecurring(int delay)
        {
            string cronExp = $"*/{delay} * * * *";
            RecurringJob.AddOrUpdate(() => UploadingOzon(1), cronExp);

            return Redirect("/Hangfire");
        }

        [HttpPost]
        public IActionResult SetYandexRecurring(int delay)
        {
            string cronExp = $"*/{delay} * * * *";
            RecurringJob.AddOrUpdate(() => UploadingYandex(1), cronExp);

            return Redirect("/Hangfire");
        }

        [HttpPost]
        public async Task<IActionResult> SetEverydayUpdate(int monthsCount, int updateHour, int updateMinute)
        {
            string cronExp = $"{updateMinute} {updateHour} * * *";
            RecurringJob.AddOrUpdate(() => EverydayUpdate(monthsCount), cronExp);

            return Redirect("/Hangfire");
        }


        [HttpPost]
        public async Task<Dictionary<string, int[]>> EverydayUpdate(int monthsCount)
        {
            var periods = GetDateRanges(monthsCount);
            List<OzonClient> ozonClients = (await _ozonClientServcies.GetOzonClients()).Where(c => c.ClientType == ClientType.OZON).ToList();
            Dictionary<string, int[]> result = new Dictionary<string, int[]>();

            foreach (var period in periods)
            {
                int timeZone = _releaseManager.GetTimeZone();


                foreach (var client in ozonClients)
                {
                    try
                    {
                        _jsonDataBuilder.SetClient(client.DecryptClientId, client.DecryptApiKey);
                        var jsonData = await _jsonDataBuilder.BuildData(period.Item1, period.Item2);
                        var orders = await _orderCaster.JsonToOrders(jsonData);
                        orders = orders.Select(order => { order.OzonClient = client; return order; }).ToList();
                        var uploadResult = await _orderRepository.AddOrders(orders);
                        result.Add(client.Name, uploadResult);
                    }
                    catch (Exception ex)
                    {
                        continue;
                    }
                }
            }

            return result;
        }

        public async Task<string> UploadingOzon(int period)
        {
            List<OzonClient> ozonClients = (await _ozonClientServcies.GetOzonClients()).Where(c => c.ClientType == ClientType.OZON).ToList();
            Dictionary<string, int[]> result = new Dictionary<string, int[]>();
            int timeZone = _releaseManager.GetTimeZone();
            var start = DateTime.Now.AddHours(-period * 24 + timeZone);
            var end = DateTime.Now.AddHours(timeZone);
            string clientStatus = "Результат загрузки по Ozon клиентам<br>";

            foreach (var client in ozonClients)
            {
                try
                {
                    _jsonDataBuilder.SetClient(client.DecryptClientId, client.DecryptApiKey);
                    var jsonData = await _jsonDataBuilder.BuildData(start, end);
                    var orders = await _orderCaster.JsonToOrders(jsonData);
                    orders = orders.Select(order => { order.OzonClient = client; return order; }).ToList();
                    var uploadResult = await _orderRepository.AddOrders(orders);
                    result.Add(client.Name, uploadResult);
                    clientStatus += $"&nbsp;&nbsp;&nbsp;&nbsp;{client.Name}: отчёт успешно создан<br>";
                }
                catch (Exception ex)
                {
                    clientStatus += $"&nbsp;&nbsp;&nbsp;&nbsp;{client.Name}: {ex.Message}<br>";
                    continue;
                }
            }

            int? duplicateCount;
            int? deletedRowsCount;
            (duplicateCount, deletedRowsCount) = _duplicateOrdersServcies.DeleteDuplicateOrders();

            return GetJobResultString(result, start, end, clientStatus, duplicateCount, deletedRowsCount);
        }

        public async Task<string> UploadingYandex(int period)
        {
            List<OzonClient> ozonClients = (await _ozonClientServcies.GetOzonClients()).Where(c => c.ClientType == ClientType.YANDEX).ToList();
            Dictionary<string, int[]> result = new Dictionary<string, int[]>();
            int timeZone = _releaseManager.GetTimeZone();
            var start = DateTime.Now.AddHours(-period * 24 + timeZone);
            var end = DateTime.Now.AddHours(timeZone);
            string clientStatus = "Результат загрузки по Yandex клиентам<br>";

            foreach (var client in ozonClients)
            {
                try
                {
                    _yandexDataManager.SetClient(client.DecryptClientId, client.DecryptApiKey);
                    var jsonData = await _yandexDataManager.GetOrders(start, end);
                    var orders = await _orderCaster.YandexToOrders(jsonData);
                    orders = orders.Select(order => { order.OzonClient = client; return order; }).ToList();
                    var uploadResult = await _orderRepository.AddOrders(orders);
                    result.Add(client.Name, uploadResult);
                    clientStatus += $"&nbsp;&nbsp;&nbsp;&nbsp;{client.Name}: отчёт успешно создан<br>";
                }
                catch (Exception ex)
                {
                    clientStatus += $"&nbsp;&nbsp;&nbsp;&nbsp;{client.Name}: {ex.Message}<br>";
                    continue;
                }
            }

            int? duplicateCount;
            int? deletedRowsCount;
            (duplicateCount, deletedRowsCount) = _duplicateOrdersServcies.DeleteDuplicateOrders();

            return GetJobResultString(result, start, end, clientStatus, duplicateCount, deletedRowsCount);
        }

        private string GetJobResultString(Dictionary<string, int[]> result, DateTime start, DateTime end, string clientStatus, int? duplicateCount, int? deletedRowsCount)
        {
            string jobResult = "";

            jobResult += $"Период: {start.ToString("dd.MM.yyyy HH:mm:ss")} - {end.ToString("dd.MM.yyyy HH:mm:ss")}<br/>";
            foreach(var kvp in result)
            {
                jobResult += $"&nbsp;&nbsp;&nbsp;&nbsp;Клиент: {kvp.Key} - загружено {kvp.Value[0]}, обновлено {kvp.Value[1]}<br/>";
            }
            jobResult += clientStatus;

            jobResult += $"Удалено дублей {deletedRowsCount} из {duplicateCount}";


            return jobResult;
        }

        public  List<(DateTime, DateTime)> GetDateRanges(int months)
        {
            List<(DateTime, DateTime)> dateRanges = new List<(DateTime, DateTime)>();

            // Текущая дата
            DateTime endDate = DateTime.Today;

            // Дата, которая была указано количество месяцев назад
            DateTime startDate = endDate.AddMonths(-months);

            // Переход по неделям
            while (startDate < endDate)
            {
                DateTime weekStart = startDate;
                DateTime weekEnd = startDate.AddDays(6);

                // Проверяем, чтобы конец недели не превышал текущую дату
                if (weekEnd > endDate)
                {
                    weekEnd = endDate;
                }

                dateRanges.Add((weekStart, weekEnd));

                // Переходим к следующей неделе
                startDate = startDate.AddDays(7);
            }

            return dateRanges;
        }

    }


}
