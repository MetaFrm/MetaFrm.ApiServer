using MetaFrm.Database;
using MetaFrm.Service;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MetaFrm.ApiServer.RabbitMQ
{
    internal class RabbitMQConsumer : ICore, IDisposable
    {
        private static RabbitMQConsumer? _consumer;
        private IConnection? _connection;
        private IModel? _model;
        internal string? ConnectionString { get; set; }
        internal string? QueueName { get; set; }

        private readonly string Login;
        private readonly string AccessCode;

        private RabbitMQConsumer()
        {
            _consumer = this;

            this.Login = this.GetAttribute(nameof(this.Login));
            this.AccessCode = this.GetAttribute(nameof(this.AccessCode));
        }

        internal void Init()
        {
            this.Close();

            if (this.ConnectionString.IsNullOrEmpty())
                return;

            this._connection = new ConnectionFactory
            {
                Uri = new(this.ConnectionString)
            }.CreateConnection();

            this._model = _connection.CreateModel();
            this._model.QueueDeclare(queue: this.QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(this._model);
            consumer.Received += Consumer_Received;

            this._model.BasicConsume(queue: this.QueueName, autoAck: true, consumer: consumer);
        }

        private void Consumer_Received(object? sender, BasicDeliverEventArgs e)
        {
            var body = e.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            RabbitMQData? myObject = JsonSerializer.Deserialize<RabbitMQData?>(message);


            if (myObject == null)
                return;

            if (myObject.ServiceData == null || myObject.Response == null) return;

            foreach (var key in myObject.ServiceData.Commands.Keys)
            {
                for (int i = 0; i < myObject.ServiceData.Commands[key].Values.Count; i++)
                {
                    if (myObject.ServiceData.Commands[key].CommandText == this.Login)//Login
                    {
                        string? email = myObject.ServiceData.Commands[key].Values[i]["EMAIL"].StringValue;

                        this.PushNotification(nameof(this.Login)
                            , email
                            , $"Login {(myObject.Response.Status == Status.OK ? "OK" : "Fail")}"
                            , $"{(myObject.Response.Status == Status.OK ? email : myObject.Response.Message)}"
                            , myObject.DateTime
                            , myObject.Response.Status
                            , null);
                    }
                    else if (myObject.ServiceData.Commands[key].CommandText == this.AccessCode)//AccessCode
                    {
                        this.SandEmail(nameof(this.AccessCode)
                            , myObject.ServiceData.Commands[key].Values[i]["SUBJECT"].StringValue
                            , myObject.ServiceData.Commands[key].Values[i]["BODY"].StringValue
                            , myObject.ServiceData.Commands[key].Values[i]["EMAIL"].StringValue);
                    }
                }
            }
        }

        private void PushNotification(string ACTION, string? EMAIL, string Title, string? Body, DateTime dateTime, Status status, Dictionary<string, string>? data)
        {
            IService service;
            Data.DataTable? dataTable;

            dataTable = this.GetFirebaseFCM_Token(ACTION, EMAIL);

            if (dataTable == null)
                return;

            ServiceData serviceData = new()
            {
                ServiceName = "MetaFrm.Service.FirebaseAdminService",
                TransactionScope = false,
            };

            serviceData["1"].CommandText = "FirebaseAdminService";
            serviceData["1"].AddParameter("Token", DbType.NVarChar, 4000);
            serviceData["1"].AddParameter(nameof(Title), DbType.NVarChar, 4000);
            serviceData["1"].AddParameter(nameof(Body), DbType.NVarChar, 4000);
            serviceData["1"].AddParameter("ImageUrl", DbType.NVarChar, 4000);
            serviceData["1"].AddParameter("Data", DbType.NVarChar, 4000);

            foreach (var item in dataTable.DataRows)
            {
                serviceData["1"].NewRow();
                serviceData["1"].SetValue("Token", item.String("TOKEN_STR"));
                serviceData["1"].SetValue(nameof(Title), $"{Title} {dateTime:dd HH:mm:ss}");
                serviceData["1"].SetValue(nameof(Body), $"{Body}");
                serviceData["1"].SetValue("ImageUrl", status == Status.OK ? "Complete" : "Fail");
                serviceData["1"].SetValue("Data", data != null ? JsonSerializer.Serialize(data) : null);
            }

            service = (IService)Factory.CreateInstance(serviceData.ServiceName);
            //service = (IService)new MetaFrm.Service.FirebaseAdminService();
            _ = service.Request(serviceData);
        }
        private Data.DataTable? GetFirebaseFCM_Token(string ACTION, string? EMAIL)
        {
            IService service;
            Response response;

            ServiceData serviceData = new()
            {
                TransactionScope = false,
            };
            serviceData["1"].CommandText = this.GetAttribute("SearchToken");
            serviceData["1"].CommandType = System.Data.CommandType.StoredProcedure;
            serviceData["1"].AddParameter("TOKEN_TYPE", DbType.NVarChar, 50, "Firebase.FCM");
            serviceData["1"].AddParameter(nameof(ACTION), DbType.NVarChar, 50, ACTION);
            serviceData["1"].AddParameter(nameof(EMAIL), DbType.NVarChar, 100, EMAIL);

            service = (IService)Factory.CreateInstance(serviceData.ServiceName);
            response = service.Request(serviceData);

            if (response.Status == Status.OK)
            {
                if (response.DataSet != null && response.DataSet.DataTables.Count > 0)
                {
                    Console.WriteLine("Get FirebaseFCM Token Completed !!");
                    return response.DataSet.DataTables[0];
                }
            }
            else
            {
                if (response.Message != null)
                    throw new Exception(response.Message);
            }

            throw new Exception("Get FirebaseFCM Token  Fail !!");
        }
        private Data.DataTable? SandEmail(string ACTION, string? SUBJECT, string? BODY, string? EMAIL)
        {
            IService service;
            Response response;

            ServiceData serviceData = new()
            {
                TransactionScope = false,
            };
            serviceData["1"].CommandText = this.GetAttribute("SandEmail");
            serviceData["1"].CommandType = System.Data.CommandType.StoredProcedure;
            serviceData["1"].AddParameter(nameof(ACTION), DbType.NVarChar, 50, ACTION);
            serviceData["1"].AddParameter(nameof(SUBJECT), DbType.NVarChar, 4000, SUBJECT);
            serviceData["1"].AddParameter(nameof(BODY), DbType.NVarChar, 4000, BODY);
            serviceData["1"].AddParameter(nameof(EMAIL), DbType.NVarChar, 100, EMAIL);

            service = (IService)Factory.CreateInstance(serviceData.ServiceName);
            response = service.Request(serviceData);

            if (response.Status == Status.OK)
            {
                if (response.DataSet != null && response.DataSet.DataTables.Count > 0)
                {
                    Console.WriteLine("Get FirebaseFCM Token Completed !!");
                    return response.DataSet.DataTables[0];
                }
            }
            else
            {
                if (response.Message != null)
                    throw new Exception(response.Message);
            }

            throw new Exception("Get FirebaseFCM Token  Fail !!");
        }

        private void Close()
        {
            if (_model != null && _model.IsOpen)
            {
                _model.Close();
                _model = null;
            }
            if (_connection != null && _connection.IsOpen)
            {
                _connection.Close();
                _connection = null;
            }
        }

        public static RabbitMQConsumer Instance => _consumer ?? new();

        public void Dispose()
        {
            this.Close();
        }
    }
}