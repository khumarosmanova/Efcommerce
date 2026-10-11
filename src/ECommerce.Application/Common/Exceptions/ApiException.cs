using System.Net;

namespace ECommerce.Application.Common.Exceptions;

public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public List<string> Errors { get; }

    public ApiException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest, List<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? new List<string>();
    }
}
