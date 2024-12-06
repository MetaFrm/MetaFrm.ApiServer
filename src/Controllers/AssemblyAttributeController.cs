using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
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
        static readonly object lockObject = new();
        private readonly ILogger<AssemblyAttributeController> _logger = logger;
        private readonly Factory _factory = factory;

        /// <summary>
        /// 키와 값의 컬렉션을 나타냅니다.
        /// </summary>
        private static Dictionary<string, AssemblyAttribute> AssemblyAttributes { get; set; } = [];

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

            lock (lockObject)
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

                    lock (lockObject)
                        if (assemblyAttribute != null && !AssemblyAttributes.TryGetValue(key, out AssemblyAttribute? value))
                        {
                            AssemblyAttributes.Add(key, assemblyAttribute);

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

                lock (lockObject)
                    AssemblyAttributes.Add(key, Factory.LoadInstance<AssemblyAttribute>(path));
            }

            lock (lockObject)
                if (AssemblyAttributes.TryGetValue(key, out AssemblyAttribute? value))
                    return Ok(value);
                else
                    return Ok(null);
        }
    }
}