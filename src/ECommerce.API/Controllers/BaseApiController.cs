using ECommerce.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult HandleResult<T>(ApiResponse<T> result)
    {
        if (result.Succeeded)
        {
            return Ok(result);
        }

        return BadRequest(result);
    }

    protected IActionResult Success<T>(T data, string message = "Success")
    {
        return Ok(ApiResponse<T>.Success(data, message));
    }

    protected IActionResult Success(string message = "Success")
    {
        return Ok(ApiResponse.Success(message));
    }

    protected IActionResult Failure(string message, List<string>? errors = null, int statusCode = 400)
    {
        return StatusCode(statusCode, ApiResponse.Failure(message, errors));
    }
}
