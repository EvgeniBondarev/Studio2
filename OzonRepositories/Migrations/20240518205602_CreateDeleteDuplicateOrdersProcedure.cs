using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzonRepositories.Migrations
{
    //update-database -context "OzonOrderContext" -Migration "CreateDeleteDuplicateOrdersProcedure"

    //DECLARE @DuplicateCount INT, @DeletedRowsCount INT;
    //EXEC DeleteDuplicateOrders @DuplicateCount OUTPUT, @DeletedRowsCount OUTPUT;
    //PRINT 'Количество дубликатов: ' + CAST(@DuplicateCount AS VARCHAR(10));
    //PRINT 'Количество удаленных записей: ' + CAST(@DeletedRowsCount AS VARCHAR(10));

    public partial class CreateDeleteDuplicateOrdersProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE PROCEDURE DeleteDuplicateOrders
                    @DuplicateCount INT OUTPUT,
                    @DeletedRowsCount INT OUTPUT
                AS
                BEGIN
                    BEGIN TRANSACTION;

                    -- Найти записи для удаления, оставив только одну из групп с наибольшим количеством NULL значений
                    WITH RowsToDelete AS (
                        SELECT 
                            *,
                            ROW_NUMBER() OVER (PARTITION BY 
                                [Key],
                                ShipmentNumber,
                                ProcessingDate,
                                ShippingDate,
                                Status,
                                ShipmentAmount,
                                ProductName,
                                Article,
                                Price,
                                Quantity,
                                PurchasePrice
                            ORDER BY 
                                CASE WHEN MaxOzonCommission IS NULL THEN 1 ELSE 0 END +
                                CASE WHEN MaxCommissionInfo IS NULL THEN 1 ELSE 0 END +
                                CASE WHEN MinCommissionInfo IS NULL THEN 1 ELSE 0 END +
                                CASE WHEN MinProfit IS NULL THEN 1 ELSE 0 END +
                                CASE WHEN MaxProfit IS NULL THEN 1 ELSE 0 END +
                                CASE WHEN MinDiscount IS NULL THEN 1 ELSE 0 END +
                                CASE WHEN MaxDiscount IS NULL THEN 1 ELSE 0 END +
                                CASE WHEN SupplierId IS NULL THEN 1 ELSE 0 END +
                                CASE WHEN AppStatusId IS NULL THEN 1 ELSE 0 END +
                                CASE WHEN OrderNumberToSupplier IS NULL THEN 1 ELSE 0 END DESC
                            ) AS RowNum
                        FROM Orders
                    )

                    -- Удалить строки
                    DELETE FROM Orders
                    WHERE Id IN (
                        SELECT Id
                        FROM RowsToDelete
                        WHERE RowNum > 1
                    );

                    SELECT @DeletedRowsCount = @@ROWCOUNT;

                    COMMIT TRANSACTION;
                END;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS DeleteDuplicateOrders");
        }
    }
}
