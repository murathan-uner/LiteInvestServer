

using LiteInvestFront.Components.Pages;
using LiteInvestServerDll;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiteInvestFront.Controller
{

    /// <summary>
    /// Контроллер для работы с позициями
    /// </summary>
    [ApiController]
    [Tags("Positions")]
    public class PositionController : ControllerBase
    {




        /// <summary>
        /// Получить позиции трейдера
        /// </summary>
        /// <param name="login">Трейдер</param>
        /// <param name="statePosition">Состояние позиции</param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/positions")]
        [Authorize]
        public IActionResult GetUserPositions(string login, StatePosition statePosition)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                if (!(users?.ContainsKey(login) ?? false))
                {
                    return new BadRequestResult();
                }
                var positions = serverService.GetAllPositionsForUser(login);
                var filterPositions = positions.Where(p =>
                {
                    if (statePosition == StatePosition.Open && p.PosValue != 0)
                    {
                        return true;
                    }
                    else if (statePosition == StatePosition.Close && p.PosValue == 0)
                    {
                        return true;
                    }
                    return false;
                }
                );

                return new JsonResult(filterPositions);

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Получить все позиции на бирже по общему биржевому  счету 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/positions/board")]
        [Authorize]
        public IActionResult GetBoardPositions()
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                var positions = serverService.GetBoardPosition();
                return new JsonResult(positions);

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// User position request
        /// </summary>
        public class UserPositionRequest
        {
            /// <summary>
            /// User name
            /// </summary>
            public string login { get; set; }
            /// <summary>
            /// State Position 
            /// </summary>
            public StatePosition statePosition { get; set; }

        }



        /// <summary>
        /// Закрыть все позиции трейдера по всем или по заданному инструменту .
        /// </summary>
        /// <param name="login">Имя пользователя</param>
        /// <param name="instrument">Инструмент, не обязательный параметр</param>
        /// <returns></returns>
        [HttpDelete]
        [Route("/api/positions/close")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [Authorize]
        public IActionResult CloseAllUserPositionsByInstrument(string login, string? instrument)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                if (!(users?.ContainsKey(login) ?? false))
                {
                    return new BadRequestResult();
                }

                var res = serverService.ClosePositionBySecurity(login, instrument).Result;
                return new JsonResult(res);

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }



        /// <summary>
        /// State Position
        /// </summary>
        public enum StatePosition
        {
            /// <summary>
            /// Open Position
            /// </summary>
            Open,
            /// <summary>
            /// Close Position
            /// </summary>
            Close
        }

        ServerService serverService;

        ServiceDB serviceDB;

        System.Collections.Concurrent.ConcurrentDictionary<string, LiteInvest.Entity.ServerEntity.User>? users;

        

        /// <summary>
        /// Dependency Injection
        /// </summary>
        /// <param name="_serverService"></param>
        /// <param name="_serviceDB"></param>
        public PositionController(ServerService _serverService, ServiceDB _serviceDB)
        {
            serviceDB = _serviceDB;
            serverService = _serverService;
            users = serviceDB.GetUsersContext();
            

        }

    }
}
