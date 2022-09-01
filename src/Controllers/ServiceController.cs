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
                    throw new MetaFrmException("ServiceName is null.");

                if (!Authorize.AuthorizeTokenList.ContainsKey(token))
                {
                    var projectServiceBase = token.AesDecryptorAndDeserialize<ProjectServiceBase>();

                    if (projectServiceBase == null || projectServiceBase.ProjectID != Factory.ProjectID)
                        throw new MetaFrmException("Token error.");

                    if (serviceData.Commands.Count != 1)
                        return this.Unauthorized();

                    foreach (var command in serviceData.Commands)
                        if (!notAuthorizeCommandText.Contains(command.Value.CommandText))
                            return this.Unauthorized();
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