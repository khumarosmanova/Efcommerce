using System.Net;

namespace ECommerce.Application.Common.Exceptions;

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message = "You are not authorized to perform this action.")
        : base(message, HttpStatusCode.Unauthorized)
    {
    }
}
