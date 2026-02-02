using MetaFrm.Api;
using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using MetaFrm.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;

namespace MetaFrm.ApiServer.Controllers.V2
{
    /// <summary>
    /// ProjectServiceController
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="_"></param>
    [Route("api/v2/[controller]")]
    [ApiController]
    public class ProjectServiceController(ILogger<ProjectServiceController> logger, Factory _) : ControllerBase, ICore
    {
        private readonly ILogger<ProjectServiceController> _logger = logger;
        private readonly Factory factory = _;

        private static int? ExpiryTimeSpanFromDays;

        /// <summary>
        /// Get
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult? Get()
        {
            ProjectServiceBase? projectServiceBase;

            string? accessKey = Request.Headers.Authorization;

            if (accessKey == null || !accessKey.StartsWith($"{Headers.Bearer} "))
            {
                if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("Invalid access key. {accessKey}", accessKey);

                return this.Unauthorized("Invalid access key.");
            }
            else
            {
                accessKey = accessKey.Split(' ')[1];

                try
                {
                    projectServiceBase = accessKey?.AesDecryptorAndDeserialize<ProjectServiceBase>();
                }
                catch (Exception ex)
                {
                    if (this._logger.IsEnabled(LogLevel.Error))
                        this._logger.LogError(ex, "{accessKey}", accessKey);

                    return this.Unauthorized("Invalid access key.");
                }

                if (projectServiceBase == null || Factory.ProjectServiceBase == null || projectServiceBase.ProjectID != Factory.ProjectServiceBase.ProjectID)
                {
                    if (this._logger.IsEnabled(LogLevel.Error))
                        this._logger.LogError("Invalid access key. {accessKey}, {ProjectID}, {ServiceID}, {ProjectID}", accessKey, projectServiceBase?.ProjectID, projectServiceBase?.ServiceID, Factory.ProjectServiceBase?.ProjectID);

                    return this.Unauthorized("Invalid access key.");
                }

                try
                {
                    HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/{Factory.ApiVersion}/ProjectService")
                    {
                        Headers = {
                        { HeaderNames.Accept, MediaTypeNames.Application.Json },
                    }
                    };

                    httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(Headers.Bearer, accessKey);

                    HttpResponseMessage httpResponseMessage = Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).Result;

                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        ProjectServiceShort1? projectService;
                        projectService = httpResponseMessage.Content.ReadFromJsonAsync<ProjectServiceShort1>().Result;

                        if (projectService != null)
                        {
                            if (httpResponseMessage.Headers.TryGetValues(Headers.DeployToken, out var valuesAccessToken))
                                Response.Headers[Headers.DeployToken] = valuesAccessToken.First();
                            if (httpResponseMessage.Headers.TryGetValues(Headers.DeployTokenExpire, out var valuesAccessTokenExpire))
                                Response.Headers[Headers.DeployTokenExpire] = valuesAccessTokenExpire.First();

                            ExpiryTimeSpanFromDays ??= this.GetAttributeInt("ExpiryTimeSpanFromDays");

                            AuthorizeToken authorizeToken = Authorize.CreateToken(projectServiceBase.ProjectID, projectServiceBase.ServiceID, AuthType.ProjectService, TimeSpan.FromDays((int)ExpiryTimeSpanFromDays), Response.Headers[Headers.DeployToken], this.HttpContext.Connection.RemoteIpAddress?.ToString());

                            Response.Headers[Headers.AccessToken] = authorizeToken.Token;
                            Response.Headers[Headers.AccessTokenExpire] = authorizeToken.ExpiryDate.ToUniversalTime().ToString("O");

                            return Ok(projectService);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (this._logger.IsEnabled(LogLevel.Error))
                        this._logger.LogError(ex, "{accessKey}, {ProjectID}, {ServiceID}", accessKey, projectServiceBase.ProjectID, projectServiceBase.ServiceID);
                }

                return Ok(null);
            }
        }
    }
}