using LiteInvestServerDll;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LiteInvest.Entity.ServerEntity;
using LiteInvest.Entity.PlazaEntity;
using LiteInvestFront.Components;

namespace LiteInvestFront.Controller
{


    #if DEBUG 



    /// <summary>
    /// Настроечные параметры сервера
    /// </summary>
    [ApiController]
    [Tags("Develop server parameters")]
    public class DevelopController : ControllerBase
    {

        /// <summary>
        /// Установить время клиринга
        /// </summary>
        /// <param name="timeClearingRequest">Время клиринга на площадке</param>
        /// <returns></returns>
        [HttpPost]
        [Route("/api/parametrs/timeClearing")]
        [EndpointDescription("Если указать пустой запрос,  то возвращает текущее время клиринга")]
        [Authorize]
        public IActionResult SetTimeClearing(TimeClearingRequest? timeClearingRequest)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                if (timeClearingRequest != null)
                {
                    PlazaOptions.TimeClearing[timeClearingRequest.board] = timeClearingRequest.timeClearing;
                }
                return new JsonResult(PlazaOptions.TimeClearing);

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        public record TimeClearingRequest
        {
            /// <summary>
            /// Площадка
            /// </summary>
            public BoardEnum board {  get; set; }

            /// <summary>
            /// Время клиринга
            /// </summary>
            public TimeSpan timeClearing {  get; set; }
            
        }

        AccountManager accountManager;

        ServerService serverService;

        ServiceDB serviceDB;

        ConcurrentDictionary<string, LiteInvest.Entity.ServerEntity.User>? users;

        IConfiguration configuration;
        public DevelopController(AccountManager _accountManager, ServerService _serverService, ServiceDB _serviceDB, IConfiguration configuration)
        {
            accountManager = _accountManager;
            serviceDB = _serviceDB;
            serverService = _serverService;
            users = serviceDB.GetUsersContext();
            this.configuration = configuration;
        }

    }


#endif
}
