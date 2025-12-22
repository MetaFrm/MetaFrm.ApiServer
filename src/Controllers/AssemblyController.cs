using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Collections.Concurrent;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// AssemblyController
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="_"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class AssemblyController(ILogger<AssemblyController> logger, Factory _) : ControllerBase, ICore
    {
        private readonly ILogger<AssemblyController> _logger = logger;
        private readonly Factory factory = _;

        /// <summary>
        /// 키와 값의 컬렉션을 나타냅니다.
        /// </summary>
        private static ConcurrentDictionary<string, string> AssemblyText { get; set; } = [];

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
            path = Path.Combine(Factory.FolderPathDat, $"{authorizeToken.ProjectServiceBase.ProjectID}_{authorizeToken.ProjectServiceBase.ServiceID}_A_{fullNamespace}.dat");

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

                    if (!assembly.IsNullOrEmpty())
                    {
                        if (!AssemblyText.TryAdd(key, assembly) && this._logger.IsEnabled(LogLevel.Warning))
                            this._logger.LogWarning("GetAssembly AssemblyText TryAdd Fail : {key}", key);

                        Task.Run(delegate
                        {
                            Factory.SaveString(assembly, path);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (this._logger.IsEnabled(LogLevel.Error))
                    this._logger.LogError(ex, "GetAssembly : {Message}", ex.Message);

                if (!AssemblyText.TryAdd(key, Factory.LoadString(path)) && this._logger.IsEnabled(LogLevel.Warning))
                    this._logger.LogWarning("GetAssembly AssemblyText TryAdd Factory.LoadString Fail : {key}, {path}", key, path);
            }

            if (AssemblyText.TryGetValue(key, out string? value2))
                return Ok(value2);
            else
                return Ok(null);
        }
    }
}