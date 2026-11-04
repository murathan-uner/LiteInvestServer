using LiteInvest.Entity.ServerEntity;

using LiteInvestServerDll;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LiteInvestFront.Controller
{
    /// <summary>
    /// Контроллер для работы с комиссиями
    /// </summary>
    [ApiController]
    [Tags("Commission")]
    //[Consumes("application/json")]
    [Route("/api/commission")]
    public class CommissionController : ControllerBase
    {

       

        /// <summary>
        /// Получить значение комиссии проп по рынку
        /// </summary>
        /// <returns>Комиссия Проп</returns>
        [HttpGet]
        [Route("prop")]
        [Authorize]
        //[Produces("application/json")]
        //[Consumes("application/json")]
        public IActionResult GetCommissionProp(BoardEnum? board)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                if (board == null)
                {
                    return new BadRequestObjectResult("Не верная торговая площадка");
                }
                CommissionBase? com = CommissionManager.GetCommissionProp((BoardEnum)board);

                return new JsonResult(com);

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

        }

        /// <summary>
        /// Установить значение комиссии проп по рынку
        /// </summary>
        /// <param name="board">Рынок</param>
        /// <param name="commission">Параметры комиссии</param>
        /// <returns></returns>
        [HttpPut]
        [Route("prop")]
        [Authorize]
        [Produces("application/json")]
        [Consumes("application/json")]
        public IActionResult SetCommissionProp(SetCommissionBoardRequest commission)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                CommissionManager.SetCommissionProp(commission.board, commission.commission);
                CommissionBase? com = CommissionManager.GetCommissionProp(commission.board);

                return new JsonResult(com);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

        }







        /// <summary>
        /// Получить значение комиссии брокера по рынку
        /// </summary>
        /// <returns>Комиссии брокера по рынку</returns>
        [HttpGet]
        [Route("broker")]
        [Authorize]
        //[Produces("application/json")]
        //[Consumes("application/json")]
        public IActionResult GetCommissionBroker(BoardEnum board)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                CommissionBase? com = CommissionManager.GetCommissionBroker(board);

                return new JsonResult(com);

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

        }

        /// <summary>
        /// Установить значение комиссии брокера по рынку
        /// </summary>
        /// <param name="board">Рынок</param>
        /// <param name="commission">Параметры комиссии</param>
        /// <returns></returns>
        [HttpPut]
        [Route("broker")]
        [Authorize]
        [Produces("application/json")]
        [Consumes("application/json")]
        public IActionResult SetCommissionBroker(SetCommissionBoardRequest commission)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                CommissionManager.SetCommissionBroker(commission.board, commission.commission);
                CommissionBase? com = CommissionManager.GetCommissionBroker(commission.board);

                return new JsonResult(com);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

        }

        /// <summary>
        /// Установить размер по рынку
        /// </summary>
        public record SetCommissionBoardRequest
        {
            /// <summary>
            /// Пользователь
            /// </summary>
            public BoardEnum board { get; set; }
            /// <summary>
            /// Параметры комиссии
            /// </summary>
            public CommissionBase commission { get; set; }


        }


        System.Collections.Concurrent.ConcurrentDictionary<string, LiteInvest.Entity.ServerEntity.User>? users;

        public CommissionController(ServiceDB _serviceDB)
        {   
            users = _serviceDB.GetUsersContext();
        }

    }

}


//Не актуально
/*

       /// <summary>
       /// Получить значение комиссии payout по трейдеру
       /// </summary>
       /// <returns>Комиссия PayOut</returns>
       [HttpGet]
       [Route("payout")]
       [Authorize]
       //[Produces("application/json")]
       //[Consumes("application/json")]
       public IActionResult GetCommissionPayout(string userName)
       {
           try
           {
               if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
               {
                   return new UnauthorizedResult();
               }
               if (string.IsNullOrWhiteSpace(userName) || !users.ContainsKey(userName))
               {
                   return new BadRequestObjectResult("Не верный пользователь");
               }
               CommissionBase? com = CommissionManager.GetCommissionPayOut(userName);

               return new JsonResult(com);

           }
           catch (Exception ex)
           {
               return new BadRequestObjectResult(ex.Message);
           }

       }

       /// <summary>
       /// Установить значение комиссии payout по трейдеру
       /// </summary>
       /// <param name="board">Рынок</param>
       /// <param name="commission">Параметры комиссии</param>
       /// <returns></returns>
       [HttpPut]
       [Route("payout")]
       [Authorize]
       [Produces("application/json")]
       [Consumes("application/json")]
       public IActionResult SetCommissionPayOut(SetCommissionUserRequest commission)
       {
           try
           {
               if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
               {
                   return new UnauthorizedResult();
               }
               if (commission.commission.ValueType != ValueTypeEnum.Percent)
               {
                   return new BadRequestObjectResult("Комиссия payout должна быть в процентах.");
               }
               if (string.IsNullOrWhiteSpace(commission.userName) || !users.ContainsKey(commission.userName))
               {
                   return new BadRequestObjectResult("Не верный пользователь");
               }
               CommissionManager.SetCommissionPayOut(commission.userName, commission.commission);
               CommissionBase? com = CommissionManager.GetCommissionPayOut(commission.userName);

               return new JsonResult(com);
           }
           catch (Exception ex)
           {
               return new BadRequestObjectResult(ex.Message);
           }

       }

       /// <summary>
       /// Установить размер PayOut по тредеру
       /// </summary>
       public record SetCommissionUserRequest
       {
           /// <summary>
           /// Пользователь
           /// </summary>
           public string userName { get; set; }
           /// <summary>
           /// Параметры комиссии
           /// </summary>
           public CommissionBase commission { get; set; }

           /// <summary>
           /// Площадка
           /// </summary>
           public BoardEnum board { get; set; }

       }

      
       */
