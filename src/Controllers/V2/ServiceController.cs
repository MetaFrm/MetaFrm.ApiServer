using MetaFrm.ApiServer.Auth;
using MetaFrm.Auth;
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
        public IActionResult Get(ServiceData serviceData)
        {
            Response response;
            AuthorizeToken? authorizeToken = null;

            try
            {
                authorizeToken = Request.GetAuthorizeToken();

                if (authorizeToken == null)
                {
                    this._logger.Error("Invalid token. {0}", System.Text.Json.JsonSerializer.Serialize(serviceData));

                    return this.BadRequest("Invalid token.");
                }

                if (serviceData.Commands.Count < 1)
                {
                    this._logger.Error("No command. {0}, {1}", authorizeToken.Token, System.Text.Json.JsonSerializer.Serialize(serviceData));

                    return this.BadRequest("No command.");
                }

                if (authorizeToken.TokenType == AuthType.ProjectService)
                    foreach (var command in serviceData.Commands)
                        if (!this.NotAuthorizeCommandText.Contains(command.Value.CommandText))
                        {
                            this._logger.Error("No CommandText. {0}, {1}", authorizeToken.Token, System.Text.Json.JsonSerializer.Serialize(serviceData));

                            return this.BadRequest("No CommandText.");
                        }

                if (!string.IsNullOrEmpty(serviceData.ServiceName))
                    response = ((IService)Factory.CreateInstance(serviceData.ServiceName)).Request(serviceData);
                else
                {
                    response = new()
                    {
                        Status = Status.OK
                    };
                }

                bool isBrokerProducerCommandText = false;
                bool isBrokerProducerCommandTextParallel = false;
                foreach (var command in serviceData.Commands)
                {
                    if (!isBrokerProducerCommandText && this.BrokerProducerCommandText.Contains(command.Value.CommandText))
                    {
                        isBrokerProducerCommandText = true;

                        Task.Run(() =>
                        {
                            BrokerProducer ??= (IServiceString?)this.CreateInstance("BrokerProducer", true, true, [BrokerConnectionString!, BrokerQueueName!]);
                            BrokerProducer?.Request(System.Text.Json.JsonSerializer.Serialize(new BrokerData { ServiceData = serviceData, Response = response }));
                        });
                    }
                    if (!isBrokerProducerCommandTextParallel && this.BrokerProducerCommandTextParallel.Contains(command.Value.CommandText))
                    {
                        isBrokerProducerCommandTextParallel = true;

                        Task.Run(() =>
                        {
                            BrokerProducerParallel?.Request(System.Text.Json.JsonSerializer.Serialize(new BrokerData { ServiceData = serviceData, Response = response }));
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "{0}, {1}", authorizeToken?.Token, System.Text.Json.JsonSerializer.Serialize(serviceData));

                response = new()
                {
                    Status = Status.Failed,
                    Message = ex.Message,
                };
            }

            return Ok(response);
        }
    }
}