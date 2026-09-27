using System.Net;

namespace CreditCalculator.Application.Exceptions;

public class BusinessRuleException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public BusinessRuleException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) : base(message)
    {
        StatusCode = statusCode;
    }
}
