using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// ProjectServiceController
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectServiceController : ControllerBase, ICore
    {
        static readonly object lockObject = new();
        private readonly ILogger<ProjectServiceController> _logger;

        /// <summary>
        /// 키와 값의 컬렉션을 나타냅니다.
        /// </summary>
        private static Dictionary<string, ProjectService> ProjectServices { get; set; } = new Dictionary<string, ProjectService>();

        private readonly HttpClient httpClient;
        private static bool httpClientException;

        /// <summary>
        /// ProjectServiceController
        /// </summary>
        /// <param name="logger"></param>
        public ProjectServiceController(ILogger<ProjectServiceController> logger)
        {
            _logger = logger;

            // Update port # in the following line.
            this.httpClient = new()
            {
                BaseAddress = new Uri(Factory.BaseAddress)
            };
            this.httpClient.DefaultRequestHeaders.Accept.Clear();
            this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="accessKey"></param>
        /// <returns></returns>
        [HttpGet(Name = "GetProjectService")]
        public ProjectService? Get([FromHeader] string accessKey)
        {
            string key;
            string path;

            var projectServiceBase = accessKey.AesDecryptorAndDeserialize<ProjectServiceBase>();

            if (projectServiceBase == null || projectServiceBase.ProjectID != Factory.ProjectID)
                throw new MetaFrmException("AccessKey error.");

            key = $"{projectServiceBase.ProjectID}.{projectServiceBase.ServiceID}";
            path = $"{Factory.FolderPathDat}{projectServiceBase.ProjectID}_{projectServiceBase.ServiceID}_A_PS.dat";


            lock (lockObject)
                if (ProjectServices.TryGetValue(key, out ProjectService? projectService))
                    return projectService;

            if (!httpClientException)
                try
                {
                    //this.httpClient.DefaultRequestHeaders.Clear();
                    this.httpClient.DefaultRequestHeaders.Add("AccessKey", accessKey);

                    HttpResponseMessage response = httpClient.GetAsync($"api/ProjectService").Result;

                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        ProjectService? projectService;
                        projectService = response.Content.ReadFromJsonAsync<ProjectService>().Result;

                        if (projectService != null)
                        {
                            lock (lockObject)
                                if (!ProjectServices.TryGetValue(key, out ProjectService? projectService1))
                                {
                                    projectService.Token = Authorize.CreateToken(projectServiceBase.ProjectID, projectServiceBase.ServiceID, TimeSpan.FromDays(365), projectService.Token, this.HttpContext.Connection.RemoteIpAddress?.ToString()).GetToken;
                                    ProjectServices.Add(key, projectService);

                                    Task.Run(delegate
                                    {
                                        Factory.SaveInstance(projectService, path);
                                    });
                                }
                                else
                                    projectService = projectService1;

                            return projectService;
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    httpClientException = true;
                    lock (lockObject)
                        ProjectServices.Add(key, Factory.LoadInstance<ProjectService>(path));
                }
            else
                lock (lockObject)
                    ProjectServices.Add(key, Factory.LoadInstance<ProjectService>(path));

            lock (lockObject)
                if (ProjectServices.TryGetValue(key, out ProjectService? projectService))
                    return projectService;
                else
                    return null;
        }
    }
}