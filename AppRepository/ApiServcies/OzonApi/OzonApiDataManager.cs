using Newtonsoft.Json.Linq;
using Servcies.ApiServcies.OzonApi.Filters;
using Servcies.ParserServcies;
using System.Globalization;

namespace Servcies.ApiServcies.OzonApi
{
    public class OzonApiDataManager : IApiDataManager<OzonApiDataManager>
    {
        private OzonApiClient ozonApiClient;
        private readonly CsvUrlParser csvUrlParser;
        private readonly Dictionary<string, string>  StatusDescriptions = new Dictionary<string, string>
                                                                        {
                                                                            { "waiting", "в очереди на обработку" },
                                                                            { "processing", "обрабатывается" },
                                                                            { "success", "отчёт успешно создан" },
                                                                            { "failed", "ошибка при создании отчёта" }
                                                                        };

        public async Task<bool> GetTestRequest(string clientId, string apiKey)
        {
            return await ozonApiClient.GetTestRequest(clientId, apiKey);
        }
        public OzonApiDataManager(string clientId, string apiKey)
        {
            ozonApiClient = new OzonApiClient(clientId, apiKey);
            csvUrlParser = new CsvUrlParser();
        }
        public OzonApiDataManager SetClient(string clientId, string apiKey)
        {
            ozonApiClient = new OzonApiClient(clientId, apiKey);
            return this;
        }

        public async Task<string> GetReportCode(DateTime start, DateTime end,
                                                string uri = OzonApiUrl.REPORT_CODE,
                                                string deliverySchema = "fbs")
        {
            CultureInfo russianCulture = new CultureInfo("ru-RU");

            var postingsReportRequest = new PostingsReportRequest
            {

                Filter = new PostingsReportRequestFilter
                {
                    ProcessedAtFrom = start, // DateTime.Now.AddHours(-period * 24 + timeZone),
                    ProcessedAtTo = end, // DateTime.Now.AddHours(timeZone),
                    DeliverySchema = [deliverySchema],
                    Sku = [],
                    CancelReasonId = [],
                    OfferId = "",
                    StatusAlias = [],
                    Statuses = [],
                    Title = ""
                },
                Language = "DEFAULT"
            };


            var postingsResponse = ozonApiClient.MakeRequest(postingsReportRequest, uri);

            return postingsResponse["result"]["code"].ToString();
        }

        public async Task<string> GetReportFile(string code, string uri = OzonApiUrl.REPORT_FILE)
        {

            var reportInfoRequest = new ReportInfoRequest
            {
                Code = code
            };
            var infoResponse = ozonApiClient.MakeRequest(reportInfoRequest, uri);

            if (infoResponse["result"]["status"].ToString() == "success")
            {
                return infoResponse["result"]["file"].ToString();
            }
            else
            {
                throw new Exception(message: StatusDescriptions[infoResponse["result"]["status"].ToString()]);
            }  
        }

        public async Task<JArray> ReadFileByUrl(string url)
        {
            var fileContent = await csvUrlParser.ReadFileFromUrl(url);
            return fileContent;
        }

        public async Task<string> GetProductCode(string[] articles,
                                                  string[] ozonIds,
                                                  string uri = OzonApiUrl.PRODUCT_CODE)
        {
            var productsReportRequest = new ProductsReportRequest
            {
                Language = "DEFAULT",
                OfferId = articles,
                Search = "",
                Sku = ozonIds,
                Visibility = "ALL"
            };

            var productsReportResponse = ozonApiClient.MakeRequest(productsReportRequest, uri);
            return productsReportResponse["result"]["code"].ToString();

        }

        public async Task<JObject> GetProductPrices(string[] articles,
                                                   string[] ozonProductIds,
                                                   string uri = OzonApiUrl.PRODUCT_PRICES)
        {
            for (int i = 0; i < articles.Length; i++)
            {
                if (articles[i].StartsWith("'"))
                {
                    articles[i] = articles[i].Substring(1);
                }
            }

            var productPricesRequest = new ProductPricesRequest
            {
                Filter = new ProductPricesRequestFilter
                {
                    OfferId = articles,
                    ProductId = ozonProductIds,
                    Visibility = "ALL"
                },
                LastId = "",
                Limit = 500
            };

            var productPricesResponse = ozonApiClient.MakeRequest(productPricesRequest, uri);
            return productPricesResponse;

        }

        public async Task<JObject> GetProductWarehouses(string[] sku,
                                                       string[] fbsSku,
                                                       string uri = OzonApiUrl.PRODUCT_WAERHOUSES) ///https://docs.ozon.ru/api/seller/#operation/PostingAPI_GetFbsPostingListV3
        {
            var stocksByWarehouseRequest = new StocksByWarehouseRequest
            {
                Sku = sku,
                FbsSku = fbsSku
            };

            var stocksByWarehouseResponse = ozonApiClient.MakeRequest(stocksByWarehouseRequest, uri);
            return stocksByWarehouseResponse;


        }


        public async Task<JObject> GetProductWarehousAndCity(string productNumber,
                                                       string uri = OzonApiUrl.WAERHOUS_AND_CITY)
        {
            var productAnalyticsRequest = new ProductAnalyticsRequest
            {
                PostingNumber = productNumber,

                With = new ProductAnalyticsRequestFilter
                {
                    AnalyticsData = true,
                    Barcodes = false,
                    FinancialData = true,
                    ProductExemplars = false,
                    Translit = false
                }
            };

            var productAnalyticsResponse = ozonApiClient.MakeRequest(productAnalyticsRequest, uri);
            return productAnalyticsResponse;
        }

        public async Task<string> GetProductSatatus(string productNumber,
                                                       string uri = OzonApiUrl.WAERHOUS_AND_CITY)
        {
            try
            {
                var productAnalyticsRequest = new ProductAnalyticsRequest
                {
                    PostingNumber = productNumber,

                    With = new ProductAnalyticsRequestFilter
                    {
                        AnalyticsData = true,
                        Barcodes = false,
                        FinancialData = true,
                        ProductExemplars = false,
                        Translit = false
                    }
                };

                var productAnalyticsResponse = ozonApiClient.MakeFastRequest(productAnalyticsRequest, uri);
                return productAnalyticsResponse["result"]["status"].ToString();
            }
            catch(Exception ex) 
            {
                return string.Empty;
            }
        }

        public async Task<MemoryStream> GetOrderLabel(string shipmentNumber, string uri = OzonApiUrl.ORDER_LABLE)
        {
            var orderLableRequest = new OrdersLableRequest
            {
                ShipmentNumber = new[] { shipmentNumber }
            };

            var response = await ozonApiClient.MakeFileRequestAsync(orderLableRequest, uri);
            return response;
        }

        public async Task<bool> CheckOrderForExistence(string productNumber, 
                                                       string uri = OzonApiUrl.WAERHOUS_AND_CITY)
        {
            var productAnalyticsRequest = new ProductAnalyticsRequest
            {
                PostingNumber = productNumber,

                With = new ProductAnalyticsRequestFilter
                {
                    AnalyticsData = false,
                    Barcodes = false,
                    FinancialData = true,
                    ProductExemplars = false,
                    Translit = false
                }
            };


            try
            {

                var productAnalyticsResponse = ozonApiClient.MakeRequest(productAnalyticsRequest, uri);
                if (productAnalyticsResponse.ContainsKey("code") || productAnalyticsResponse.ContainsKey("message") || productAnalyticsResponse.ContainsKey("details"))
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

    }
}
