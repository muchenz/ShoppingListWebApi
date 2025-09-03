using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shared.DataEndpoints.Models;
using System;
using System.Linq;

namespace ShoppingListWebApi.Data;

public static class ControllerBaseExt
{
    public static ActionResult ReturnResultError(this ControllerBase controller, Result result)
    {

        var errors = result.GetErrors();

        if (errors.All(a=>a.ErrorType == ErrorTypes.ValidationError))
        {
            return ValidationErrors(controller,  errors);
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
        ObjectResult objectResult = controller.Problem(title: error.Description, statusCode: GetHttpCode(error));
        
        return objectResult;
    }

    private static ActionResult ValidationErrors(ControllerBase controller, Error[] errors)
    {
        
        var stateDictionary = new ModelStateDictionary();

        foreach (var error in errors)
        {
            stateDictionary.AddModelError(error.Code, error.Description);
        }

        var valProblem = controller.ValidationProblem(stateDictionary);

        return valProblem;

    }

    static int GetHttpCode(Error error)
    {
        return error.ErrorType switch
        {
            ErrorTypes.Conflict => StatusCodes.Status409Conflict,
            ErrorTypes.None => StatusCodes.Status200OK,
            ErrorTypes.NotFound => StatusCodes.Status404NotFound,
            ErrorTypes.Forbidden => StatusCodes.Status403Forbidden,
            ErrorTypes.ValidationError => StatusCodes.Status400BadRequest,
            _ => 500
        };
    }
}
