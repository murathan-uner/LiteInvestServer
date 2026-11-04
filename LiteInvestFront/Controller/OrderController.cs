
using LiteInvest.Entity.PlazaEntity;

using LiteInvestServerDll;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using static LiteInvest.Entity.PlazaEntity.Order;

namespace LiteInvestFront.Controller
{
    /// <summary>
    /// Контроллер для работы с ордерами
    /// </summary>
    [ApiController]
    [Tags("Orders")]
    public class OrderController : ControllerBase
    {
        /// <summary>
        /// Получить все ордера трейдера
        /// </summary>
        /// <param name="login">Трейдер</param>
        /// <param name="orderState">Состояние ордера</param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/orders")]
        [Authorize]
        public IActionResult GetUserOrders(string login, Order.OrderStateType? orderState)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                if (!(users?.ContainsKey(login) ??false))
                {
                    return new BadRequestObjectResult("User  необходимо указать"); 
                }
                if (!(orders?.ContainsKey(login)??false))
                {
                    return new JsonResult(new List<Order>());
                }
                ICollection<Order>? ordersResult;
                if (orderState != null)
                {
                    ordersResult = orders[login].Where(o => o.Value.State == orderState).ToDictionary().Values;
                }
                else
                {
                    ordersResult = orders[login].Values;
                }
                
                return new JsonResult(ordersResult);
                
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }


        /// <summary>
        /// Отменить все ордера заданного трейдера
        /// </summary>
        /// <param name="login">Трейдер</param>
        /// <returns></returns>
        [HttpDelete]
        [Route("/api/orders/cancel")]
        [Authorize]
        public IActionResult CancelAllUserOrders(string login)
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
                if (!(orders?.ContainsKey(login) ?? false))
                {
                    return new JsonResult(new List<Order>());
                }

                var res = serverService.CancelAllUserOrder(login);
                

                return new JsonResult(res);

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }


        ServerService serverService;

        ServiceDB serviceDB;

        System.Collections.Concurrent.ConcurrentDictionary<string, LiteInvest.Entity.ServerEntity.User>? users;

        System.Collections.Concurrent.ConcurrentDictionary<string, System.Collections.Concurrent.ConcurrentDictionary<int, LiteInvest.Entity.PlazaEntity.Order>>? orders;

        /// <summary>
        /// Dependency Injection
        /// </summary>
        /// <param name="_serverService"></param>
        /// <param name="_serviceDB"></param>
        public OrderController(ServerService _serverService, ServiceDB _serviceDB)
        {
            serviceDB = _serviceDB;
            serverService = _serverService;
            users = serviceDB.GetUsersContext();
            orders = serviceDB.GetOrders();

        }

    }
}
