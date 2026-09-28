using System.Net;

namespace CreditCalculator.Application.Exceptions;

public class BusinessRuleException : Domain.Exceptions.BusinessRuleException
{
    public HttpStatusCode HttpStatusCode => (HttpStatusCode)StatusCode;

    public BusinessRuleException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(message, (int)statusCode)
    {
    }

    public BusinessRuleException(string message, int statusCode)
        : base(message, statusCode)
    {
    }
}
