using RabbitMQ.Client;
using System.Text;

namespace MetaFrm.ApiServer.RabbitMQ
{
    internal class RabbitMQProducer : ICore, IDisposable
    {
        private static RabbitMQProducer? _producer;
        private static RabbitMQConsumer? _consumer;
        private IConnection? _connection;
        private IModel? _model;
        internal string? ConnectionString { get; set; }
        internal string? QueueName { get; set; }

        private RabbitMQProducer()
        {
            _producer = this;
        }

        private void Init()
        {
            this.Close();

            this.ConnectionString = this.GetAttribute("ConnectionString");
            this.QueueName = this.GetAttribute("QueueName");

            if(_consumer == null)
            {
                _consumer = RabbitMQConsumer.Instance;
                _consumer.ConnectionString = this.ConnectionString;
                _consumer.QueueName = this.QueueName;
                _consumer.Init();
            }

            this._connection = new ConnectionFactory
            {
                Uri = new(this.ConnectionString)
            }.CreateConnection();

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

        //https://dotnetblog.asphostportal.com/how-to-make-sure-your-asp-net-core-keep-running-on-iis/
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