using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Shared.DataEndpoints.Models;
using System.Linq;

namespace ShoppingListWebApi.Controllers;

public class ApiControllerBase: ControllerBase
{
    public  ActionResult ReturnResultError(Result result)
    {

        var errors = result.GetErrors();

        if (errors.All(a => a.ErrorType == ErrorTypes.ValidationError))
        {
            return ValidationErrors(errors);
        }


        var error = result.GetError();


        //ObjectResult objectResult = error switch
        //{
        //    { ErrorType: ErrorTypes.Conflict } => controller.Problem(title:error.Description, statusCode:GetHttpCode(error)),
        //    { ErrorType: ErrorTypes.None } => controller.Problem(title: error.Description, statusCode: GetHttpCode(error)),
        //    { ErrorType: ErrorTypes.NotFound } => controller.Problem(title: error.Description, statusCode: GetHttpCode(error)),
        //    { ErrorType: ErrorTypes.Forbidden } => controller.Problem(title: error.Description, statusCode: GetHttpCode(error)),
        //    { ErrorType: ErrorTypes.ValidationError } => controller.Problem(title: error.Description, statusCode: GetHttpCode(error)),
        //    _ => new BadRequestObjectResult("Something wrong happens")

        //};

        return ReturnResultError(error);
    }

    public  ActionResult ReturnResultError(Error error)
    {
        ObjectResult objectResult = Problem(title: error.Description, statusCode: GetHttpCode(error));

        return objectResult;
    }
    private  ActionResult ValidationErrors(Error[] errors)
    {

        var stateDictionary = new ModelStateDictionary();

        foreach (var error in errors)
        {
            stateDictionary.AddModelError(error.Code, error.Description);
        }

        var valProblem = ValidationProblem(stateDictionary);

        return valProblem;

    }

    int GetHttpCode(Error error)
    {
        return error.ErrorType switch
        {
            ErrorTypes.Conflict => StatusCodes.Status409Conflict,
            ErrorTypes.None => StatusCodes.Status200OK,
            ErrorTypes.NotFound => StatusCodes.Status404NotFound,
            ErrorTypes.Forbidden => StatusCodes.Status403Forbidden,
            ErrorTypes.ValidationError => StatusCodes.Status400BadRequest,
            ErrorTypes.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorTypes.Unexpected => StatusCodes.Status500InternalServerError,
            _ => 500
        };
    }
}
