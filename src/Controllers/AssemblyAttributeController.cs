using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// AssemblyAttributeController
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AssemblyAttributeController : ControllerBase, ICore
    {
        static readonly object lockObject = new();
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
                    StreamWriter? streamWriter = null;

                    try
                    {
                        //Factory.SaveInstance(ex, $"{path}e");

                        streamWriter = System.IO.File.CreateText($"{path}e");
                        streamWriter.Write(ex.ToString());
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        streamWriter?.Close();
                    }

                    httpClientException = true;
                    lock (lockObject)
                        AssemblyAttributes.Add(key, Factory.LoadInstance<AssemblyAttribute>(path));
                }
            else
                lock (lockObject)
                    AssemblyAttributes.Add(key, Factory.LoadInstance<AssemblyAttribute>(path));

            lock (lockObject)
                if (AssemblyAttributes.TryGetValue(key, out AssemblyAttribute? value))
                    return Ok(value);
                else
                    return Ok(null);
        }
    }
}