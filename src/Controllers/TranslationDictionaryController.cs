using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Net.Mime;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// TranslationDictionaryController
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="_"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationDictionaryController(ILogger<TranslationDictionaryController> logger, Factory _) : ControllerBase, ICore
    {
        private readonly ILogger<TranslationDictionaryController> _logger = logger;
        private readonly Factory factory = _;

        /// <summary>
        /// 키와 값의 컬렉션을 나타냅니다.
        /// </summary>
        private static ConcurrentDictionary<string, Response> TranslationDictionary { get; set; } = [];

        /// <summary>
        /// Get
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult? Get()
        {
            string key;
            string path;

            key = $"{Factory.ProjectServiceBase?.ProjectID}.{Factory.ProjectServiceBase?.ServiceID}";
            path = Path.Combine(Factory.FolderPathDat, $"{Factory.ProjectServiceBase?.ProjectID}_{Factory.ProjectServiceBase?.ServiceID}_TD.dat");

            if (TranslationDictionary.TryGetValue(key, out Response? response))
                return Ok(response);

            try
            {
                HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, $"{Factory.BaseAddress}api/TranslationDictionary")
                {
                    Headers = {
                        { HeaderNames.Accept, MediaTypeNames.Application.Json },
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
                        if (!TranslationDictionary.TryAdd(key, result) && this._logger.IsEnabled(LogLevel.Warning))
                            this._logger.LogWarning("GetTranslationDictionary TranslationDictionary TryAdd Fail : {key}", key);

                        Task.Run(delegate
                        {
                            Factory.SaveInstance(result, path);
                        });

                        return Ok(result);
                    }
                }
            }
            catch (Exception ex)
            {
                if (this._logger.IsEnabled(LogLevel.Error))
                    this._logger.LogError(ex, "GetTranslationDictionary : {Message}", ex.Message);

                if (!TranslationDictionary.TryAdd(key, Factory.LoadInstance<Response>(path)) && this._logger.IsEnabled(LogLevel.Warning))
                    this._logger.LogWarning("GetTranslationDictionary TranslationDictionary TryAdd Factory.LoadInstance Fail : {key}, {path}", key, path);
            }

            if (TranslationDictionary.TryGetValue(key, out Response? projectService))
                return Ok(projectService);
            else
                return Ok(null);
        }
    }
}