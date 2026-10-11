using System.Net;

namespace ECommerce.Application.Common.Exceptions;

public class ValidationException : ApiException
{
    public ValidationException(List<string> errors)
        : base("One or more validation failures have occurred.", HttpStatusCode.BadRequest, errors)
    {
    }

    public ValidationException(string error)
        : base(error, HttpStatusCode.BadRequest, new List<string> { error })
    {
    }
}
