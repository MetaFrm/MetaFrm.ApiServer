using MetaFrm.Api;
using MetaFrm.Api.Models;
using MetaFrm.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace MetaFrm.ApiServer.Auth
{
    /// <summary>
    /// AuthorizeFilter
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="_"></param>
    public class AuthorizeFilter(ILogger<AuthorizeFilter> logger, Factory _) : IAuthorizationFilter, ICore
    {
        private readonly ILogger<AuthorizeFilter> _logger = logger;
        private readonly Factory factory = _;

        /// <summary>
        /// Called early in the filter pipeline to confirm request is authorized.
        /// </summary>
        /// <param name="context"></param>
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            string? token = context.HttpContext.Request.Headers["token"];

            string? authHeader = context.HttpContext.Request.Headers.Authorization;

            if (authHeader != null && authHeader.StartsWith($"{Headers.Bearer} "))
                token = authHeader.Split(' ')[1];

            if (token == null)
            {
                this._logger.Error("Authorization failed. {0}, {1}", token, authHeader);

                context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류
                return;
            }
            else
                switch (context.HttpContext.Request.Path.Value)
                {
                    case string value when value == $"/api/v1/AccessCode" || value == $"/api/v2/AccessCode":
                        if (Authorize.IsToken(token, AuthType.Login))
                            return;

                        string? accessGroup = context.HttpContext.Request.Headers[Headers.AccessGroup];

                        accessGroup ??= context.HttpContext.Request.Headers["accessGroup"];

                        if (accessGroup == AuthType.Join)
                        {
                            if (Authorize.IsToken(token, AuthType.ProjectService))
                                return;

                            try
                            {
                                var projectServiceBase1 = token.AesDecryptorAndDeserialize<ProjectServiceBase>();

                                if (projectServiceBase1 == null || Factory.ProjectServiceBase == null || projectServiceBase1.ProjectID != Factory.ProjectServiceBase.ProjectID)
                                {
                                    this._logger.Error("Token error. {0}, {1}, {2}, {3}", value, token, authHeader, accessGroup);

                                    context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                                    return;
                                }
                                else
                                    return;
                            }
                            catch (Exception ex)
                            {
                                this._logger.Error(ex, "Token error. {0}, {1}, {2}", value, token, authHeader);

                                context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                                return;
                            }
                        }

                        this._logger.Error("Token error. {0}, {1}, {2}, {3}", value, token, authHeader, accessGroup);

                        context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                        return;

                    case string value when value == $"/api/v1/Service" || value == $"/api/v2/Service":
                        if (Authorize.IsToken(token, AuthType.Login))
                            return;

                        if (Authorize.IsToken(token, AuthType.ProjectService))
                            return;

                        try
                        {
                            var projectServiceBase2 = token.ToString().AesDecryptorAndDeserialize<ProjectServiceBase>();

                            if (projectServiceBase2 == null || Factory.ProjectServiceBase == null || projectServiceBase2.ProjectID != Factory.ProjectServiceBase.ProjectID)
                            {
                                this._logger.Error("Token error. {0}, {1}, {2}, {3}, {4}", value, token, authHeader, projectServiceBase2?.ProjectID, Factory.ProjectServiceBase?.ProjectID);

                                context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                                return;
                            }
                            else
                                return;
                        }
                        catch (Exception ex)
                        {
                            this._logger.Error(ex, "Token error. {0}, {1}, {2}", value, token, authHeader);

                            context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                            return;
                        }

                    case string value when value == $"/api/v1/Login" || value == $"/api/v2/Login":
                        if (!Authorize.IsToken(token, AuthType.ProjectService))
                        {
                            this._logger.Error("Authorization failed. {0}, {1}, {2}", value, token, authHeader);

                            context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류
                        }

                        return;

                    case string value when value == $"/api/v1/TranslationDictionary" || value == $"/api/v2/TranslationDictionary":
                        if (!Authorize.IsToken(token, AuthType.ProjectService))
                        {
                            this._logger.Error("Authorization failed. {0}, {1}, {2}", value, token, authHeader);

                            context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류
                        }

                        return;

                    default:
                        if (!Authorize.IsToken(token, AuthType.ProjectService))
                        {
                            this._logger.Error("Authorization failed. {0}, {1}, {2}", context.HttpContext.Request.Path.Value, token, authHeader);

                            context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류
                        }

                        return;
                }
        }
    }
}