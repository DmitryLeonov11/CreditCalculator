using CreditCalculator.Calculations.Models;
using ClosedXML.Excel;

namespace CreditCalculator.Api.Export;

public static class PaymentScheduleExcelExporter
{
    public static byte[] Generate(PaymentScheduleResult schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("График платежей");

        worksheet.Cell("A1").Value = "График платежей";
        worksheet.Range("A1:F1").Merge();
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 16;

        worksheet.Cell("A2").Value = "Всего выплачено";
        worksheet.Cell("B2").Value = schedule.TotalPaid;
        worksheet.Cell("C2").Value = "Переплата";
        worksheet.Cell("D2").Value = schedule.Overpayment;
        worksheet.Range("B2").Style.NumberFormat.Format = MoneyNumberFormat;
        worksheet.Range("D2").Style.NumberFormat.Format = MoneyNumberFormat;
        worksheet.Range("A2:D2").Style.Font.Bold = true;

        var headers = new[] { "№", "Дата платежа", "Платёж, BYN", "Проценты, BYN", "Основной долг, BYN", "Остаток долга, BYN" };
        for (var column = 0; column < headers.Length; column++)
            worksheet.Cell(4, column + 1).Value = headers[column];

        var row = 5;
        foreach (var payment in schedule.Payments)
        {
            worksheet.Cell(row, 1).Value = payment.Number;
            worksheet.Cell(row, 2).Value = payment.Date.ToDateTime(TimeOnly.MinValue);
            worksheet.Cell(row, 3).Value = payment.Payment;
            worksheet.Cell(row, 4).Value = payment.InterestPart;
            worksheet.Cell(row, 5).Value = payment.PrincipalPart;
            worksheet.Cell(row, 6).Value = payment.RemainingBalance;
            row++;
        }

        var lastRow = Math.Max(4, row - 1);
        worksheet.Range(4, 1, 4, 6).Style.Font.Bold = true;
        worksheet.Range(4, 1, 4, 6).Style.Fill.BackgroundColor = XLColor.LightGray;
        if (row > 5)
        {
            worksheet.Range(5, 2, row - 1, 2).Style.DateFormat.Format = "dd.mm.yyyy";
            worksheet.Range(5, 3, row - 1, 6).Style.NumberFormat.Format = MoneyNumberFormat;
        }

        worksheet.Range(4, 1, lastRow, 6).SetAutoFilter();
        worksheet.SheetView.FreezeRows(4);
        worksheet.Columns(1, 6).AdjustToContents();
        worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 23);
        worksheet.Column(6).Width = Math.Max(worksheet.Column(6).Width, 23);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private const string MoneyNumberFormat = "#,##0.00 \"BYN\"";
}