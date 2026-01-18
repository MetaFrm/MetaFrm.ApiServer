using MetaFrm.Api;
using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;

namespace MetaFrm.ApiServer.Controllers.V1
{
    /// <summary>
    /// AssemblyAttributeController
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="_"></param>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AssemblyAttributeController(ILogger<AssemblyAttributeController> logger, Factory _) : ControllerBase, ICore
    {
        private readonly ILogger<AssemblyAttributeController> _logger = logger;
        private readonly Factory factory = _;

        /// <summary>
        /// 키와 값의 컬렉션을 나타냅니다.
        /// </summary>
        private static ConcurrentDictionary<string, AssemblyAttributeShort> AssemblyAttributes { get; set; } = [];

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
                if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("Invalid token. {fullNamespace}", fullNamespace);

                return Ok(null);
            }

            key = string.Format("{0}.{1}.{2}", authorizeToken.ProjectServiceBase.ProjectID, authorizeToken.ProjectServiceBase.ServiceID, fullNamespace);
            path = Path.Combine(Factory.FolderPathDat, $"{authorizeToken.ProjectServiceBase.ProjectID}_{authorizeToken.ProjectServiceBase.ServiceID}_A_{fullNamespace}_A.dat");

            if (AssemblyAttributes.TryGetValue(key, out AssemblyAttributeShort? value))
                return Ok(value);

            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/{Factory.ApiVersion}/AssemblyAttribute?fullNamespace={fullNamespace}")
                {
                    Headers = {
                        { HeaderNames.Accept, MediaTypeNames.Application.Json },
                    }
                };

                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(Headers.Bearer, authorizeToken.UserKey);

                HttpResponseMessage httpResponseMessage = Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    AssemblyAttributeShort? assemblyAttribute;
                    assemblyAttribute = httpResponseMessage.Content.ReadFromJsonAsync<AssemblyAttributeShort>().Result;

                    if (assemblyAttribute != null)
                    {
                        if (!AssemblyAttributes.TryAdd(key, assemblyAttribute) && this._logger.IsEnabled(LogLevel.Warning))
                            this._logger.LogWarning("AssemblyAttributes TryAdd Fail. {Token}, {key}, {fullNamespace}", authorizeToken.Token, key, fullNamespace);

                        Task.Run(delegate
                        {
                            Factory.SaveInstance(assemblyAttribute, path);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (this._logger.IsEnabled(LogLevel.Error))
                    this._logger.LogError(ex, "Exception. {Token}, {key}, {fullNamespace}", authorizeToken.Token, key, fullNamespace);

                if (!AssemblyAttributes.TryAdd(key, Factory.LoadInstance<AssemblyAttribute>(path)) && this._logger.IsEnabled(LogLevel.Warning))
                    this._logger.LogWarning("AssemblyAttributes TryAdd(Factory.LoadInstance) Fail. {Token}, {key}, {path}, {fullNamespace}", authorizeToken.Token, key, path, fullNamespace);
            }

            if (AssemblyAttributes.TryGetValue(key, out AssemblyAttributeShort? value2))
                return Ok(value2);
            else
            {
                if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("AssemblyAttributes TryGetValue(key) Fail. {Token}, {key}, {fullNamespace}", authorizeToken.Token, key, fullNamespace);

                return Ok(null);
            }
        }
    }
}