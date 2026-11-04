using Microsoft.AspNetCore.Mvc;

using LiteInvestFront.Components.Pages;
using LiteInvestServerDll;

using Microsoft.AspNetCore.Authorization;
using LiteInvest.Entity.ServerEntity;
using static LiteInvestFront.Controller.UserController;


namespace LiteInvestFront.Controller
{


    [ApiController]
    [Tags("Finance")]
    public class FinanceResultСontroller : ControllerBase
    {

        /// <summary>
        /// Получить финансовый результат
        /// </summary>
        /// <param name="accountCapexNumber">Номер капитального счета, не обязательный параметр</param>
        /// <param name="login">Логин, не обязательный параметр</param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/finance")]
        //[Produces("application/json")]
        //[Consumes("application/json")]
        [Authorize]
        public IActionResult GetFinanceResult(string? accountCapexNumber, string? login)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                List<FinanceResult>? f = serverService.GetAllFinanceResult();
                f = f.Where(f => (string.IsNullOrWhiteSpace(accountCapexNumber) || f.AccountCapex == accountCapexNumber)
                                         && (string.IsNullOrWhiteSpace(login) || f.UserName == login)).ToList();

                if (f != null)
                {
                    return new JsonResult(f);
                }
                else
                {
                    return new BadRequestObjectResult($"Счет или пользователь не найден");

                }
            }
            catch(Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

            AccountManager accountManager;

        ServerService serverService;

        ServiceDB serviceDB;

        System.Collections.Concurrent.ConcurrentDictionary<string, LiteInvest.Entity.ServerEntity.User>? users;

        public FinanceResultСontroller(AccountManager _accountManager, ServerService _serverService, ServiceDB _serviceDB)
        {
            accountManager = _accountManager;
            serviceDB = _serviceDB;
            serverService = _serverService;
            users = serviceDB.GetUsersContext();
        }

    }
}
