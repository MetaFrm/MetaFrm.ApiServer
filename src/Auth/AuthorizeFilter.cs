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
                if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("Authorization failed. {token}, {authHeader}", token, authHeader);

                context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류
                return;
            }
            else
                switch (context.HttpContext.Request.Path.Value)
                {
                    case string value when value == "/api/AccessCode" || value == $"/api/{Factory.ApiVersion}/AccessCode":
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
                                    if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("Token error. {value}, {token}, {authHeader}, {accessGroup}", value, token, authHeader, accessGroup);

                                    context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                                    return;
                                }
                                else
                                    return;
                            }
                            catch (Exception ex)
                            {
                                if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError(ex, "Token error. {value}, {token}, {authHeader}", value, token, authHeader);

                                context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                                return;
                            }
                        }

                        if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("Token error. {value}, {token}, {authHeader}, {accessGroup}", value, token, authHeader, accessGroup);

                        context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                        return;

                    case string value when value == "/api/Service" || value == $"/api/{Factory.ApiVersion}/Service":
                        if (Authorize.IsToken(token, AuthType.Login))
                            return;

                        if (Authorize.IsToken(token, AuthType.ProjectService))
                            return;

                        try
                        {
                            var projectServiceBase2 = token.ToString().AesDecryptorAndDeserialize<ProjectServiceBase>();

                            if (projectServiceBase2 == null || Factory.ProjectServiceBase == null || projectServiceBase2.ProjectID != Factory.ProjectServiceBase.ProjectID)
                            {
                                if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("Token error. {value}, {token}, {authHeader}, {ProjectID}, {ProjectID}", value, token, authHeader, projectServiceBase2?.ProjectID, Factory.ProjectServiceBase?.ProjectID);

                                context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                                return;
                            }
                            else
                                return;
                        }
                        catch (Exception ex)
                        {
                            if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError(ex, "Token error. {value}, {token}, {authHeader}", value, token, authHeader);

                            context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                            return;
                        }

                        //AuthorizeToken? authorizeToken = null;

                        //authorizeToken = context.HttpContext.Request.GetAuthorizeToken();

                        //if (authorizeToken == null || Factory.ProjectServiceBase == null || authorizeToken.TokenType != AuthType.ProjectService || authorizeToken.ProjectServiceBase.ProjectID != Factory.ProjectServiceBase.ProjectID)
                        //{
                        //    if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("Token error. {value}, {token}, {authHeader}, {ProjectID}, {ProjectID}", value, token, authHeader, authorizeToken?.ProjectServiceBase.ProjectID, Factory.ProjectServiceBase?.ProjectID);

                        //    context.Result = new UnauthorizedObjectResult("Token error.");//인증 오류
                        //    return;
                        //}
                        //else
                        //    return;

                    case string value when value == "/api/Login" || value == $"/api/{Factory.ApiVersion}/Login":
                        if (!Authorize.IsToken(token, AuthType.ProjectService))
                        {
                            if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("Authorization failed. {value}, {token}, {authHeader}", value, token, authHeader);

                            context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류
                        }

                        return;

                    case string value when value == "/api/TranslationDictionary" || value == $"/api/{Factory.ApiVersion}/TranslationDictionary":
                        return;

                    default:
                        if (!Authorize.IsToken(token, AuthType.ProjectService))
                        {
                            if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("Authorization failed. {Value}, {token}, {authHeader}", context.HttpContext.Request.Path.Value, token, authHeader);

                            context.Result = new UnauthorizedObjectResult("Authorization failed.");//인증 오류
                        }

                        return;
                }
        }
    }
}