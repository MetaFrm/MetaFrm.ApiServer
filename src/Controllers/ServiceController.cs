using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using MetaFrm.ApiServer.RabbitMQ;
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
        private readonly string[] NotAuthorizeCommandText = (Factory.ProjectService.GetAttributeValue(nameof(NotAuthorizeCommandText)) ?? "").Split(',');
        private readonly string[] RabbitMQProducerCommandText = (Factory.ProjectService.GetAttributeValue(nameof(RabbitMQProducerCommandText)) ?? "").Split(',');

        /// <summary>
        /// ServiceController
        /// </summary>
        /// <param name="logger"></param>
        public ServiceController(ILogger<ServiceController> logger)
        {
            _logger = logger;
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
            IService service;
            Response response;

            try
            {
                if (serviceData.ServiceName == null)
                    return this.BadRequest("ServiceName is null.");

                if (!Authorize.AuthorizeTokenList.TryGetValue(token, out AuthorizeToken? authorizeToken))
                {
                    var projectServiceBase = token.AesDecryptorAndDeserialize<ProjectServiceBase>();

                    if (projectServiceBase == null || projectServiceBase.ProjectID != Factory.ProjectID)
                        return this.Unauthorized("Token error.");

                    if (serviceData.Commands.Count != 1)
                        return this.BadRequest("No command.");

                    foreach (var command in serviceData.Commands)
                        if (!this.NotAuthorizeCommandText.Contains(command.Value.CommandText))
                            return this.BadRequest("No CommandText.");
                }

                service = (IService)Factory.CreateInstance(serviceData.ServiceName);
                response = service.Request(serviceData);

                foreach (var command in serviceData.Commands)
                    if (this.RabbitMQProducerCommandText.Contains(command.Value.CommandText))
                        Task.Run(() =>
                        {
                            RabbitMQProducer.Instance.BasicPublish(System.Text.Json.JsonSerializer.Serialize(new BrokerData { ServiceData = serviceData, Response = response }));
                        });
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