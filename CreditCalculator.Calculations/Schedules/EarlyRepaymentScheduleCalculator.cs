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
            var principalPart = number == termMonths ? balance : Math.Min(payment - interest, balance);

            balance -= principalPart;
            balance = ApplyExtraRepayment(earlyRepaymentsByMonth, number, balance, ref principalPart);

            var paymentAmount = principalPart + interest;
            payments.Add(new PaymentScheduleItem(number, date, paymentAmount, interest, principalPart, balance));
        }

        return BuildResult(payments, principal);
    }

    public static PaymentScheduleResult BuildReducePaymentSchedule(
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
            var principalPart = number == termMonths ? balance : Math.Min(payment - interest, balance);

            balance -= principalPart;

            var balanceBeforeExtra = balance;
            balance = ApplyExtraRepayment(earlyRepaymentsByMonth, number, balance, ref principalPart);

            var remainingMonths = termMonths - number;
            var extraRepaymentApplied = balanceBeforeExtra - balance;
            if (extraRepaymentApplied > 0m && balance > 0m && remainingMonths > 0)
                payment = AnnuityScheduleCalculator.CalculateMonthlyPayment(balance, annualRatePercent, remainingMonths);

            var paymentAmount = principalPart + interest;
            payments.Add(new PaymentScheduleItem(number, date, paymentAmount, interest, principalPart, balance));
        }

        return BuildResult(payments, principal);
    }

    private static decimal ApplyExtraRepayment(
        ILookup<int, EarlyRepayment> earlyRepaymentsByMonth,
        int month,
        decimal balance,
        ref decimal principalPart)
    {
        var extraRepayment = earlyRepaymentsByMonth[month].Sum(item => item.Amount);
        if (extraRepayment <= 0m)
            return balance;

        extraRepayment = Math.Min(extraRepayment, balance);
        principalPart += extraRepayment;
        return balance - extraRepayment;
    }

    private static PaymentScheduleResult BuildResult(List<PaymentScheduleItem> payments, decimal principal)
    {
        var totalPaid = payments.Sum(item => item.Payment);
        var overpayment = totalPaid - principal;

        return new PaymentScheduleResult(payments, totalPaid, overpayment);
    }
}
