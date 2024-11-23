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
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectServiceController : ControllerBase, ICore
    {
        private readonly ILogger<ProjectServiceController> _logger;

        /// <summary>
        /// ProjectServiceController
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="factory"></param>
        public ProjectServiceController(ILogger<ProjectServiceController> logger, Factory factory)
        {
            _logger = logger;
        }

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

            if (projectServiceBase == null || projectServiceBase.ProjectID != Factory.ProjectServiceBase.ProjectID)
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
                        projectService.Token = Authorize.CreateToken(projectServiceBase.ProjectID, projectServiceBase.ServiceID, "PROJECT_SERVICE", TimeSpan.FromDays(365), projectService.Token, this.HttpContext.Connection.RemoteIpAddress?.ToString()).GetToken;

                        return Ok(projectService);
                    }
                }
            }
            catch (Exception ex)
            {
                StreamWriter? streamWriter = null;

                try
                {
                    //Factory.SaveInstance(ex, $"{path}e");

                    streamWriter = System.IO.File.CreateText($"{path}e");
                    streamWriter.Write(ex.ToString());
                }
                catch (Exception)
                {
                }
                finally
                {
                    streamWriter?.Close();
                }
            }

            return Ok(null);
        }
    }
}