using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// ServiceController
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase, ICore
    {
        private readonly ILogger<ServiceController> _logger;
        private readonly Factory _factory;
        private readonly string[] NotAuthorizeCommandText = (Factory.ProjectService.GetAttributeValue(nameof(NotAuthorizeCommandText)) ?? "").Split(',');
        private readonly string[] BrokerProducerCommandText = (Factory.ProjectService.GetAttributeValue(nameof(BrokerProducerCommandText)) ?? "").Split(',');
        private readonly string[] BrokerProducerCommandTextParallel = (Factory.ProjectService.GetAttributeValue(nameof(BrokerProducerCommandTextParallel)) ?? "").Split(',');

        /// <summary>
        /// ServiceController
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="factory"></param>
        public ServiceController(ILogger<ServiceController> logger, Factory factory)
        {
            _logger = logger;
            _factory = factory;

            //if (!Factory.IsRegisterInstance("MetaFrm.Service.RabbitMQConsumer"))
            //    Factory.RegisterInstance(new MetaFrm.Service.RabbitMQConsumer(this.GetAttribute("BrokerConnectionString"), this.GetAttribute("BrokerQueueName")), "MetaFrm.Service.RabbitMQConsumer");
            //if (!Factory.IsRegisterInstance("MetaFrm.Service.RabbitMQProducer"))
            //    Factory.RegisterInstance(new MetaFrm.Service.RabbitMQProducer(this.GetAttribute("BrokerConnectionString"), this.GetAttribute("BrokerQueueName")), "MetaFrm.Service.RabbitMQProducer");

            this.CreateInstance("BrokerConsumer", true, true, [this.GetAttribute("BrokerConnectionString"), this.GetAttribute("BrokerQueueName")]);

            if (!Factory.IsRegisterInstance(nameof(BrokerProducerCommandTextParallel)) && !this.GetAttribute("BrokerQueueNameParallel").IsNullOrEmpty())
            {
                ICore? serviceString = this.CreateInstance("BrokerProducer", false, true, [this.GetAttribute("BrokerConnectionStringParallel"), this.GetAttribute("BrokerQueueNameParallel")]);
                if (serviceString != null)
                    Factory.RegisterInstance(serviceString, nameof(BrokerProducerCommandTextParallel));
            }
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="token"></param>
        /// <param name="serviceData"></param>
        /// <returns></returns>
        [HttpPost(Name = "GetService")]
        [Authorize]
        public IActionResult Get([FromHeader] string token, ServiceData serviceData)
        {
            Response response;

            try
            {
                if (!Authorize.AuthorizeTokenList.TryGetValue(token, out AuthorizeToken? authorizeToken))
                {
                    var projectServiceBase = token.AesDecryptorAndDeserialize<ProjectServiceBase>();

                    if (projectServiceBase == null || Factory.ProjectServiceBase == null || projectServiceBase.ProjectID != Factory.ProjectServiceBase.ProjectID)
                        return this.Unauthorized("Token error.");

                    if (serviceData.Commands.Count != 1)
                        return this.BadRequest("No command.");

                    foreach (var command in serviceData.Commands)
                        if (!this.NotAuthorizeCommandText.Contains(command.Value.CommandText))
                            return this.BadRequest("No CommandText.");
                }

                if (!serviceData.ServiceName.IsNullOrEmpty())
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
                            ((IServiceString?)this.CreateInstance("BrokerProducer", true, true, [this.GetAttribute("BrokerConnectionString"), this.GetAttribute("BrokerQueueName")]))?.Request(System.Text.Json.JsonSerializer.Serialize(new BrokerData { ServiceData = serviceData, Response = response }));
                        });
                    }
                    if (!isBrokerProducerCommandTextParallel && this.BrokerProducerCommandTextParallel.Contains(command.Value.CommandText))
                    {
                        isBrokerProducerCommandTextParallel = true;

                        Task.Run(() =>
                        {
                            if (Factory.IsRegisterInstance(nameof(BrokerProducerCommandTextParallel)))
                                ((IServiceString?)Factory.LoadInstance(nameof(BrokerProducerCommandTextParallel)))?.Request(System.Text.Json.JsonSerializer.Serialize(new BrokerData { ServiceData = serviceData, Response = response }));
                        });
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(0, "[{Now}] Service execute exception. Exception:{exception}", DateTime.Now, exception);

                response = new()
                {
                    Status = Status.Failed,
                    Message = exception.Message,
                    //#if DEBUG
                    //                    ServiceException = new ServiceException(exception),
                    //#endif
                };
            }

            return Ok(response);
        }
    }
}