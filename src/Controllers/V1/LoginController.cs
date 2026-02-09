using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using MetaFrm.Auth;
using MetaFrm.Database;
using MetaFrm.Extensions;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MetaFrm.ApiServer.Controllers.V1
{
    /// <summary>
    /// LoginController
    /// </summary>
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase, ICore
    {
        private readonly ILogger<LoginController> _logger;
        private readonly Factory factory;
        private readonly bool? IsPushNotification;
        private static string? BrokerConnectionString;
        private static string? BrokerQueueName;
        private static ICore? BrokerConsumer;
        private static IServiceString? BrokerProducer;
        private static string? CommandText;
        private static IService? Service;

        /// <summary>
        /// AssemblyController
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="_"></param>
        public LoginController(ILogger<LoginController> logger, Factory _)
        {
            this._logger = logger;
            this.factory = _;

            this.IsPushNotification ??= this.GetAttribute(nameof(this.IsPushNotification)) == "Y";
            BrokerConnectionString ??= this.GetAttribute("BrokerConnectionString");
            BrokerQueueName ??= this.GetAttribute("BrokerQueueName");
            BrokerConsumer ??= this.CreateInstance("BrokerConsumer", true, true, [BrokerConnectionString, BrokerQueueName]);
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        public IActionResult Get([FromBody] Login login)
        {
            Response response;
            AuthorizeToken? authorizeToken;
            string password1 = string.Empty;
            string email1 = string.Empty;

            authorizeToken = Request.GetAuthorizeToken();

            if (authorizeToken == null || authorizeToken.Token == null)
            {
                this._logger.Error("Invalid token. {0}", login.Email);

                return this.Unauthorized("Token error.");
            }

            try
            {
                password1 = login.Password.AesDecryptorToBase64String(login.Email, authorizeToken.Token);
                email1 = login.Email.AesDecryptorToBase64String(authorizeToken.Token, AuthType.Login);
            }
            catch (Exception ex)
            {
                this._logger.Error(ex, "{0}, {1}", login.Email, email1);

                return this.Unauthorized("Authorization failed.");
            }

            ServiceData data = new()
            {
                TransactionScope = false
            };
            data["1"].CommandText = CommandText ??= this.GetAttribute("Login");
            data["1"].CommandType = System.Data.CommandType.StoredProcedure;
            data["1"].AddParameter("EMAIL", DbType.NVarChar, 100, email1);
            data["1"].AddParameter("ACCESS_NUMBER", DbType.NVarChar, 4000, password1);

            response = (Service ??= (IService)Factory.CreateInstance(data.ServiceName)).Request(data);

            if (IsPushNotification == true)
                Task.Run(() =>
                {
                    BrokerProducer ??= ((IServiceString?)this.CreateInstance("BrokerProducer", true, true, [BrokerConnectionString ?? "", BrokerQueueName ?? ""]));
                    BrokerProducer?.Request(System.Text.Json.JsonSerializer.Serialize(new BrokerData { ServiceData = data, Response = response }));
                });

            if (response.Status != Status.OK)
            {
                this._logger.Error("{0} {1}, {2}, {3}", response.Message, authorizeToken.Token, login.Email, email1);

                return Ok(new UserInfo()
                {
                    Status = Status.Failed,
                    Message = response.Message
                });
            }
            else
            {
                if (response.DataSet != null && response.DataSet.DataTables != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
                {
                    Dictionary<string, string> keyValuePairs = [];
                    string? name;
                    string? value;

                    if (response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
                    {
                        name = response.DataSet.DataTables[0].DataRows[0].String("NAME");

                        foreach (Data.DataRow dataRow in response.DataSet.DataTables[0].DataRows)
                            foreach (Data.DataColumn dataColumn in response.DataSet.DataTables[0].DataColumns)
                                if (dataColumn.FieldName != "NAME" && name != null && dataColumn.FieldName != null && dataRow.Value(dataColumn.FieldName) != null)
                                {
                                    value = dataRow.Value(dataColumn.FieldName)?.ToString();

                                    if (value != null)
                                        keyValuePairs.Add($"{name}.{dataColumn.FieldName}", value);
                                }

                        keyValuePairs.Add("Token", Authorize.CreateToken(authorizeToken.ProjectServiceBase.ProjectID, authorizeToken.ProjectServiceBase.ServiceID, AuthType.Login, $"USP_LOGIN {email1}", this.HttpContext.Connection.RemoteIpAddress?.ToString()).Token ?? "");
                    }

                    return Ok(new UserInfo()
                    {
                        Status = Status.OK,
                        Token = keyValuePairs.AesEncryptAndSerialize(authorizeToken.Token, AuthType.Login),
                    });
                }
                else
                {
                    this._logger.Error("Account information is missing. {0}, {1}, {2}", authorizeToken.Token, login.Email, email1);
                    return Ok(null);
                }
            }
        }
    }
}