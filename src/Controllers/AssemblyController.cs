using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// AssemblyController
    /// </summary>
    /// <remarks>
    /// AssemblyController
    /// </remarks>
    /// <param name="logger"></param>
    /// <param name="factory"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class AssemblyController(ILogger<AssemblyController> logger, Factory factory) : ControllerBase, ICore
    {
        static readonly object lockObject = new();
        private readonly ILogger<AssemblyController> _logger = logger;
        private readonly Factory _factory = factory;

        /// <summary>
        /// 키와 값의 컬렉션을 나타냅니다.
        /// </summary>
        private static Dictionary<string, string> AssemblyText { get; set; } = [];

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

            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/Assembly?fullNamespace={fullNamespace}")
                {
                    Headers = {
                        { HeaderNames.Accept, "text/plain" },
                        { "token", authorizeToken.UserKey },
                    }
                };

                HttpResponseMessage httpResponseMessage = Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    string? assembly;
                    assembly = httpResponseMessage.Content.ReadAsStringAsync().Result;

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
                _logger.LogError(ex, "GetAssembly : {Message}", ex.Message);

                lock (lockObject)
                    AssemblyText.Add(key, Factory.LoadString(path));
            }

            lock (lockObject)
                if (AssemblyText.TryGetValue(key, out string? value))
                    return Ok(value);
                else
                    return Ok(null);
        }
    }
}