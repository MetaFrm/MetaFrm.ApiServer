using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// AssemblyAttributeController
    /// </summary>
    /// <remarks>
    /// AssemblyAttributeController
    /// </remarks>
    /// <param name="logger"></param>
    /// <param name="factory"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class AssemblyAttributeController(ILogger<AssemblyAttributeController> logger, Factory factory) : ControllerBase, ICore
    {
        private readonly ILogger<AssemblyAttributeController> _logger = logger;
        private readonly Factory _factory = factory;

        /// <summary>
        /// 키와 값의 컬렉션을 나타냅니다.
        /// </summary>
        private static ConcurrentDictionary<string, AssemblyAttribute> AssemblyAttributes { get; set; } = [];

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="token"></param>
        /// <param name="fullNamespace"></param>
        /// <returns></returns>
        [HttpGet(Name = "GetAssemblyAttribute")]
        [Authorize]
        public IActionResult? Get([FromHeader] string token, string fullNamespace)
        {
            string key;
            string path;

            if (!Authorize.AuthorizeTokenList.TryGetValue(token, out AuthorizeToken? authorizeToken) || authorizeToken == null)
                return Ok(null);

            key = string.Format("{0}.{1}.{2}", authorizeToken.ProjectServiceBase.ProjectID, authorizeToken.ProjectServiceBase.ServiceID, fullNamespace);
            path = $"{Factory.FolderPathDat}{authorizeToken.ProjectServiceBase.ProjectID}_{authorizeToken.ProjectServiceBase.ServiceID}_A_{fullNamespace}_A.dat";

            if (AssemblyAttributes.TryGetValue(key, out AssemblyAttribute? value))
                return Ok(value);

            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/AssemblyAttribute?fullNamespace={fullNamespace}")
                {
                    Headers = {
                        { HeaderNames.Accept, "application/json" },
                        { "token", authorizeToken.UserKey },
                    }
                };

                HttpResponseMessage httpResponseMessage = Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    AssemblyAttribute? assemblyAttribute;
                    assemblyAttribute = httpResponseMessage.Content.ReadFromJsonAsync<AssemblyAttribute>().Result;

                    if (assemblyAttribute != null)
                    {
                        if (!AssemblyAttributes.TryAdd(key, assemblyAttribute))
                            _logger.LogError("GetAssemblyAttribute AssemblyAttributes TryAdd Fail : {key}", key);

                        Task.Run(delegate
                        {
                            Factory.SaveInstance(assemblyAttribute, path);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAssemblyAttribute : {Message}", ex.Message);

                if (!AssemblyAttributes.TryAdd(key, Factory.LoadInstance<AssemblyAttribute>(path)))
                    _logger.LogError("GetAssemblyAttribute AssemblyAttributes TryAdd Factory.LoadInstance Fail : {key}, {path}", key, path);
            }

            if (AssemblyAttributes.TryGetValue(key, out AssemblyAttribute? value2))
                return Ok(value2);
            else
                return Ok(null);
        }
    }
}