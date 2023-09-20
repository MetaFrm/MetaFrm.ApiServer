using RabbitMQ.Client;
using System.Text;

namespace MetaFrm.ApiServer.RabbitMQ
{
    internal class RabbitMQProducer : ICore, IDisposable
    {
        private static RabbitMQProducer? _producer;
        private IConnection? _connection;
        private IModel? _model;
        internal string? ConnectionString { get; private set; }
        internal string? QueueName { get; private set; }

        private RabbitMQProducer()
        {
            _producer = this;
        }

        private void Init()
        {
            this.Close();

            this.ConnectionString = this.GetAttribute("ConnectionString");
            this.QueueName = this.GetAttribute("QueueName");

            _ = RabbitMQConsumer.Instance;

            var factory = new ConnectionFactory();
            factory.Uri = new(this.ConnectionString);
            this._connection = factory.CreateConnection();

            this._model = _connection.CreateModel();
            this._model.QueueDeclare(queue: this.QueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
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

        public static RabbitMQProducer Instance => _producer ?? new();

        public void Dispose()
        {
            this.Close();
        }

        public void BasicPublish(string json)
        {
            if (_model == null)
                this.Init();
            if (_model == null)
                return;

            _model.BasicPublish(string.Empty, this.QueueName, null, Encoding.UTF8.GetBytes(json));
        }
    }
}