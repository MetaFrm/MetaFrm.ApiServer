using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// ProjectServiceController
    /// </summary>
    /// <remarks>
    /// ProjectServiceController
    /// </remarks>
    /// <param name="logger"></param>
    /// <param name="factory"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectServiceController(ILogger<ProjectServiceController> logger, Factory factory) : ControllerBase, ICore
    {
        private readonly ILogger<ProjectServiceController> _logger = logger;
        private readonly Factory _factory = factory;

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="accessKey"></param>
        /// <returns></returns>
        [HttpGet(Name = "GetProjectService")]
        public IActionResult? Get([FromHeader] string accessKey)
        {
            string path;

            var projectServiceBase = accessKey.AesDecryptorAndDeserialize<ProjectServiceBase>();

            if (projectServiceBase == null)
                return this.Unauthorized("AccessKey error(projectServiceBase is null).");

            if (Factory.ProjectServiceBase == null)
                return this.Unauthorized("AccessKey error(Factory.ProjectServiceBase is null).");

            if (projectServiceBase.ProjectID != Factory.ProjectServiceBase.ProjectID)
                return this.Unauthorized("AccessKey error.");

            path = $"{Factory.FolderPathDat}{projectServiceBase.ProjectID}_{projectServiceBase.ServiceID}_{DateTime.Now:dd HH:mm:ss}_A_PS.dat";

            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/ProjectService")
                {
                    Headers = {
                        { HeaderNames.Accept, "application/json" },
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
                        projectService.Token = Authorize.CreateToken(projectServiceBase.ProjectID, projectServiceBase.ServiceID, "PROJECT_SERVICE", TimeSpan.FromDays(this.GetAttributeInt("ExpiryTimeSpanFromDays")), projectService.Token, this.HttpContext.Connection.RemoteIpAddress?.ToString()).GetToken;

                        return Ok(projectService);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetProjectService : {Message}", ex.Message);
            }

            return Ok(null);
        }
    }
}