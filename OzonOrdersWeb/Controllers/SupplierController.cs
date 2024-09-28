﻿using Microsoft.AspNetCore.Mvc;
using OzonDomains.Models;
using OzonRepositories.Context;
using Services.CacheServcies.Cache.OzonOrdersCache;
using Services.CacheServcies.Cache;
using Servcies.DataServcies;
using OzonDomains.ViewModels;
using Servcies.FiltersServcies.FilterModels;
using System.Data.Entity.Infrastructure;
using Servcies.FiltersServcies.SortModels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;
using OServcies.FiltersServcies.FilterModels;
using OzonOrdersWeb.ViewModels;
using OzonDomains;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Servcies.ParserServcies;
using Servcies.ReleasServcies.ReleaseManager;
using OzonOrdersWeb.ViewModels.SupplierViewModels;
using Servcies.FiltersServcies.DataFilterManagers;

namespace OzonOrdersWeb.Controllers
{
    [Authorize(Roles = "User,Admin")]
    public class SupplierController : Controller, ISortLogicContriller<Supplier, SupplierSortState>
    {
        private readonly SupplierDataServcies _servcies;
        private readonly OrdersDataServcies _ordersDataServcies;
        private readonly OrderCache _cache;
        private readonly CacheUpdater<Order> _cacheUpdater;
        private readonly SupplierDataFilterManager _dataFilterManager;
        private readonly OrderCaster _orderCaster;
        private readonly ReleaseManager _releaseManager;
        public SupplierController(SupplierDataServcies servcies, 
                                  OrdersDataServcies ordersDataServcies,
                                  OrderCache orderCache, 
                                  CacheUpdater<Order> cacheUpdater,
                                  SupplierDataFilterManager dataFilterManager,
                                  OrderCaster orderCaster,
                                  ReleaseManager releaseManager)
        {
            _servcies = servcies;
            _ordersDataServcies = ordersDataServcies;
            _cache = orderCache;
            _cacheUpdater = cacheUpdater;
            _dataFilterManager = dataFilterManager; 
            _orderCaster = orderCaster;
            _releaseManager = releaseManager;
        }

