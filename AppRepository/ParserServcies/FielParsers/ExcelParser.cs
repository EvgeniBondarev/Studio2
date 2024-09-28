using OfficeOpenXml;
using OzonDomains;
using Servcies.ParserServcies.FielParsers.Models;
using Servcies.ReleasServcies.ReleaseManager;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.ParserServcies.FielParsers
{
    public class ExcelParser 
    {
        private ReleaseManager _releaseManager;
        private CultureInfo _culture;

        public ExcelParser(ReleaseManager releaseManager)
        {
            _releaseManager = releaseManager;
            _culture = _releaseManager.GetCultureInfo();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public List<Dictionary<string, string>> UpdateTableToStandartColumns(List<Dictionary<string, string>> table,
                                                                         Dictionary<string, string> ColumnMappings)
        {
            var updatedTableData = new List<Dictionary<string, string>>();

            foreach (var row in table)
            {
                var updatedRow = new Dictionary<string, string>();

                foreach (var column in row)
                {

                    var mappedKey = ColumnMappings.FirstOrDefault(n => n.Value == column.Key).Key;

                    if (mappedKey != null)
                    {
                        updatedRow.Add(mappedKey, column.Value);
                    }

                }

                updatedTableData.Add(updatedRow);
            }

            return updatedTableData;
        }

        public MemoryStream ConvertCsvToExcel(Stream csvStream, char delimiter)
        {
            csvStream.Position = 0; // Ensure the stream is at the beginning

            var excelStream = new MemoryStream();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                using (var reader = new StreamReader(csvStream, Encoding.UTF8))
                {
                    int row = 1;
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        string[] cells = SplitCsvLine(line, delimiter);

                        for (int col = 0; col < cells.Length; col++)
                        {
                            worksheet.Cells[row, col + 1].Value = cells[col];
                        }
                        row++;
                    }
                }

                package.SaveAs(excelStream);
            }

            excelStream.Position = 0; // Ensure the stream is at the beginning before returning
            return excelStream;
        }

        private string[] SplitCsvLine(string line, char delimiter)
        {
            List<string> cells = new List<string>();

            bool inQuotes = false;
            StringBuilder currentCell = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == delimiter && !inQuotes)
                {
                    cells.Add(currentCell.ToString().Trim('"'));
                    currentCell.Clear();
                }
                else
                {
                    currentCell.Append(c);
                }
            }

            cells.Add(currentCell.ToString().Trim('"')); // Add last cell
            return cells.ToArray();
        }

        public List<string> GetTableHeaders(Stream excelStream, int startRow = 1, int startColumn = 1)
        {
            using (var package = new ExcelPackage(excelStream))
            {
                var worksheet = package.Workbook.Worksheets[0]; // Получаем первый лист
                var headers = new List<string>();

                // Читаем заголовки начиная с указанной строки и первой колонки
                foreach (var cell in worksheet.Cells[startRow, startColumn, startRow, worksheet.Dimension.End.Column])
                {
                    headers.Add(cell.Text);
                }

                return headers;
            }
        }

        public List<Dictionary<string, string>> GetTableData(Stream excelStream, int startRow = 1, int startColumn = 1)
        {
            var tableData = new List<Dictionary<string, string>>();

            using (var package = new ExcelPackage(excelStream))
            {
                var worksheet = package.Workbook.Worksheets[0];
                var headers = new List<string>();

                // Читаем заголовки
                foreach (var cell in worksheet.Cells[startRow, startColumn, startRow, worksheet.Dimension.End.Column])
                {
                    headers.Add(cell.Text);
                }

                // Читаем данные
                for (int row = startRow + 1; row <= worksheet.Dimension.End.Row; row++)
                {
                    var rowData = new Dictionary<string, string>();
                    bool rowIsEmpty = true; // Флаг для проверки пустой строки

                    for (int col = startColumn; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var cell = worksheet.Cells[row, col];
                        string cellText;

                        // Проверка, содержит ли ячейка дату
                        if (cell.Value is DateTime dateValue)
                        {
                            cellText = dateValue.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            cellText = cell.Text;
                        }

                        // Если хотя бы одно значение непустое, считаем строку непустой
                        if (!string.IsNullOrWhiteSpace(cellText))
                        {
                            rowIsEmpty = false;
                        }

                        // Добавляем данные в строку
                        rowData[headers[col - startColumn]] = cellText;
                    }

                    // Добавляем строку в список только если она непустая
                    if (!rowIsEmpty)
                    {
                        tableData.Add(rowData);
                    }
                }
            }

            return tableData;
        }

        public async Task<List<MatchedRowModel>> MatchExcelDataAsync(Stream file1Stream, Stream file2Stream, List<MatchingColumn> matchingColumns, int startRow1, int startColumn1, int startRow2, int startColumn2)
        {
            var matchedRows = new List<MatchedRowModel>();

            // Чтение данных из первого файла
            var headersFile1 = GetTableHeaders(file1Stream, startRow1, startColumn1);
            file1Stream.Position = 0;  // Сброс потока для повторного чтения
            var dataFile1 = GetTableData(file1Stream, startRow1, startColumn1);

            // Чтение данных из второго файла
            var headersFile2 = GetTableHeaders(file2Stream, startRow2, startColumn2);
            file2Stream.Position = 0;  // Сброс потока для повторного чтения
            var dataFile2 = GetTableData(file2Stream, startRow2, startColumn2);

            // Удаление дублирующихся заголовков
            headersFile1 = headersFile1.Distinct().ToList();
            headersFile2 = headersFile2.Distinct().ToList();

            // Перебираем строки из первого файла
            foreach (var rowFile1 in dataFile1)
            {
                //// Проверяем на пустые или неполные строки
                //if (IsRowEmptyOrIncomplete(rowFile1, headersFile1))
                //{
                //    continue; // Пропускаем пустые или неполные строки
                //}

                var matchedRow = new MatchedRowModel
                {
                    File1Data = rowFile1,
                    File2Data = new List<Dictionary<string, string>>() // Инициализируем список совпадений
                };

                var rowsToRemove = new List<Dictionary<string, string>>(); // Строки для удаления

                // Находим строки из второго файла, которые совпадают по указанным столбцам
                foreach (var rowFile2 in dataFile2)
                {
                    // Проверяем на пустые или неполные строки
                    if (IsRowEmptyOrIncomplete(rowFile2, headersFile2))
                    {
                        continue; // Пропускаем пустые или неполные строки
                    }

                    // Проверяем, совпадают ли все значения по указанным парам столбцов одновременно
                    bool isMatch = matchingColumns.All(match =>
                    {
                        // Проверяем, что оба столбца существуют в соответствующих строках
                        if (!rowFile1.ContainsKey(match.Item1) || !rowFile2.ContainsKey(match.Item2))
                        {
                            return false;
                        }

                        // Приводим оба значения к наиболее подходящему типу для корректного сравнения
                        var value1 = rowFile1[match.Item1];
                        var value2 = rowFile2[match.Item2];

                        // Приведение типов и сравнение значений
                        return CompareValues(value1, value2);
                    });

                    // Если строка совпадает по всем указанным значениям, добавляем её в список совпадений
                    if (isMatch)
                    {
                        matchedRow.File2Data.Add(rowFile2);
                        rowsToRemove.Add(rowFile2); // Добавляем строку в список для удаления
                    }
                }

                // Если найдены совпадения для текущей строки из первого файла, добавляем их в результирующий список
                if (matchedRow.File2Data.Any())
                {
                    matchedRows.Add(matchedRow);

                    // Удаляем совпавшие строки из второго файла, чтобы не проверять их повторно
                    foreach (var row in rowsToRemove)
                    {
                        dataFile2.Remove(row);
                    }
                }
            }

            return matchedRows;
        }


        private bool IsRowEmptyOrIncomplete(Dictionary<string, string> row, List<string> headers)
        {
            // Проверяем на наличие всех заголовков и непустых значений
            foreach (var header in headers)
            {
                if (!row.ContainsKey(header) || string.IsNullOrWhiteSpace(row[header]))
                {
                    return true; // Строка неполная или содержит пустые значения
                }
            }
            return false; // Строка полная
        }

        private bool CompareValues(object value1, object value2)
        {
            // Создаем объект CultureInfo для русской культуры
            var culture = _culture;

            // Преобразование в DateTime и сравнение по дню, месяцу и году
            if (DateTime.TryParse(value1?.ToString(), culture, DateTimeStyles.None, out DateTime date1) &&
                DateTime.TryParse(value2?.ToString(), culture, DateTimeStyles.None, out DateTime date2))
            {
                return date1.Date == date2.Date;  // Сравниваем только дату, без учета времени
            }

            // Преобразование в decimal и сравнение только целых частей
            if (decimal.TryParse(value1?.ToString(), NumberStyles.Any, culture, out decimal decimal1) &&
                decimal.TryParse(value2?.ToString(), NumberStyles.Any, culture, out decimal decimal2))
            {
                return Math.Floor(decimal1) == Math.Floor(decimal2);  // Сравниваем только целые части
            }

            // Преобразование в int
            if (int.TryParse(value1?.ToString(), NumberStyles.Any, culture, out int int1) &&
                int.TryParse(value2?.ToString(), NumberStyles.Any, culture, out int int2))
            {
                return int1 == int2;
            }

            // Сравнение как строки (по умолчанию)
            return string.Equals(value1?.ToString(), value2?.ToString(), StringComparison.OrdinalIgnoreCase);
        }

    }
}
