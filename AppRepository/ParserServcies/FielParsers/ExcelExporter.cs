using OfficeOpenXml.Style;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OzonDomains.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Servcies.ParserServcies.FielParsers
{
    public class ExcelExporter
    {
        public Dictionary<string, Func<Order, object>> ColumnMappings = new Dictionary<string, Func<Order, object>>
            {
                { "Номер заказа", order => order.ShipmentNumber },
                { "Клиент", order => order.OzonClient?.Name },
                { "Принят в обработку", order => order.FullFormattedProcessingDate },
                { "Дата отгрузки", order => order.FullFormattedShippingDate },
                { "Срок доставки", order => order.FormattedDeliveryPeriod },
                { "Статус клиента", order => order.Status },
                { "Статус", order => order.AppStatus?.Name },
                { "Наименование товара", order => order.ProductName },
                { "Артикул", order => order.ProductKey },
                { "Производитель", order => order.Manufacturer?.Name },
                { "Склад отгрузки", order => order.ShipmentWarehouse?.Name },
                { "Поставщик", order => order.Supplier?.Name },
                { "Номер заказа поставщику", order => order.OrderNumberToSupplier },
                { "Цена сайта", order => order.ProductInfo?.CurrentPriceWithDiscount },
                { "Цена", order => order.Price },
                { "Количество", order => order.Quantity },
                { "Сумма отправления", order => order.ShipmentAmount },
                { "Категория", order => order.ProductInfo?.CommercialCategory},
                { "Объемный вес", order => order.ProductInfo?.VolumetricWeight },
                { "Цена закупки", order => order.PurchasePrice },
                { "Комиссия ОЗОН (мин.)", order => order.MinOzonCommission },
                { "Комиссия ОЗОН (макс.)", order => order.MaxOzonCommission },
                { "Прибыль (мин.)", order => order.MinProfit },
                { "Прибыль (макс.)", order => order.MaxProfit },
                { "Наценка % (мин.)", order => order.MinDiscount },
                { "Наценка % (макс.)", order => order.MaxDiscount },
                { "Город доставки", order => order.DeliveryCity }
            };

        public byte[] ExportToExcel(List<Order> orders, List<string> columnsToExport)
        {
            

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Orders");

                int colIndex = 1;
                for (int i = 0; i < columnsToExport.Count; i++)
                {
                    var columnName = columnsToExport[i];

                    if (columnName == "Комиссия ОЗОН" || columnName == "Прибыль" || columnName == "Наценка %")
                    {
                        worksheet.Cells[1, colIndex].Value = columnName + " (мин.)";
                        colIndex++;
                        worksheet.Cells[1, colIndex].Value = columnName + " (макс.)";
                        colIndex++;
                    }
                    else
                    {
                        worksheet.Cells[1, colIndex].Value = columnName;
                        colIndex++;
                    }
                }

                int row = 2;
                foreach (var order in orders)
                {
                    colIndex = 1;
                    foreach (var columnName in columnsToExport)
                    {
                        if (columnName == "Комиссия ОЗОН" || columnName == "Прибыль" || columnName == "Наценка %")
                        {
                            if (ColumnMappings.TryGetValue(columnName + " (мин.)", out Func<Order, object> getMinValue))
                            {
                                object minValue = getMinValue(order);
                                worksheet.Cells[row, colIndex].Value = minValue;
                                colIndex++;
                            }

                            if (ColumnMappings.TryGetValue(columnName + " (макс.)", out Func<Order, object> getMaxValue))
                            {
                                object maxValue = getMaxValue(order);
                                worksheet.Cells[row, colIndex].Value = maxValue;
                                colIndex++;
                            }
                        }
                        else if (ColumnMappings.TryGetValue(columnName, out Func<Order, object> getValue))
                        {
                            object value = getValue(order);
                            worksheet.Cells[row, colIndex].Value = value;
                            colIndex++;
                        }
                    }
                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            }
        }

    }
}
