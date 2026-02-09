using MetaFrm.Api;
using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace MetaFrm.ApiServer.Controllers.V1
{
    /// <summary>
    /// AssemblyController
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="_"></param>
    [Route("api/v1/[controller]")]
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
        /// <param name="fullNamespace"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public IActionResult? Get(string fullNamespace)
        {
            string key;
            string path;
            AuthorizeToken? authorizeToken;

            authorizeToken = Request.GetAuthorizeToken();

            if (authorizeToken == null || authorizeToken.Token == null)
            {
                this._logger.Error("Invalid token. {0}", fullNamespace);

                return Ok(null);
            }

            key = string.Format("{0}.{1}.{2}", authorizeToken.ProjectServiceBase.ProjectID, authorizeToken.ProjectServiceBase.ServiceID, fullNamespace);
            path = Path.Combine(Factory.FolderPathDat, $"{authorizeToken.ProjectServiceBase.ProjectID}_{authorizeToken.ProjectServiceBase.ServiceID}_A_{fullNamespace}.dat");

            if (AssemblyText.TryGetValue(key, out string? value))
                return Ok(value);

            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/{Factory.ApiVersion}/Assembly?fullNamespace={fullNamespace}")
                {
                    Headers = { { HeaderNames.Accept, MediaTypeNames.Text.Plain } }
                };

                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(Headers.Bearer, authorizeToken.UserKey);

                HttpResponseMessage httpResponseMessage = Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    string? assembly;
                    assembly = httpResponseMessage.Content.ReadAsStringAsync().Result;

                    if (!string.IsNullOrEmpty(assembly))
                    {
                        if (!AssemblyText.TryAdd(key, assembly))
                            this._logger.Warning("AssemblyText TryAdd Fail : {0}, {1}, {2}", authorizeToken.Token, key, fullNamespace);

                        Task.Run(delegate
                        {
                            Factory.SaveString(assembly, path);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "Exception. {0}, {1}, {2}", authorizeToken.Token, key, fullNamespace);

                if (!AssemblyText.TryAdd(key, Factory.LoadString(path)))
                    this._logger.Warning("AssemblyText TryAdd(Factory.LoadString) Fail : {0}, {1}, {2}", authorizeToken.Token, key, path);
            }

            if (AssemblyText.TryGetValue(key, out string? value2))
                return Ok(value2);
            else
            {
                this._logger.Error("AssemblyText TryGetValue(key) Fail. {0}, {1}, {2}", authorizeToken.Token, key, fullNamespace);

                return Ok(null);
            }
        }
    }
}