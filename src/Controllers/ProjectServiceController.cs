using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// ProjectServiceController
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="_"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectServiceController(ILogger<ProjectServiceController> logger, Factory _) : ControllerBase, ICore
    {
        private readonly ILogger<ProjectServiceController> _logger = logger;
        private readonly Factory factory = _;

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="accessKey"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult? Get([FromHeader] string accessKey)
        {
            var projectServiceBase = accessKey.AesDecryptorAndDeserialize<ProjectServiceBase>();

            if (projectServiceBase == null)
                return this.Unauthorized("AccessKey error(projectServiceBase is null).");

            if (Factory.ProjectServiceBase == null)
                return this.Unauthorized("AccessKey error(Factory.ProjectServiceBase is null).");

            if (projectServiceBase.ProjectID != Factory.ProjectServiceBase.ProjectID)
                return this.Unauthorized("AccessKey error.");

            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/ProjectService")
                {
                    Headers = {
                        { HeaderNames.Accept, MediaTypeNames.Application.Json },
                        { "AccessKey", accessKey },
                    }
                };

                HttpResponseMessage httpResponseMessage = Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    ProjectService? projectService;
                    projectService = httpResponseMessage.Content.ReadFromJsonAsync<ProjectService>().Result;

                    if (projectService != null)
                    {
                        projectService.Token = Authorize.CreateToken(projectServiceBase.ProjectID, projectServiceBase.ServiceID, "PROJECT_SERVICE", TimeSpan.FromDays(this.GetAttributeInt("ExpiryTimeSpanFromDays")), projectService.Token, this.HttpContext.Connection.RemoteIpAddress?.ToString()).Token;

                        return Ok(projectService);
                    }
                }
            }
            catch (Exception ex)
            {
                if (this._logger.IsEnabled(LogLevel.Error))
                    this._logger.LogError(ex, "GetProjectService : {Message}", ex.Message);
            }

            return Ok(null);
        }
    }
}