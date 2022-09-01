using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using MetaFrm.Api.Models;

namespace MetaFrm.ApiServer.Auth
{
    /// <summary>
    /// AuthorizeFilter
    /// </summary>
    public class AuthorizeFilter : IAuthorizationFilter
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
            var token = context.HttpContext.Request.Headers["token"];

            switch (context.HttpContext.Request.Path.Value)
            {
                case "/api/AccessCode":
                    var accessGroup = context.HttpContext.Request.Headers["accessGroup"];

                    if (Authorize.AuthorizeTokenList.Any(x => x.Key == token))
                        return;


                    if (accessGroup == "JOIN")
                    {
                        var projectServiceBase1 = token.ToString().AesDecryptorAndDeserialize<ProjectServiceBase>();

                        if (accessGroup == "JOIN" && (projectServiceBase1 == null || projectServiceBase1.ProjectID != Factory.ProjectID))
                            throw new MetaFrmException("Token error.");
                    }

                    if (accessGroup == "JOIN")
                        return;

                    break;

                case "/api/Service":
                    if (Authorize.AuthorizeTokenList.Any(x => x.Key == token))
                        return;

                    var projectServiceBase2 = token.ToString().AesDecryptorAndDeserialize<ProjectServiceBase>();

                    if ((projectServiceBase2 == null || projectServiceBase2.ProjectID != Factory.ProjectID))
                        throw new MetaFrmException("Token error.");

                    if (projectServiceBase2.ProjectID == Factory.ProjectID)
                        return;

                    break;
            }

            if (!Authorize.AuthorizeTokenList.ContainsKey(token) || Authorize.AuthorizeTokenList[token].IsExpired)
            {
                context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류
            }

            //var hasClaim = context.HttpContext.User.Claims.Any(c => c.Type == _claim.Type && c.Value == _claim.Value);
            //if (!hasClaim)
            //{
            //    context.Result = new ForbidResult();//권한 없음
            //    context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류
            //}
        }
    }
}