        public async Task<IActionResult> Index(SupplierSortState sortOrder = SupplierSortState.StandardState, int page = 1)
        {
            SaveSortStateCookie(sortOrder);

            List<Supplier> suppliers = await _servcies.GetSuppliers();

            var filterDataString = HttpContext.Request.Cookies["SupplierFilterData"];

            var filterData = new SupplierFilterModel();
            if (!string.IsNullOrEmpty(filterDataString))
            {
                filterData = JsonConvert.DeserializeObject<SupplierFilterModel>(filterDataString);
                suppliers = _dataFilterManager.FilterByFilterData(suppliers, filterData);
            }

            SetSortOrderViewData(sortOrder);
            suppliers = (await ApplySortOrder(suppliers, sortOrder)).ToList();

            var supplierViewModel = new SupplierViewModel<Supplier, SupplierFilterModel>(suppliers, page, 15, filterData)
            {
                CurrencyCodes = [(CurrencyCode.RUB, "RUB"),
                                (CurrencyCode.USD, "USD"),
                                (CurrencyCode.EUR, "EUR"),
                                (CurrencyCode.BYN, "BYN")],
                CurrencyCodesSelectList = new List<SelectListItem>
                                {
                                    new SelectListItem { Value = CurrencyCode.RUB.ToString(), Text = "RUB" },
                                    new SelectListItem { Value = CurrencyCode.USD.ToString(), Text = "USD" },
                                    new SelectListItem { Value = CurrencyCode.EUR.ToString(), Text = "EUR" },
                                    new SelectListItem { Value = CurrencyCode.BYN.ToString(), Text = "BYN" },
                                }
            };

            ViewData["DisplayCurrencyCode"] = supplierViewModel.CurrencyCodes.FirstOrDefault(t => t.Item1 == filterData.CurrencyCode).Item2;

            return View(supplierViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Index(SupplierFilterModel filterData, int page = 1)
        {
            List<Supplier> suppliers = await _servcies.GetSuppliers();
            suppliers = _dataFilterManager.FilterByFilterData(suppliers, filterData);

            var serializedFilterData = JsonConvert.SerializeObject(filterData);
            HttpContext.Response.Cookies.Append("SupplierFilterData", serializedFilterData);

            return View(new SupplierViewModel<Supplier, SupplierFilterModel>(suppliers, page, 15, filterData)
            {
                CurrencyCodes = [(CurrencyCode.RUB, "RUB"),
                                 (CurrencyCode.USD, "USD"),
                                 (CurrencyCode.EUR, "EUR"),
                                 (CurrencyCode.BYN, "BYN")],
                CurrencyCodesSelectList = new List<SelectListItem>
                                {
                                    new SelectListItem { Value = CurrencyCode.RUB.ToString(), Text = "RUB" },
                                    new SelectListItem { Value = CurrencyCode.USD.ToString(), Text = "USD" },
                                    new SelectListItem { Value = CurrencyCode.EUR.ToString(), Text = "EUR" },
                                    new SelectListItem { Value = CurrencyCode.BYN.ToString(), Text = "BYN" },
                                }
            });
        }

        public async Task<IActionResult> Create()
        {
            string result = (string)TempData["SupplierResult"];

            var suppliersList = await _servcies.GetSuppliers();

            var viewModel = new CreateSupplierViewModel
            {
                SupplierResult = result,
                SuppliersList = suppliersList,
                CurrencyCodes = new List<SelectListItem>
                                {
                                    new SelectListItem { Value = CurrencyCode.RUB.ToString(), Text = "RUB" },
                                    new SelectListItem { Value = CurrencyCode.USD.ToString(), Text = "USD" },
                                    new SelectListItem { Value = CurrencyCode.EUR.ToString(), Text = "EUR" },
                                    new SelectListItem { Value = CurrencyCode.BYN.ToString(), Text = "BYN" },
                                }
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                var existingSupplier = await _servcies.GetSupplierAsync(supplier);
                if (existingSupplier != null)
                {
                    TempData["SupplierResult"] = $"Не удалось добавить поставщика '{supplier.Name}'";
                    return RedirectToAction(nameof(Create));
                }

                await _servcies.AddSupplier(supplier);

                return Redirect(Request.Cookies["PreviousPageUrl"].ToString());
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _servcies.GetSupplierAsync(id);
            if (supplier != null)
            {
                var defaultSupplier = await _servcies.GetSupplierAsync(supplier);

                var ordersWithDeletedSupplier = (await _ordersDataServcies.GetOrders()).ToList();
                ordersWithDeletedSupplier = ordersWithDeletedSupplier.Where(o => o.SupplierId == id).ToList();
                foreach (var order in ordersWithDeletedSupplier)
                {
                    order.Supplier = defaultSupplier;
                }
                
                await _servcies.DeleteSupplier(supplier);
                await _cacheUpdater.Update(_cache);
            }

            return Redirect(Request.Cookies["PreviousPageUrl"].ToString());
        }

        public async Task<IActionResult> Edit(int id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supplier = await _servcies.GetSupplierAsync(id);
            if (supplier == null)
            {
                return NotFound();
            }

            var viewModel = new EditSupplierViewModel
            {
                Supplier = supplier,
                CurrencyCodes = new List<SelectListItem>
                                {
                                    new SelectListItem { Value = CurrencyCode.RUB.ToString(), Text = "RUB" },
                                    new SelectListItem { Value = CurrencyCode.USD.ToString(), Text = "USD" },
                                    new SelectListItem { Value = CurrencyCode.EUR.ToString(), Text = "EUR" },
                                    new SelectListItem { Value = CurrencyCode.BYN.ToString(), Text = "BYN" },
                                }
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _servcies.UpdateSupplier(supplier);
                    //List<Order> ordersToUpdate = await _ordersDataServcies.GetOrdersByManufacturerCode(manufacturer.Code);
                    //await _orderCache.UpdateCacheIncrementally(ordersToUpdate);

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _servcies.GetSupplierAsync(id) == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        public void SetSortOrderViewData(SupplierSortState supplierSort)
        {
            ViewData["SupplierNameSort"] = supplierSort == SupplierSortState.NameAsc ? SupplierSortState.NameDesc : SupplierSortState.NameAsc;
            ViewData["SupplieCostFactorSort"] = supplierSort == SupplierSortState.CostFactorAsc ? SupplierSortState.CostFactorDesc : SupplierSortState.CostFactorAsc;
            ViewData["SupplieWeightFactorSort"] = supplierSort == SupplierSortState.WeightFactorAsc ? SupplierSortState.WeightFactorDesc : SupplierSortState.WeightFactorAsc;
            ViewData["SupplieCurrencyCodeSort"] = supplierSort == SupplierSortState.CurrencyCodeAsc ? SupplierSortState.CurrencyCodeDesc : SupplierSortState.CurrencyCodeAsc;
            ViewData["SupplieWeightFactorCurrencyCodeSort"] = supplierSort == SupplierSortState.WeightFactorCurrencyCodeAsc ? SupplierSortState.WeightFactorCurrencyCodeDesc: SupplierSortState.WeightFactorCurrencyCodeAsc;
        }

        public async Task<IEnumerable<Supplier>> ApplySortOrder(IEnumerable<Supplier> suppliers, SupplierSortState supplierSort)
        {
            return supplierSort switch
            {
                SupplierSortState.NameAsc => suppliers.OrderBy(o => o.Name),
                SupplierSortState.NameDesc => suppliers.OrderByDescending(o => o.Name),

                SupplierSortState.CostFactorAsc => suppliers.OrderBy(o => o.CostFactor),
                SupplierSortState.CostFactorDesc => suppliers.OrderByDescending(o => o.CostFactor),

                SupplierSortState.WeightFactorAsc => suppliers.OrderBy(o => o.WeightFactor),
                SupplierSortState.WeightFactorDesc => suppliers.OrderByDescending(o => o.WeightFactor),

                SupplierSortState.CurrencyCodeAsc => suppliers.OrderBy(o => o.CurrencyCode),
                SupplierSortState.CurrencyCodeDesc => suppliers.OrderByDescending(o => o.CurrencyCode),

                SupplierSortState.WeightFactorCurrencyCodeAsc => suppliers.OrderBy(o => o.WeightFactorCurrencyCode),
                SupplierSortState.WeightFactorCurrencyCodeDesc => suppliers.OrderByDescending(o => o.WeightFactorCurrencyCode),

                _ => suppliers
            }; ;
        }

        public void SaveSortStateCookie(SupplierSortState supplierSort)
        {
            if (supplierSort != SupplierSortState.StandardState)
            {
                Response.Cookies.Delete("SupplierSortState");
                Response.Cookies.Append("SupplierSortState", supplierSort.ToString());
            }
        }

        [HttpPost]
        public async Task<IActionResult> DelSortStateCookie()
        {
            Response.Cookies.Delete("SupplierSortState");
            Response.Cookies.Append("SupplierSortState", SupplierSortState.StandardState.ToString());
            return RedirectToAction("Index");
        }
    }
}
