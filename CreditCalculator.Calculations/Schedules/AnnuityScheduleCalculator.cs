using CreditCalculator.Calculations.Models;
using CreditCalculator.Calculations.Validation;

namespace CreditCalculator.Calculations.Schedules;

public static class AnnuityScheduleCalculator
{
    public static decimal CalculateMonthlyPayment(decimal principal, decimal annualRatePercent, int termMonths)
    {
        ScheduleInputValidator.Validate(principal, annualRatePercent, termMonths);

        var monthlyRate = annualRatePercent / 12 / 100;

        if (monthlyRate == 0m)
            return Math.Round(principal / termMonths, 2, MidpointRounding.AwayFromZero);

        var factor = 1m - 1m / DecimalPow(1m + monthlyRate, termMonths);
        return Math.Round(principal * monthlyRate / factor, 2, MidpointRounding.AwayFromZero);
    }

    public static PaymentScheduleResult BuildSchedule(decimal principal, decimal annualRatePercent, int termMonths, DateOnly firstPaymentDate)
    {
        ScheduleInputValidator.Validate(principal, annualRatePercent, termMonths);

        var monthlyRate = annualRatePercent / 12 / 100;
        var payment = CalculateMonthlyPayment(principal, annualRatePercent, termMonths);

        var payments = new List<PaymentScheduleItem>(termMonths);
        var balance = principal;

        for (var number = 1; number <= termMonths; number++)
        {
            var date = firstPaymentDate.AddMonths(number - 1);
            var interest = Math.Round(balance * monthlyRate, 2, MidpointRounding.AwayFromZero);

            decimal principalPart;
            decimal paymentAmount;

            if (number == termMonths)
            {
                principalPart = balance;
                paymentAmount = principalPart + interest;
            }
            else
            {
                principalPart = payment - interest;
                paymentAmount = payment;
            }

            balance -= principalPart;
            payments.Add(new PaymentScheduleItem(number, date, paymentAmount, interest, principalPart, balance));
        }

        var totalPaid = payments.Sum(item => item.Payment);
        var overpayment = totalPaid - principal;

        return new PaymentScheduleResult(payments, totalPaid, overpayment);
    }

    private static decimal DecimalPow(decimal value, int exponent)
    {
        var result = 1m;
        for (var i = 0; i < exponent; i++)
            result *= value;

        return result;
    }
}
