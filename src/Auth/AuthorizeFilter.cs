using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using MetaFrm.Api.Models;

namespace MetaFrm.ApiServer.Auth
{
    /// <summary>
    /// AuthorizeFilter
    /// </summary>
    public class AuthorizeFilter : IAuthorizationFilter, ICore
    {
        /// <summary>
        /// AuthorizeFilter class 생성자
        /// </summary>
        public AuthorizeFilter() { }

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
                    if (Authorize.IsToken(token))
                        return;

                    var accessGroup = context.HttpContext.Request.Headers["accessGroup"];

                    if (accessGroup == "JOIN")
                    {
                        var projectServiceBase1 = token.ToString().AesDecryptorAndDeserialize<ProjectServiceBase>();

                        if (projectServiceBase1 == null || projectServiceBase1.ProjectID != Factory.ProjectID)
                            throw new MetaFrmException("Token error.");
                    }
                    return;

                case "/api/Service":
                    if (Authorize.IsToken(token))
                        return;

                    var projectServiceBase2 = token.ToString().AesDecryptorAndDeserialize<ProjectServiceBase>();

                    if ((projectServiceBase2 == null || projectServiceBase2.ProjectID != Factory.ProjectID))
                        throw new MetaFrmException("Token error.");
                    return;
            }

            if (!Authorize.IsToken(token))
                context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류

            //var hasClaim = context.HttpContext.User.Claims.Any(c => c.Type == _claim.Type && c.Value == _claim.Value);
            //if (!hasClaim)
            //{
            //    context.Result = new ForbidResult();//권한 없음
            //    context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류
            //}
        }
    }
}