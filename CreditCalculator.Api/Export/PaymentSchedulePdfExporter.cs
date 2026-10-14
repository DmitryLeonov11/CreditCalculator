using System.Globalization;
using CreditCalculator.Calculations.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CreditCalculator.Api.Export;

public static class PaymentSchedulePdfExporter
{
    private static readonly CultureInfo BelarusianCulture = CultureInfo.GetCultureInfo("ru-BY");

    public static byte[] Generate(string title, PaymentScheduleResult schedule)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(schedule);

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(28);
            page.DefaultTextStyle(style => style.FontSize(9));
            page.Header().Text(title).FontSize(18).Bold();
            page.Content().PaddingVertical(12).Column(column =>
            {
                column.Item().Text($"Всего выплачено: {FormatMoney(schedule.TotalPaid)}     Переплата: {FormatMoney(schedule.Overpayment)}")
                    .FontSize(11).SemiBold();
                column.Item().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(32);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.4f);
                        columns.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("№");
                        header.Cell().Element(HeaderCell).Text("Дата платежа");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Платёж");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Проценты");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Основной долг");
                        header.Cell().Element(HeaderCell).AlignRight().Text("Остаток долга");
                    });

                    foreach (var payment in schedule.Payments)
                    {
                        table.Cell().Element(BodyCell).Text(payment.Number.ToString(BelarusianCulture));
                        table.Cell().Element(BodyCell).Text(payment.Date.ToString("d", BelarusianCulture));
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(payment.Payment));
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(payment.InterestPart));
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(payment.PrincipalPart));
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatMoney(payment.RemainingBalance));
                    }
                });
            });
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Страница ");
                text.CurrentPageNumber();
                text.Span(" из ");
                text.TotalPages();
            });
        })).GeneratePdf();
    }

    private static string FormatMoney(decimal amount) => $"{amount.ToString("N2", BelarusianCulture)} BYN";

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten2).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(5).DefaultTextStyle(style => style.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5);
}