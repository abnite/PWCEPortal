using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using PWCEPortal.Models;

namespace PWCEPortal.Controllers;

public class ErrorController : Controller
{
    // GET
    public IActionResult Index()
    {
        return View();
    }
    
    [Route("Error/{statusCode}")]
    public IActionResult HttpStatusCodeHandler(int statusCode)
    {
        var error = new ErrorViewModel
        {
            StatusCode = statusCode,
            StatusDescription = ReasonPhrases.GetReasonPhrase(statusCode),
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        };

        switch (statusCode)
        {
            case 400:
                error.Message = "Bad request - The server cannot process your request.";
                break;
            case 401:
                error.Message = "Unauthorized - Please login to access this resource.";
                break;
            case 403:
                error.Message = "Forbidden - You don't have permission to access this resource.";
                break;
            case 404:
                error.Message = "Page not found - The requested resource doesn't exist.";
                break;
            case 500:
                error.Message = "Internal server error - Something went wrong on our side.";
                break;
            default:
                error.Message = "An unexpected error occurred.";
                break;
        }

        return View("Error", error);
    }

    [Route("Error")]
    public IActionResult GlobalErrorHandler()
    {
        var error = new ErrorViewModel
        {
            StatusCode = 500,
            StatusDescription = "Internal Server Error",
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Message = "An unexpected error occurred while processing your request."
        };

        return View("Error", error);
    }
}