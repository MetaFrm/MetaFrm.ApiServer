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

        private RabbitMQConsumer()
        {
            _consumer = this;
        }

        internal void Init()
        {
            this.Close();

            if (string.IsNullOrEmpty(this.ConnectionString))
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

            BrokerData? brokerData = JsonSerializer.Deserialize<BrokerData?>(message);

            if (brokerData == null)
                return;
            
            ((IBrokerService?)this.CreateInstance("BrokerService"))?.Request(brokerData);
            //((IBrokerService?)new MetaFrm.Service.BrokerService())?.Request(brokerData);
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