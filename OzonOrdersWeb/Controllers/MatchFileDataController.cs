using Microsoft.AspNetCore.Mvc;
using OzonDomains;
using OzonOrdersWeb.ViewModels.OrderViewModels;
using Servcies.ParserServcies.FielParsers;
using Servcies.ParserServcies.FielParsers.Models;
using static Servcies.ParserServcies.FielParsers.ExcelParser;

namespace OzonOrdersWeb.Controllers
{
    public class MatchFileDataController : Controller
    {
        private readonly ExcelParser _excelParser;

        public MatchFileDataController(ExcelParser excelParser)
        {
            _excelParser = excelParser;
        }

        public IActionResult Index()
        {
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> MatchFiles(
            IFormFile file1,
            IFormFile file2,
            List<MatchingColumn> matchingColumns,
            int startRow1 = 1,
            int startColumn1 = 1,
            int startRow2 = 1,
            int startColumn2 = 1)
        {
            // Проверяем, что файлы были загружены
            if (file1 == null || file2 == null || matchingColumns == null || !matchingColumns.Any())
            {
                ModelState.AddModelError("", "Необходимо загрузить два файла и указать столбцы для сопоставления.");
                return View(); // Верните обратно на представление с ошибкой
            }

            // Открываем потоки файлов
            using (var file1Stream = file1.OpenReadStream())
            using (var file2Stream = file2.OpenReadStream())
            {
                List<MatchedRowModel> matchedResults = await _excelParser.MatchExcelDataAsync(file1Stream, file2Stream, matchingColumns, startRow1, startColumn1, startRow2, startColumn2);

                // Проверяем результаты
                if (!matchedResults.Any())
                {
                    ViewBag.Message = "Совпадений не найдено.";
                }
                else
                {
                    ViewBag.Message = $"{matchedResults.Count} совпадений найдено.";
                }

                return View("MatchResults", new MatchedRowViewModel() { MatchedResults = matchedResults, MainFileName = file1.FileName, ScondaryFileName = file2.FileName});
            }
        }

    }
}
