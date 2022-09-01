using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
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
        public string? Get([FromHeader] string token, string fullNamespace)
        {
            string key;
            string path;
            AuthorizeToken authorizeToken;

            authorizeToken = Authorize.AuthorizeTokenList[token];

            key = string.Format("{0}.{1}.{2}", authorizeToken.ProjectServiceBase.ProjectID, authorizeToken.ProjectServiceBase.ServiceID, fullNamespace);
            path = $"{Factory.FolderPathDat}{authorizeToken.ProjectServiceBase.ProjectID}_{authorizeToken.ProjectServiceBase.ServiceID}_A_{fullNamespace}.dat";


            if (AssemblyText.ContainsKey(key))
                return AssemblyText[key];

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

                        if (!assembly.IsNullOrEmpty() && !AssemblyText.ContainsKey(key))
                        {
                            AssemblyText.Add(key, assembly);
                            Factory.SaveString(assembly, path);
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    httpClientException = true;
                    AssemblyText.Add(key, Factory.LoadString(path));
                }

            if (AssemblyText.ContainsKey(key))
                return AssemblyText[key];
            else
                return null;
        }
    }
}