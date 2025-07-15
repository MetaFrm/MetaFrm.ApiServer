using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using MetaFrm.Api.Models;

namespace MetaFrm.ApiServer.Auth
{
    /// <summary>
    /// AuthorizeFilter
    /// </summary>
    /// <param name="_"></param>
    public class AuthorizeFilter(Factory _) : IAuthorizationFilter, ICore
    {
        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized.
        /// </summary>
        /// <param name="context"></param>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            string? token = context.HttpContext.Request.Headers["token"];

            if (token == null)
            {
                context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류
                return;
            }

            switch (context.HttpContext.Request.Path.Value)
            {
                case "/api/AccessCode":
                    if (Authorize.IsToken(token, "LOGIN"))
                        return;

                    var accessGroup = context.HttpContext.Request.Headers["accessGroup"];

                    if (accessGroup == "JOIN")
                    {
                        var projectServiceBase1 = token.ToString().AesDecryptorAndDeserialize<ProjectServiceBase>();

                        if (projectServiceBase1 == null || Factory.ProjectServiceBase == null || projectServiceBase1.ProjectID != Factory.ProjectServiceBase.ProjectID)
                            context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                    }
                    return;

                case "/api/Service":
                    if (Authorize.IsToken(token, "LOGIN"))
                        return;

                    try
                    {
                        var projectServiceBase2 = token.ToString().AesDecryptorAndDeserialize<ProjectServiceBase>();

                        if ((projectServiceBase2 == null || Factory.ProjectServiceBase == null || projectServiceBase2.ProjectID != Factory.ProjectServiceBase.ProjectID))
                            context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                    }
                    catch (Exception)
                    {
                        context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                    }

                    return;
            }

            if (!Authorize.IsToken(token, "PROJECT_SERVICE"))
                context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류
        }
    }
}