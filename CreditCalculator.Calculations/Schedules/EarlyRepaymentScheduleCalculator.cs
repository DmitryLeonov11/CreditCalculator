using CreditCalculator.Calculations.Models;
using CreditCalculator.Calculations.Validation;

namespace CreditCalculator.Calculations.Schedules;

public static class EarlyRepaymentScheduleCalculator
{
    public static PaymentScheduleResult BuildReduceTermSchedule(
        decimal principal,
        decimal annualRatePercent,
        int termMonths,
        DateOnly firstPaymentDate,
        IReadOnlyList<EarlyRepayment> earlyRepayments)
    {
        ScheduleInputValidator.Validate(principal, annualRatePercent, termMonths);
        ScheduleInputValidator.ValidateEarlyRepayments(earlyRepayments, termMonths);

        var monthlyRate = annualRatePercent / 12 / 100;
        var payment = AnnuityScheduleCalculator.CalculateMonthlyPayment(principal, annualRatePercent, termMonths);
        var earlyRepaymentsByMonth = earlyRepayments.ToLookup(item => item.Month);

        var payments = new List<PaymentScheduleItem>();
        var balance = principal;

        for (var number = 1; number <= termMonths && balance > 0m; number++)
        {
            var date = firstPaymentDate.AddMonths(number - 1);
            var interest = Math.Round(balance * monthlyRate, 2, MidpointRounding.AwayFromZero);
            var principalPart = Math.Min(payment - interest, balance);

            balance -= principalPart;

            var extraRepayment = earlyRepaymentsByMonth[number].Sum(item => item.Amount);
            if (extraRepayment > 0m)
            {
                extraRepayment = Math.Min(extraRepayment, balance);
                principalPart += extraRepayment;
                balance -= extraRepayment;
            }

            var paymentAmount = principalPart + interest;
            payments.Add(new PaymentScheduleItem(number, date, paymentAmount, interest, principalPart, balance));
        }

        var totalPaid = payments.Sum(item => item.Payment);
        var overpayment = totalPaid - principal;

        return new PaymentScheduleResult(payments, totalPaid, overpayment);
    }
}
