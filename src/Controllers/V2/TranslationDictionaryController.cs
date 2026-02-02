using MetaFrm.ApiServer.Auth;
using MetaFrm.Data;
using MetaFrm.Localization;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MetaFrm.ApiServer.Controllers.V2
{
    /// <summary>
    /// TranslationDictionaryController
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="_"></param>
    [Route("api/v2/[controller]")]
    [ApiController]
    public class TranslationDictionaryController(ILogger<TranslationDictionaryController> logger, Factory _) : ControllerBase, ICore
    {
        private readonly ILogger<TranslationDictionaryController> _logger = logger;
        private readonly Factory factory = _;

        /// <summary>
        /// 키와 값의 컬렉션을 나타냅니다.
        /// </summary>
        private static ConcurrentDictionary<string, ResponseShort> TranslationDictionary { get; set; } = [];

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

            if (TranslationDictionary.TryGetValue(key, out ResponseShort? response))
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






                DataSetShort? dataSetShort = null;

                if (result.DataSet != null)
                {
                    List<DataTableShort> dataTableShorts = [];

                    foreach (var table in result.DataSet.DataTables)
                    {
                        List<DataColumnShort> dataColumnShorts = [];

                        foreach (var col in table.DataColumns)
                        {
                            dataColumnShorts.Add(new()
                            {
                                F = col.FieldName,
                                C = col.Caption,
                                N = col.DataTypeFullNamespace,
                            });
                        }

                        List<DataRowShort> dataRowShorts = [];

                        foreach (var row in table.DataRows)
                        {
                            Dictionary<string, DataValueShort> dv = [];

                            foreach (var dataValue in row.Values)
                            {
                                dv.Add(dataValue.Key, new()
                                {
                                    Vt = dataValue.Value.ValueType,
                                    C = dataValue.Value.CharValue,
                                    Cs = dataValue.Value.CharsValue,
                                    B = dataValue.Value.ByteValue,
                                    Bs = dataValue.Value.BytesValue,
                                    Dt = dataValue.Value.DateTimeValue,
                                    D = dataValue.Value.DecimalValue,
                                    Do = dataValue.Value.DoubleValue,

                                    F = dataValue.Value.FloatValue,
                                    I = dataValue.Value.IntValue,
                                    L = dataValue.Value.LongValue,
                                    Sb = dataValue.Value.SbyteValue,
                                    Sbs = dataValue.Value.SbytesValue,
                                    St = dataValue.Value.ShortValue,

                                    S = dataValue.Value.StringValue,
                                    Ui = dataValue.Value.UintValue,
                                    Ul = dataValue.Value.UlongValue,
                                    Us = dataValue.Value.UshortValue,
                                    Bl = dataValue.Value.BooleanValue,

                                    G = dataValue.Value.GuidValue,
                                    Ts = dataValue.Value.TimeSpanValue,
                                    O = dataValue.Value.DateTimeOffsetValue,
                                    J = dataValue.Value.JsonValue,
                                    V = dataValue.Value.VectorValue,
                                });
                            }

                            dataRowShorts.Add(new()
                            {
                                V = dv,
                            });
                        }

                        dataTableShorts.Add(new()
                        {
                            N = table.DataTableName,
                            C = dataColumnShorts,
                            R = dataRowShorts,
                        });
                    }

                    dataSetShort = new()
                    {
                        T = dataTableShorts,
                    };
                }

                ResponseShort responseShort = new()
                {
                    S = result.Status,
                    M = result.Message,
                    D = dataSetShort,
                };







                if (!TranslationDictionary.TryAdd(key, responseShort) && this._logger.IsEnabled(LogLevel.Warning))
                    this._logger.LogWarning("TranslationDictionary TryAdd Fail : {key}", key);

                Task.Run(delegate
                {
                    Factory.SaveInstance(responseShort, path);
                });

                return Ok(responseShort);
            }
            catch (Exception ex)
            {
                if (this._logger.IsEnabled(LogLevel.Error))
                    this._logger.LogError(ex, "{key}", key);

                if (!TranslationDictionary.TryAdd(key, Factory.LoadInstance<ResponseShort>(path)) && this._logger.IsEnabled(LogLevel.Warning))
                    this._logger.LogWarning("TranslationDictionary TryAdd(Factory.LoadInstance) Fail : {key}, {path}", key, path);
            }

            if (TranslationDictionary.TryGetValue(key, out ResponseShort? projectService))
                return Ok(projectService);
            else
            {
                if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("TranslationDictionary TryGetValue(key) Fail. {key}", key);

                return Ok(null);
            }
        }
    }
}