using MetaFrm.ApiServer.Auth;
using MetaFrm.Auth;
using MetaFrm.Data;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MetaFrm.ApiServer.Controllers.V2
{
    /// <summary>
    /// ServiceController
    /// </summary>
    [Route("api/v2/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase, ICore
    {
        private readonly ILogger<ServiceController> _logger;
        private readonly Factory factory;
        private readonly string[] NotAuthorizeCommandText;
        private readonly string[] BrokerProducerCommandText;
        private readonly string[] BrokerProducerCommandTextParallel;
        private static string? BrokerConnectionString;
        private static string? BrokerQueueName;
        private static ICore? BrokerConsumer;
        private static string? BrokerQueueNameParallel;
        private static string? BrokerConnectionStringParallel;
        private static IServiceString? BrokerProducer;
        private static IServiceString? BrokerProducerParallel;

        /// <summary>
        /// ServiceController
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="_"></param>
        public ServiceController(ILogger<ServiceController> logger, Factory _)
        {
            this._logger = logger;
            this.factory = _;

            this.NotAuthorizeCommandText = (this.GetAttribute(nameof(NotAuthorizeCommandText)) ?? "").Split(',');
            this.BrokerProducerCommandText = (this.GetAttribute(nameof(BrokerProducerCommandText)) ?? "").Split(',');
            this.BrokerProducerCommandTextParallel = (this.GetAttribute(nameof(BrokerProducerCommandTextParallel)) ?? "").Split(',');

            BrokerConnectionString ??= this.GetAttribute("BrokerConnectionString");
            BrokerQueueName ??= this.GetAttribute("BrokerQueueName");
            BrokerConsumer ??= this.CreateInstance("BrokerConsumer", true, true, [BrokerConnectionString, BrokerQueueName]);

            BrokerQueueNameParallel ??= this.GetAttribute("BrokerQueueNameParallel");

            if (BrokerProducerParallel == null && !Factory.IsRegisterInstance(nameof(BrokerProducerCommandTextParallel)) && !string.IsNullOrEmpty(BrokerQueueNameParallel))
            {
                BrokerConnectionStringParallel ??= this.GetAttribute("BrokerConnectionStringParallel");

                BrokerProducerParallel ??= (IServiceString?)this.CreateInstance("BrokerProducer", false, true, [BrokerConnectionStringParallel, BrokerQueueNameParallel]);

                if (BrokerProducerParallel != null)
                    Factory.RegisterInstance(BrokerProducerParallel, nameof(BrokerProducerCommandTextParallel));
            }
            else
                BrokerProducerParallel ??= ((IServiceString?)Factory.LoadInstance(nameof(BrokerProducerCommandTextParallel)));
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="serviceData"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public IActionResult Get(ServiceDataShort serviceData)
        {
            Response response;
            AuthorizeToken? authorizeToken = null;
            ServiceData serviceData1 = new()
            {
                ServiceName = serviceData.S,
                TransactionScope = serviceData.Ts,
                Token = serviceData.T,
            };

            foreach (var c in serviceData.C)
            {
                Dictionary<string, Parameter> parameters = [];

                foreach (var p in c.Value.P)
                {
                    parameters.Add(p.Key, new()
                    {
                        DbType = p.Value.T,
                        Size = p.Value.S,
                        TargetCommandName = p.Value.C,
                        TargetParameterName = p.Value.P,
                    });
                }

                Dictionary<int, Dictionary<string, DataValue>> values = [];

                foreach (var v in c.Value.V)
                {
                    Dictionary<string, DataValue> dv = [];

                    foreach (var item in v.Value)
                    {
                        DataTable? dataTableValue = null;

                        if (item.Value.T != null)
                        {
                            List<DataColumn> dataColumns = [];

                            foreach (var col in item.Value.T.C)
                            {
                                dataColumns.Add(new()
                                {
                                    FieldName = col.F,
                                    Caption = col.C,
                                    DataTypeFullNamespace = col.N,
                                });
                            }

                            List<DataRow> dataRows = [];
                            
                            foreach (var row in item.Value.T.R)
                            {
                                Dictionary<string, DataValue> dataValues = [];

                                foreach (var vs in row.V)
                                {
                                    dataValues.Add(vs.Key, new()
                                    {
                                        ValueType = item.Value.Vt,
                                        CharValue = item.Value.C,
                                        CharsValue = item.Value.Cs,
                                        ByteValue = item.Value.B,
                                        BytesValue = item.Value.Bs,
                                        DateTimeValue = item.Value.Dt,
                                        DecimalValue = item.Value.D,
                                        DoubleValue = item.Value.Do,

                                        FloatValue = item.Value.F,
                                        IntValue = item.Value.I,
                                        LongValue = item.Value.L,
                                        SbyteValue = item.Value.Sb,
                                        SbytesValue = item.Value.Sbs,
                                        ShortValue = item.Value.St,

                                        StringValue = item.Value.S,
                                        UintValue = item.Value.Ui,
                                        UlongValue = item.Value.Ul,
                                        UshortValue = item.Value.Us,
                                        BooleanValue = item.Value.Bl,

                                        GuidValue = item.Value.G,
                                        TimeSpanValue = item.Value.Ts,
                                        DateTimeOffsetValue = item.Value.O,
                                        JsonValue = item.Value.J,
                                        VectorValue = item.Value.V,
                                    });
                                }

                                dataRows.Add(new()
                                {
                                    Values = dataValues,
                                });
                            }

                            dataTableValue = new DataTable()
                            {
                                DataTableName = item.Value.T.N,
                                DataColumns = dataColumns,
                                DataRows = dataRows,
                            }
                            ;
                        }

                        dv.Add(item.Key, new()
                        {
                            ValueType = item.Value.Vt,
                            CharValue = item.Value.C,
                            CharsValue = item.Value.Cs,
                            ByteValue = item.Value.B,
                            BytesValue = item.Value.Bs,
                            DateTimeValue = item.Value.Dt,
                            DecimalValue = item.Value.D,
                            DoubleValue = item.Value.Do,

                            FloatValue = item.Value.F,
                            IntValue = item.Value.I,
                            LongValue = item.Value.L,
                            SbyteValue = item.Value.Sb,
                            SbytesValue = item.Value.Sbs,
                            ShortValue = item.Value.St,

                            StringValue = item.Value.S,
                            UintValue = item.Value.Ui,
                            UlongValue = item.Value.Ul,
                            UshortValue = item.Value.Us,
                            BooleanValue = item.Value.Bl,

                            GuidValue = item.Value.G,
                            DataTableValue = dataTableValue,
                            TimeSpanValue = item.Value.Ts,
                            DateTimeOffsetValue = item.Value.O,
                            JsonValue = item.Value.J,
                            VectorValue = item.Value.V,
                        }
                        );
                    }

                    values.Add(v.Key, dv);
                }

                serviceData1.Commands.Add(c.Key, new()
                {
                    ConnectionName = c.Value.N,
                    CommandText = c.Value.C,
                    CommandType = c.Value.T,
                    Parameters = parameters,
                    Values = values,
                });
            }

            try
            {
                authorizeToken = Request.GetAuthorizeToken();

                if (authorizeToken == null)
                {
                    if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("Invalid token. {serviceData}", System.Text.Json.JsonSerializer.Serialize(serviceData1));

                    return this.BadRequest("Invalid token.");
                }

                if (serviceData1.Commands.Count < 1)
                {
                    if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("No command. {Token}, {serviceData}", authorizeToken.Token, System.Text.Json.JsonSerializer.Serialize(serviceData1));

                    return this.BadRequest("No command.");
                }

                if (authorizeToken.TokenType == AuthType.ProjectService)
                    foreach (var command in serviceData1.Commands)
                        if (!this.NotAuthorizeCommandText.Contains(command.Value.CommandText))
                        {
                            if (this._logger.IsEnabled(LogLevel.Error)) this._logger.LogError("No CommandText. {Token}, {serviceData}", authorizeToken.Token, System.Text.Json.JsonSerializer.Serialize(serviceData1));

                            return this.BadRequest("No CommandText.");
                        }

                if (!string.IsNullOrEmpty(serviceData1.ServiceName))
                    response = ((IService)Factory.CreateInstance(serviceData1.ServiceName)).Request(serviceData1);
                else
                {
                    response = new()
                    {
                        Status = Status.OK
                    };
                }

                bool isBrokerProducerCommandText = false;
                bool isBrokerProducerCommandTextParallel = false;
                foreach (var command in serviceData1.Commands)
                {
                    if (!isBrokerProducerCommandText && this.BrokerProducerCommandText.Contains(command.Value.CommandText))
                    {
                        isBrokerProducerCommandText = true;

                        Task.Run(() =>
                        {
                            BrokerProducer ??= (IServiceString?)this.CreateInstance("BrokerProducer", true, true, [BrokerConnectionString!, BrokerQueueName!]);
                            BrokerProducer?.Request(System.Text.Json.JsonSerializer.Serialize(new BrokerData { ServiceData = serviceData1, Response = response }));
                        });
                    }
                    if (!isBrokerProducerCommandTextParallel && this.BrokerProducerCommandTextParallel.Contains(command.Value.CommandText))
                    {
                        isBrokerProducerCommandTextParallel = true;

                        Task.Run(() =>
                        {
                            BrokerProducerParallel?.Request(System.Text.Json.JsonSerializer.Serialize(new BrokerData { ServiceData = serviceData1, Response = response }));
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (this._logger.IsEnabled(LogLevel.Error))
                    this._logger.LogError(ex, "{Token}, {serviceData}", authorizeToken?.Token, System.Text.Json.JsonSerializer.Serialize(serviceData1));

                response = new()
                {
                    Status = Status.Failed,
                    Message = ex.Message,
                };
            }

            DataSetShort? dataSetShort = null;

            if (response.DataSet != null)
            {
                List<DataTableShort> dataTableShorts = [];

                foreach (var table in response.DataSet.DataTables)
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
                S = response.Status,
                M = response.Message,
                D = dataSetShort,
            };

            return Ok(responseShort);
        }
    }
}