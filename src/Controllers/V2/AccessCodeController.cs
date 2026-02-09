using MetaFrm.Api;
using MetaFrm.ApiServer.Auth;
using MetaFrm.Database;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MetaFrm.ApiServer.Controllers.V2
{
    /// <summary>
    /// AccessCodeController
    /// </summary>
    [Route("api/v2/[controller]")]
    [ApiController]
    public class AccessCodeController : ControllerBase, ICore
    {
        private readonly ILogger<AccessCodeController> _logger;
        private readonly Factory factory;
        private static bool? IsEmail;
        private static string? BrokerConnectionString;
        private static string? BrokerQueueName;
        private static ICore? BrokerConsumer;
        private static IServiceString? BrokerProducer;
        private static string? CommandText;
        private static IService? Service;

        /// <summary>
        /// AccessCodeController
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="_"></param>
        public AccessCodeController(ILogger<AccessCodeController> logger, Factory _)
        {
            this._logger = logger;
            this.factory = _;

            IsEmail ??= this.GetAttribute(nameof(this.IsEmail)) == "Y";
            BrokerConnectionString ??= this.GetAttribute("BrokerConnectionString");
            BrokerQueueName ??= this.GetAttribute("BrokerQueueName");
            BrokerConsumer ??= this.CreateInstance("BrokerConsumer", true, true, [BrokerConnectionString, BrokerQueueName]);
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="accessGroup"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public IActionResult? Get([FromHeader(Name = Headers.AccessGroup)] string accessGroup, [FromBody] string email)
        {
            Response response;
            AuthorizeToken? authorizeToken;

            authorizeToken = Request.GetAuthorizeToken();

            if (authorizeToken == null || authorizeToken.Token == null)
            {
                this._logger.Error("Invalid token. {0}", authorizeToken);

                return this.BadRequest("Invalid token.");
            }

            try
            {
                email = email.AesDecryptorToBase64String(authorizeToken.Token, accessGroup);
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "{0}, {1}, {2}", authorizeToken.Token, email, accessGroup);

                return this.BadRequest("Access Code generation failed.");
            }

            ServiceData data = new()
            {
                TransactionScope = false
            };

            data["1"].CommandText = CommandText ??= this.GetAttribute("AccessCode");
            data["1"].CommandType = System.Data.CommandType.StoredProcedure;
            data["1"].AddParameter("EMAIL", DbType.NVarChar, 100, email);
            data["1"].AddParameter("ACCESS_GROUP", DbType.NVarChar, 10, accessGroup);

            response = (Service ??= (IService)Factory.CreateInstance(data.ServiceName)).Request(data);

            if (IsEmail == true)
                Task.Run(() =>
                {
                    BrokerProducer ??= ((IServiceString?)this.CreateInstance("BrokerProducer", true, true, [BrokerConnectionString ?? "", BrokerQueueName ?? ""]));
                    BrokerProducer?.Request(System.Text.Json.JsonSerializer.Serialize(new BrokerData { ServiceData = data, Response = response }));
                });

            if (response.Status != Status.OK)
            {
                this._logger.Error("{0} {1}, {2}, {3}", response.Message, authorizeToken.Token, email, accessGroup);

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
                        return Ok(accessCode.AesEncryptToBase64String(authorizeToken.Token, accessGroup));

                    this._logger.Error("Access Code generation failed(ACCESS_CODE). {0}, {1}, {2}", authorizeToken.Token, email, accessGroup);

                    return this.BadRequest("Access Code generation failed.");
                }
                else
                {
                    this._logger.Error("Access Code generation failed(response.DataSet). {0}, {1}, {2}", authorizeToken.Token, email, accessGroup);

                    return this.BadRequest("Access Code generation failed.");
                }
            }
        }
    }
}