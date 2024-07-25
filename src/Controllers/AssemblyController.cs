using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// AssemblyController
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AssemblyController : ControllerBase, ICore
    {
        static readonly object lockObject = new();
        private readonly ILogger<AssemblyController> _logger;

        /// <summary>
        /// 키와 값의 컬렉션을 나타냅니다.
        /// </summary>
        private static Dictionary<string, string> AssemblyText { get; set; } = new Dictionary<string, string>();

        private readonly HttpClient httpClient;
        private static bool httpClientException;

        /// <summary>
        /// AssemblyController
        /// </summary>
        /// <param name="logger"></param>
        public AssemblyController(ILogger<AssemblyController> logger)
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
        /// <param name="token"></param>
        /// <param name="fullNamespace"></param>
        /// <returns></returns>
        [HttpGet(Name = "GetAssembly")]
        [Authorize]
        public IActionResult? Get([FromHeader] string token, string fullNamespace)
        {
            string key;
            string path;

            if (!Authorize.AuthorizeTokenList.TryGetValue(token, out AuthorizeToken? authorizeToken) || authorizeToken == null)
                return Ok(null);

            key = string.Format("{0}.{1}.{2}", authorizeToken.ProjectServiceBase.ProjectID, authorizeToken.ProjectServiceBase.ServiceID, fullNamespace);
            path = $"{Factory.FolderPathDat}{authorizeToken.ProjectServiceBase.ProjectID}_{authorizeToken.ProjectServiceBase.ServiceID}_A_{fullNamespace}.dat";

            lock (lockObject)
                if (AssemblyText.TryGetValue(key, out string? value))
                    return Ok(value);

            if (!httpClientException)
                try
                {
                    this.httpClient.DefaultRequestHeaders.Add("token", authorizeToken.UserKey);
                    HttpResponseMessage response = httpClient.GetAsync($"api/Assembly?fullNamespace={fullNamespace}").Result;

                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        string? assembly;
                        assembly = response.Content.ReadAsStringAsync().Result;

                        lock (lockObject)
                            if (!assembly.IsNullOrEmpty() && !AssemblyText.TryGetValue(key, out string? value))
                            {
                                AssemblyText.Add(key, assembly);

                                Task.Run(delegate
                                {
                                    Factory.SaveString(assembly, path);
                                });
                            }
                    }
                }
                catch (Exception ex)
                {
                    Factory.SaveInstance(ex, $"{path}e");

                    httpClientException = true;
                    lock (lockObject)
                        AssemblyText.Add(key, Factory.LoadString(path));
                }
            else
                lock (lockObject)
                    AssemblyText.Add(key, Factory.LoadString(path));

            lock (lockObject)
                if (AssemblyText.TryGetValue(key, out string? value))
                    return Ok(value);
                else
                    return Ok(null);
        }
    }
}