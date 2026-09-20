using CreditCalculator.Calculations.Models;
using CreditCalculator.Calculations.Validation;

namespace CreditCalculator.Calculations.Schedules;

public static class DifferentiatedScheduleCalculator
{
    public static PaymentScheduleResult BuildSchedule(decimal principal, decimal annualRatePercent, int termMonths, DateOnly firstPaymentDate)
    {
        ScheduleInputValidator.Validate(principal, annualRatePercent, termMonths);

        var monthlyRate = annualRatePercent / 12 / 100;
        var principalPart = Math.Round(principal / termMonths, 2, MidpointRounding.AwayFromZero);

        var payments = new List<PaymentScheduleItem>(termMonths);
        var balance = principal;

        for (var number = 1; number <= termMonths; number++)
        {
            var date = firstPaymentDate.AddMonths(number - 1);
            var interest = Math.Round(balance * monthlyRate, 2, MidpointRounding.AwayFromZero);
            var currentPrincipalPart = number == termMonths ? balance : principalPart;
            var paymentAmount = currentPrincipalPart + interest;

            balance -= currentPrincipalPart;
            payments.Add(new PaymentScheduleItem(number, date, paymentAmount, interest, currentPrincipalPart, balance));
        }

        var totalPaid = payments.Sum(item => item.Payment);
        var overpayment = totalPaid - principal;

        return new PaymentScheduleResult(payments, totalPaid, overpayment);
    }
}
