using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Json;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// TranslationDictionaryController
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationDictionaryController : ControllerBase, ICore
    {
        static readonly object lockObject = new();
        private readonly ILogger<TranslationDictionaryController> _logger;

        /// <summary>
        /// 키와 값의 컬렉션을 나타냅니다.
        /// </summary>
        private static Dictionary<string, Response> TranslationDictionary { get; set; } = new Dictionary<string, Response>();

        /// <summary>
        /// TranslationDictionaryController
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="factory"></param>
        public TranslationDictionaryController(ILogger<TranslationDictionaryController> logger, Factory factory)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "GetTranslationDictionary")]
        public IActionResult? Get()
        {
            string key;
            string path;

            key = $"{Factory.ProjectServiceBase.ProjectID}.{Factory.ProjectServiceBase.ServiceID}";
            path = $"{Factory.FolderPathDat}{Factory.ProjectServiceBase.ProjectID}_{Factory.ProjectServiceBase.ServiceID}_TD.dat";

            lock (lockObject)
                if (TranslationDictionary.TryGetValue(key, out Response? response))
                    return Ok(response);

            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/TranslationDictionary")
                {
                    Headers = {
                        { HeaderNames.Accept, "application/json" },
                        { "AccessKey", Factory.AccessKey },
                    }
                };

                HttpResponseMessage httpResponseMessage = Factory.HttpClientFactory.CreateClient().SendAsync(httpRequestMessage).Result;

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    Response? result;
                    result = httpResponseMessage.Content.ReadFromJsonAsync<Response>().Result;

                    if (result != null)
                    {
                        lock (lockObject)
                            if (!TranslationDictionary.TryGetValue(key, out Response? result1))
                            {
                                TranslationDictionary.Add(key, result);

                                Task.Run(delegate
                                {
                                    Factory.SaveInstance(result, path);
                                });
                            }
                            else
                                result = result1;

                        return Ok(result);
                    }
                }
            }
            catch (HttpRequestException)
            {
                lock (lockObject)
                    TranslationDictionary.Add(key, Factory.LoadInstance<Response>(path));
            }

            lock (lockObject)
                if (TranslationDictionary.TryGetValue(key, out Response? projectService))
                    return Ok(projectService);
                else
                    return Ok(null);
        }
    }
}