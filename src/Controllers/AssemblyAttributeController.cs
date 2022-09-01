using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// AssemblyAttributeController
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AssemblyAttributeController : ControllerBase, ICore
    {
        private readonly ILogger<AssemblyAttributeController> _logger;

        /// <summary>
        /// 키와 값의 컬렉션을 나타냅니다.
        /// </summary>
        private static Dictionary<string, AssemblyAttribute> AssemblyAttributes { get; set; } = new Dictionary<string, AssemblyAttribute>();

        private readonly HttpClient httpClient;
        private static bool httpClientException;

        /// <summary>
        /// AssemblyAttributeController
        /// </summary>
        /// <param name="logger"></param>
        public AssemblyAttributeController(ILogger<AssemblyAttributeController> logger)
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
        [HttpGet(Name = "GetAssemblyAttribute")]
        [Authorize]
        public AssemblyAttribute? Get([FromHeader] string token, string fullNamespace)
        {
            string key;
            string path;
            AuthorizeToken authorizeToken;

            authorizeToken = Authorize.AuthorizeTokenList[token];

            key = string.Format("{0}.{1}.{2}", authorizeToken.ProjectServiceBase.ProjectID, authorizeToken.ProjectServiceBase.ServiceID, fullNamespace);
            path = $"{Factory.FolderPathDat}{authorizeToken.ProjectServiceBase.ProjectID}_{authorizeToken.ProjectServiceBase.ServiceID}_A_{fullNamespace}_A.dat";

            if (AssemblyAttributes.ContainsKey(key))
                return AssemblyAttributes[key];

            if (!httpClientException)
                try
                {
                    this.httpClient.DefaultRequestHeaders.Add("token", authorizeToken.UserKey);
                    HttpResponseMessage response = httpClient.GetAsync($"api/AssemblyAttribute?fullNamespace={fullNamespace}").Result;

                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        AssemblyAttribute? assemblyAttribute;
                        assemblyAttribute = response.Content.ReadFromJsonAsync<AssemblyAttribute>().Result;

                        if (assemblyAttribute != null && !AssemblyAttributes.ContainsKey(key))
                        {
                            AssemblyAttributes.Add(key, assemblyAttribute);
                            Factory.SaveInstance(assemblyAttribute, path);
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    httpClientException = true;
                    AssemblyAttributes.Add(key, Factory.LoadInstance<AssemblyAttribute>(path));
                }
            else
                AssemblyAttributes.Add(key, Factory.LoadInstance<AssemblyAttribute>(path));

            if (AssemblyAttributes.ContainsKey(key))
                return AssemblyAttributes[key];
            else
                return null;
        }
    }
}