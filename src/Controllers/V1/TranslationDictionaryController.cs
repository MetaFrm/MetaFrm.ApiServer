using MetaFrm.ApiServer.Auth;
using MetaFrm.Localization;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MetaFrm.ApiServer.Controllers.V1
{
    /// <summary>
    /// TranslationDictionaryController
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="_"></param>
    [Route("api/v1/[controller]")]
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
        [Authorize]
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
                Response? result = new() { DataSet = new() };
                Data.DataTable dataTable;
                Data.DataRow dataRow;

                foreach (var item in TranslationService.TranslationDictionary)
                {
                    dataTable = new();
                    dataTable.DataColumns.Add(new(TranslationColumns.SourceText, "System.String"));
                    dataTable.DataColumns.Add(new(TranslationColumns.TargetCultureName, "System.String"));
                    dataTable.DataColumns.Add(new(TranslationColumns.TargetText, "System.String"));

                    foreach (var row in item.Value)
                    {
                        dataRow = new();

                        dataTable.DataRows.Add(dataRow);

                        dataRow.Values.Add(TranslationColumns.SourceText, new() { StringValue = row.Key });
                        dataRow.Values.Add(TranslationColumns.TargetCultureName, new() { StringValue = item.Key });
                        dataRow.Values.Add(TranslationColumns.TargetText, new() { StringValue = row.Value });
                    }

                    result.DataSet.DataTables.Add(dataTable);
                }

                result.Status = Status.OK;

                if (!TranslationDictionary.TryAdd(key, result) && this._logger.IsEnabled(LogLevel.Warning))
                    this._logger.LogWarning("TranslationDictionary TryAdd Fail : {key}", key);

                Task.Run(delegate
                {
                    Factory.SaveInstance(result, path);
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                if (this._logger.IsEnabled(LogLevel.Error))
                    this._logger.LogError(ex, "{key}", key);

                if (!TranslationDictionary.TryAdd(key, Factory.LoadInstance<Response>(path)) && this._logger.IsEnabled(LogLevel.Warning))
                    this._logger.LogWarning("TranslationDictionary TryAdd(Factory.LoadInstance) Fail : {key}, {path}", key, path);
            }

            if (TranslationDictionary.TryGetValue(key, out Response? projectService))
                return Ok(projectService);
            else
            {
                if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("TranslationDictionary TryGetValue(key) Fail. {key}", key);

                return Ok(null);
            }
        }
    }
}