using MetaFrm.Api.Models;
using MetaFrm.ApiServer.Auth;
using MetaFrm.Service;
using Microsoft.AspNetCore.Mvc;

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
        private readonly string[] notAuthorizeCommandText = (Factory.ProjectService.GetAttributeValue("NotAuthorizeCommandText") ?? "").Split(',');//new string[] { "[dbo].[USP_JOIN]", "[dbo].[USP_MENU_RESPONSIBILITY_SEL_DEFAULT]", "[dbo].[USP_MENU_RESPONSIBILITY_ASSEMBLY_SEL_DEFAULT]", "[dbo].[USP_PASSWORD_RESET]" };
        private readonly int? MainProject = null;

        /// <summary>
        /// ServiceController
        /// </summary>
        /// <param name="logger"></param>
        public ServiceController(ILogger<ServiceController> logger)
        {
            string? tmp;

            _logger = logger;

            tmp = this.GetAttribute("MainProject");

            if (tmp.IsNullOrEmpty())
                MainProject = null;
            else
                MainProject = tmp.ToInt();
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

                    if (projectServiceBase == null)
                        return this.Unauthorized("Token error.");

                    if (MainProject == null && projectServiceBase.ProjectID != Factory.ProjectID)
                        return this.Unauthorized("Token error.");

                    if (serviceData.Commands.Count != 1)
                        return this.BadRequest("No command.");

                    foreach (var command in serviceData.Commands)
                        if (!notAuthorizeCommandText.Contains(command.Value.CommandText))
                            return this.BadRequest("No CommandText.");
                }

                if (authorizeToken != null && authorizeToken.ProjectServiceBase.ProjectID != Factory.ProjectID && MainProject != null)
                {
                    serviceData["MetaFrm.Database.Adapter"].CommandText = this.GetAttribute("MetaFrm.Database.Adapter.Attribute");
                    serviceData["MetaFrm.Database.Adapter"].AddParameter("PROJECT_ID", Database.DbType.Decimal, 18, authorizeToken.ProjectServiceBase.ProjectID);
                    serviceData["MetaFrm.Database.Adapter"].AddParameter("SERVICE_ID", Database.DbType.Decimal, 18, authorizeToken.ProjectServiceBase.ServiceID);
                    serviceData["MetaFrm.Database.Adapter"].AddParameter("NAMESPACE", Database.DbType.NVarChar, 6000, this.GetAttribute("MetaFrm.Database.Adapter"));
                }

                service = (IService)Factory.CreateInstance(serviceData.ServiceName);
                response = service.Request(serviceData);
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