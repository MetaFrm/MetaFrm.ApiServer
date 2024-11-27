using MetaFrm.ApiServer.Auth;
using MetaFrm.Database;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// AccessCodeController
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AccessCodeController : ControllerBase, ICore
    {
        private readonly ILogger<AccessCodeController> _logger;
        private readonly Factory _factory;
        private readonly bool IsEmail;

        /// <summary>
        /// AccessCodeController
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="factory"></param>
        public AccessCodeController(ILogger<AccessCodeController> logger, Factory factory)
        {
            _logger = logger;
            _factory = factory;
            this.IsEmail = this.GetAttribute(nameof(this.IsEmail)) == "Y";

            //if (!Factory.IsRegisterInstance("MetaFrm.Service.RabbitMQConsumer"))
            //    Factory.RegisterInstance(new MetaFrm.Service.RabbitMQConsumer(this.GetAttribute("BrokerConnectionString"), this.GetAttribute("BrokerQueueName")), "MetaFrm.Service.RabbitMQConsumer");
            //if (!Factory.IsRegisterInstance("MetaFrm.Service.RabbitMQProducer"))
            //    Factory.RegisterInstance(new MetaFrm.Service.RabbitMQProducer(this.GetAttribute("BrokerConnectionString"), this.GetAttribute("BrokerQueueName")), "MetaFrm.Service.RabbitMQProducer");

            this.CreateInstance("BrokerConsumer", true, true, [this.GetAttribute("BrokerConnectionString"), this.GetAttribute("BrokerQueueName")]);
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="token"></param>
        /// <param name="accessGroup"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet(Name = "GetAccessCode")]
        [Authorize]
        public IActionResult? Get([FromHeader] string token, [FromHeader] string accessGroup, string email)
        {
            IService service;
            Response response;

            ServiceData data = new()
            {
                TransactionScope = false
            };

            data["1"].CommandText = this.GetAttribute("AccessCode");
            data["1"].CommandType = System.Data.CommandType.StoredProcedure;
            data["1"].AddParameter("EMAIL", DbType.NVarChar, 100, email);
            data["1"].AddParameter("ACCESS_GROUP", DbType.NVarChar, 10, accessGroup);

            service = (IService)Factory.CreateInstance(data.ServiceName);
            response = service.Request(data);

            if (this.IsEmail)
                Task.Run(() =>
                {
                    ((IServiceString?)this.CreateInstance("BrokerProducer", true, true, [this.GetAttribute("BrokerConnectionString"), this.GetAttribute("BrokerQueueName")]))?.Request(System.Text.Json.JsonSerializer.Serialize(new BrokerData { ServiceData = data, Response = response }));
                });

            if (response.Status != Status.OK)
            {
                _logger.LogError(0, "[{Now}] {Message} Email:{email}, AccessGroup:{accessGroup}", DateTime.Now, response.Message, email, accessGroup);

                if (response.Message != null)
                    return this.BadRequest(response.Message);
                else
                    return this.BadRequest("Access Code generation failed.");
            }
            else
            {
                if (response.DataSet != null && response.DataSet.DataTables != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
                {
                    string? accessCode = response.DataSet.DataTables[0].DataRows[0].String("ACCESS_CODE");

                    if (accessCode != null)
                        return Ok(accessCode.AesEncryptToBase64String(token, "MetaFrm"));

                    return this.BadRequest("Access Code generation failed.");
                }
                else
                {
                    _logger.LogError(0, "[{Now}] There are no projects or services. Email:{email}, AccessGroup:{accessGroup}", DateTime.Now, email, accessGroup);

                    return this.BadRequest("Access Code generation failed.");
                }
            }
        }
    }
}