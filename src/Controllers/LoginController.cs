using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using MetaFrm.Database;
using MetaFrm.Extensions;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;

namespace MetaFrm.ApiServer.Controllers
{
    /// <summary>
    /// LoginController
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase, ICore
    {
        private readonly ILogger<LoginController> _logger;

        /// <summary>
        /// AssemblyController
        /// </summary>
        /// <param name="logger"></param>
        public LoginController(ILogger<LoginController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <param name="token"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpPost(Name = "GetLogin")]
        public UserInfo? Get([FromHeader] string token, string email, string password)
        {
            IService service;
            Response response;

            var projectServiceBase = token.AesDecryptorAndDeserialize<ProjectServiceBase>();

            if (projectServiceBase == null || projectServiceBase.ProjectID != Factory.ProjectID)
                throw new MetaFrmException("Token error.");

            ServiceData data = new()
            {
                TransactionScope = false
            };

            password = password.AesDecryptorToBase64String(email, token);
            email = email.AesDecryptorToBase64String(token, "MetaFrm");

            data["1"].CommandText = "[dbo].[USP_LOGIN]";
            data["1"].CommandType = System.Data.CommandType.StoredProcedure;
            data["1"].AddParameter("EMAIL", DbType.NVarChar, 100, email);
            data["1"].AddParameter("ACCESS_NUMBER", DbType.NVarChar, 4000, password);

            service = (IService)Factory.CreateInstance(data.ServiceName);
            response = service.Request(data);

            if (response.Status != Status.OK)
            {
                _logger.LogError(0, "[{Now}] {Message} Token:{token}, Email:{email}, Password:{password}", DateTime.Now, response.Message, token, email, password);

                return new UserInfo()
                {
                    Status = Status.Failed,
                    Message = response.Message
                };
            }
            else
            {
                if (response.DataSet != null && response.DataSet.DataTables != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
                {
                    Dictionary<string, string> keyValuePairs = new();
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

                        keyValuePairs.Add("Token", Authorize.CreateToken(projectServiceBase.ProjectID, projectServiceBase.ServiceID, null).GetToken ?? "");
                    }

                    return new UserInfo()
                    {
                        Status = Status.OK,
                        Token = keyValuePairs.AesEncryptAndSerialize(token, "MetaFrm"),
                    };
                }
                else
                {
                    _logger.LogError(0, "[{Now}] Account information is missing. Token:{token}, Email:{email}, Password:{password}", DateTime.Now, token, email, password);
                    return null;
                }
            }
        }
    }
}