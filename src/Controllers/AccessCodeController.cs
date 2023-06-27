using MetaFrm.ApiServer.Auth;
using MetaFrm.Database;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;

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

        /// <summary>
        /// AccessCodeController
        /// </summary>
        /// <param name="logger"></param>
        public AccessCodeController(ILogger<AccessCodeController> logger)
        {
            _logger = logger;
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
        public string? Get([FromHeader] string token, [FromHeader] string accessGroup, string email)
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

            try
            {
                if (data.ServiceName == null)
                    throw new MetaFrmException("ServiceName is null.");

                service = (IService)Factory.CreateInstance(data.ServiceName);
                response = service.Request(data);

                if (response.Status != Status.OK)
                {
                    _logger.LogError(0, "[{Now}] {Message} Email:{email}, AccessGroup:{accessGroup}", DateTime.Now, response.Message, email, accessGroup);

                    if (response.Message != null)
                        throw new MetaFrmException(response.Message);
                    else
                        throw new MetaFrmException("Access Code generation failed.");
                }
                else
                {
                    if (response.DataSet != null && response.DataSet.DataTables != null && response.DataSet.DataTables.Count > 0 && response.DataSet.DataTables[0].DataRows.Count > 0)
                    {
                        string? accessCode = response.DataSet.DataTables[0].DataRows[0].String("ACCESS_CODE");

                        if (accessCode != null)
                            return accessCode.AesEncryptToBase64String(token, "MetaFrm");

                        throw new MetaFrmException("Access Code generation failed.");
                    }
                    else
                    {
                        _logger.LogError(0, "[{Now}] There are no projects or services. Email:{email}, AccessGroup:{accessGroup}", DateTime.Now, email, accessGroup);

                        throw new MetaFrmException("Access Code generation failed.");
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(0, "[{Now}] Service execute exception. Exception:{exception}", DateTime.Now, exception);

                throw new MetaFrmException(exception);
            }
        }
    }
}