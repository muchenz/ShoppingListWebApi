using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShoppingListWebApi.Data;


public static class AuthenticationFailedContextExtension
{
    public static List<string> GetSchemes(this AuthenticationFailedContext context)
    {
        List<string> schemes = null;
        IEnumerable<AuthorizeAttribute> authorizeAttributes = null;
        try
        {
            authorizeAttributes = context.HttpContext.GetEndpoint()?
                                             .Metadata
                                             .GetOrderedMetadata<AuthorizeAttribute>() ?? Enumerable.Empty<AuthorizeAttribute>();
            schemes = authorizeAttributes
               .Select(a => (a.AuthenticationSchemes?
                             .Split(',', StringSplitOptions.RemoveEmptyEntries)))
               .Where(a => a != null)
               .SelectMany(a => a).ToList();
            return schemes;

        }
        catch (Exception ex)
        {
            return new List<string>();

        }
    }
}

//public static class AuthenticationFailedContextExtension
//{
//    extension(AuthenticationFailedContext context)
//    {

//          public static List<string> GetSchemes()
//            {
//                List<string> schemes = null;
//                IEnumerable<AuthorizeAttribute> authorizeAttributes = null;
//                try
//                {
//                    authorizeAttributes = context.HttpContext.GetEndpoint()?
//                                                     .Metadata
//                                                     .GetOrderedMetadata<AuthorizeAttribute>() ?? Enumerable.Empty<AuthorizeAttribute>();
//                    schemes = authorizeAttributes
//                       .Select(a => (a.AuthenticationSchemes?
//                                     .Split(',', StringSplitOptions.RemoveEmptyEntries)))
//                       .Where(a => a != null)
//                       .SelectMany(a => a).ToList();
//                    return schemes;

//                }
//                catch (Exception ex)
//                {
//                    return new List<string>();

//                }
//            }
//    }
//}

