using MetaFrm.Api.Models;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

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

        private readonly HttpClient httpClient;
        private static bool httpClientException;

        /// <summary>
        /// TranslationDictionaryController
        /// </summary>
        /// <param name="logger"></param>
        public TranslationDictionaryController(ILogger<TranslationDictionaryController> logger)
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
        /// <returns></returns>
        [HttpGet(Name = "GetTranslationDictionary")]
        public IActionResult? Get()
        {
            string key;
            string path;

            key = $"{Factory.ProjectID}.{Factory.ServiceID}";
            path = $"{Factory.FolderPathDat}{Factory.ProjectID}_{Factory.ServiceID}_TD.dat";

            lock (lockObject)
                if (TranslationDictionary.TryGetValue(key, out Response? response))
                    return Ok(response);

            if (!httpClientException)
                try
                {
                    //this.httpClient.DefaultRequestHeaders.Clear();
                    this.httpClient.DefaultRequestHeaders.Add("AccessKey", Factory.AccessKey);

                    HttpResponseMessage response = httpClient.GetAsync($"api/TranslationDictionary").Result;

                    response.EnsureSuccessStatusCode();

                    if (response.IsSuccessStatusCode)
                    {
                        Response? result;
                        result = response.Content.ReadFromJsonAsync<Response>().Result;

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
                    httpClientException = true;
                    lock (lockObject)
                        TranslationDictionary.Add(key, Factory.LoadInstance<Response>(path));
                }
            else
                lock (lockObject)
                    TranslationDictionary.Add(key, Factory.LoadInstance<Response>(path));

            lock (lockObject)
                if (TranslationDictionary.TryGetValue(key, out Response? projectService))
                    return Ok(projectService);
                else
                    return Ok(null);
        }
    }
}
