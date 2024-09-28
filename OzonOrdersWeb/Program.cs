using Hangfire;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using OzonDomains.Models;
using OzonOrdersWeb.Middleware;
using OzonOrdersWeb.Services.Cookies;
using OzonOrdersWeb.Services.HangfireAuthorization;
using OzonRepositories.Context.Identity;
using OzonRepositories.Data;
using OzonRepositories.Utils;
using Servcies.ApiServcies.DropBoxApi;
using Servcies.ApiServcies.InterpartsApi;
using Servcies.ApiServcies.OzonApi;
using Servcies.ApiServcies.YandexApi;
using Servcies.CacheServcies.Cache.OzonOrdersCache;
using Servcies.DataServcies;
using Servcies.FiltersServcies;
using Servcies.FiltersServcies.DataFilterManagers;
using Servcies.ParserServcies;
using Servcies.ParserServcies.FielParsers;
using Servcies.PriceСlculationServcies;
using Servcies.ReleasServcies.ReleaseManager;
using Servcies.TransactionUtilsServcies;
using Services.CacheServcies.Cache;
using Services.CacheServcies.Cache.OzonOrdersCache;

var builder = WebApplication.CreateBuilder(args);
bool IsProd = true;
string regionCode = "ru";

builder.Services.AddControllersWithViews();


builder.Services.AddControllersWithViews();

IServiceCollection services = builder.Services;

string sqlConnection = builder.Configuration.GetConnectionString("SqlServerConnection");
if (IsProd)
{
    regionCode = "en";
    sqlConnection = builder.Configuration.GetConnectionString("ProdSqlServerConnection");
}

else
{
    regionCode = "ru";
    sqlConnection = builder.Configuration.GetConnectionString("SqlServerConnection");
}

services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});

services.AddDbContext<OzonRepositories.Context.OzonOrderContext>(options =>
    options.UseSqlServer(sqlConnection));


services.AddDbContext<OzonIdentityOrderContext>(options =>
        options.UseSqlServer(sqlConnection));

services.AddHangfire(h => h.UseSqlServerStorage(sqlConnection));


builder.Services
    .AddDefaultIdentity<CustomIdentityUser>()
    .AddDefaultTokenProviders()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<OzonIdentityOrderContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(1); // Установите желаемое время сессии
    options.SlidingExpiration = true; // Обновлять время сессии при активности пользователя
});


// Настройка максимального размера запроса для Kestrel
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 314572800; // 300 MB
});

// Настройка максимального размера запроса для IIS
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 314572800; // 300 MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = 4096 * 2; // Увеличение лимита до 4096 полей
});

services.AddMemoryCache();
services.AddDistributedMemoryCache();
services.AddSession();

services.AddTransient<MainRepository>();

services.AddTransient<OrderRepository>();
services.AddTransient<OrdersDataServcies>();

services.AddTransient<ProductRepository>();
services.AddTransient<ProductsDataServcies>();

services.AddTransient<OrdersHistoryRepository>();
services.AddTransient<OrderHistoryDataServcies>();

services.AddTransient<AppStatusRepository>();
services.AddTransient<AppStatusDataServcies>();

services.AddTransient<SupplierRepository>();
services.AddTransient<SupplierDataServcies>();

services.AddTransient<TransactionRepository>();
services.AddTransient<TransactionDataServcies>();

services.AddTransient<DuplicateOrdersRepository>();
services.AddTransient<DuplicateOrdersServcies>();

services.AddTransient<OzonClientRepository>();
services.AddTransient<OzonClientServcies>();

services.AddTransient<WarehouseRepository>();
services.AddTransient<WarehouseDataServcies>();

services.AddTransient<ColumnMappingRepository>();
services.AddTransient<ColumnMappingDataServcies>();

services.AddTransient<ManufacturerRepository>();
services.AddTransient<ManufacturerDataService>();

services.AddTransient<OrdersFileMetadataRepository>();
services.AddTransient<OrdersFileMetadataDataService>();

services.AddTransient<UserAccessRepository>();
services.AddTransient<UserAccessDataServices>();

services.AddTransient<ExcelParser>();
services.AddTransient<ExcelExporter>();

services.AddTransient<CryptographyServcies>(serviceProvider =>
{
    var secret = builder.Configuration.GetConnectionString("Secret");
    var cryptographyServcies = new CryptographyServcies();
    cryptographyServcies.SetSecret(secret);
    return cryptographyServcies;
});

services.AddTransient<OrderToSupplierTransactionManager>();

services.AddTransient<CurrencyRateFetcher>();
services.AddTransient<OrderPriceManager>();

services.AddScoped<ProductCache>();
services.AddScoped<OrderCache>(); // Изменено на Scoped
services.AddScoped<TransactionCache>();
services.AddScoped<CacheUpdater<Product>>();
services.AddScoped<CacheUpdater<Order>>();
services.AddScoped<CacheUpdater<Transaction>>();

services.AddTransient<DbInitializer>();

services.AddMemoryCache();
services.AddDistributedMemoryCache();
services.AddSession();

services.AddTransient<CookiesManeger>();
services.AddTransient(typeof(DataFilter<>));
services.AddTransient(typeof(QueryableDataFilter<>));
services.AddTransient<OrderDataFilterManager>();
services.AddTransient<ProductDataFilterManager>();
services.AddTransient<TransactionDataFilterManager>();
services.AddTransient<ManufacturerFilterManager>();
services.AddTransient<SupplierDataFilterManager>();
services.AddTransient<AppStatusDataFilterManager>();
services.AddTransient<OzonClientDataFilterManager>();
services.AddTransient<UserFilterManager>();
services.AddTransient<ColumnMappingDataFilterManager>();

services.AddTransient<OrderCaster>();

services.AddTransient<OzonJsonDataBuilder>(serviceProvider =>
{
    var clientId = builder.Configuration.GetConnectionString("ClientId");
    var apiKey = builder.Configuration.GetConnectionString("ApiKey");

    var jsonDataBuilder = new OzonJsonDataBuilder();
    jsonDataBuilder.Init(clientId, apiKey);
    return jsonDataBuilder;
});

services.AddTransient<YandexDataManager>(serviceProvider =>
{
    var apiKey = builder.Configuration.GetConnectionString("YandexApiKey");
    var yandexDataManager = new YandexDataManager();
    yandexDataManager.SetClient(apiKey);
    return yandexDataManager;
});

services.AddTransient<InterpartsApiDataManager>(serviceProvider =>
{
    var apiKey = builder.Configuration.GetConnectionString("InterpartsApiKey");
    var interpartsApiDataManager = new InterpartsApiDataManager();
    interpartsApiDataManager.SetClient(apiKey);
    return interpartsApiDataManager;
});

services.AddSingleton(new DropboxApiClient(builder.Configuration.GetConnectionString("DropBoxRefreshToken"),
                                           builder.Configuration.GetConnectionString("DropBoxAppKey"),
                                           builder.Configuration.GetConnectionString("DropBoxAppSecret")));

builder.Services.AddSingleton<ReleaseManager>(provider =>
{
    ReleaseManager releaseManager = ReleaseManager.Instance;
    releaseManager.SetRegionCode(regionCode);
    return releaseManager;
});

services.AddHangfireServer();

var app = builder.Build();

app.UseHangfireDashboard("/Hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorization() }
});


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseSession();
app.UseDbInitializerMiddleware();
app.UseErrorHandlingMiddleware();
app.UsePreviousPageUrlMiddleware();

app.UseMiddleware<PreviousPageUrlMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseAuthorization();



app.MapRazorPages();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Orders}/{action=IndexV2}/{id?}");

app.Run();
