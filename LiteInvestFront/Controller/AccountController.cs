using Microsoft.AspNetCore.Mvc;

using LiteInvestFront.Components.Pages;
using LiteInvestServerDll;

using Microsoft.AspNetCore.Authorization;
using LiteInvest.Entity.ServerEntity;
using static LiteInvestFront.Controller.UserController;


namespace LiteInvestFront.Controller
{


    /// <summary>
    /// Контроллер для работы со счетами 
    /// </summary>
    [ApiController]
    [Tags("Account")]
    //[Consumes("application/json")]
    [Route("/api/account")]
    public class AccountController : ControllerBase
    {


        /// <summary>
        /// Получить капитальный счет по номеру и/или по пользователю
        /// </summary>
        /// <param name="accountNumber">Номер счета, не обязательный параметр</param>
        /// <param name="login">Имя пользователя, не обязательный параметр</param>
        /// <returns></returns>
        [HttpGet]
        [Route("/api/account/capex")]
        //[Produces("application/json")]
        //[Consumes("application/json")]
        [Authorize]
        public IActionResult GetAllAccountsCapex(string? accountNumber, string? login)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                List<AccountCapex> acc;
                if (!string.IsNullOrWhiteSpace(login))
                {
                    acc = accountManager.GetAccountsCapexUser(login);
                }
                else
                {
                    acc = accountManager.GetAllAccountsCapex();
                }

                var res1 = acc.Where(r => string.IsNullOrEmpty(accountNumber) || r.AccountNumber == accountNumber).ToList();


                if (res1 != null)
                {
                    return new JsonResult(res1);
                }
                else
                {
                    return new BadRequestObjectResult($"Счет или пользователь не найден");
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Получить торговый счет по номеру и/или по пользователю
        /// </summary>
        /// <param name="accountTradeNumber">Торговый номер счета, не обязательный параметр</param>
        /// <param name="accountCapexNumber">Капитальный номер счета, не обязательный параметр</param>
        /// <param name="login">Логин, не обязательный параметр</param>
        /// 
        /// <returns></returns>
        [HttpGet]
        [Route("/api/account/trade")]
        //[Produces("application/json")]
        //[Consumes("application/json")]
        [Authorize]
        public IActionResult GetAccountsTrade(string? accountTradeNumber, string? accountCapexNumber, string? login)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                List<AccountTrade> accTrade;
                if (!string.IsNullOrWhiteSpace(login))
                {
                    accTrade = accountManager.GetAccountsTradeUser(login);
                }
                else
                {
                    accTrade = accountManager.GetAllAccountsTrade();
                }

                var res1 = accTrade.Where(r => (string.IsNullOrEmpty(accountTradeNumber) || r.AccountNumber == accountTradeNumber)
                                                                         && (string.IsNullOrEmpty(accountCapexNumber) || r.AccountCapex == accountCapexNumber)).ToList();


                if (res1 != null)
                {
                    return new JsonResult(res1);
                }
                else
                {
                    return new BadRequestObjectResult($"Счет или пользователь не найден");
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Пополнить капитальный счет
        /// </summary>
        /// <param name="addMoneyRequest">Параметры пополнения счета</param>
        /// <returns></returns>
        [HttpPut]
        [Route("/api/account/deposit")]
        //[Produces("application/json")]
        //[Consumes("application/json")]
        [Authorize]
        public IActionResult AddMoneyToAccount(AddMoneyRequest addMoneyRequest)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                if (addMoneyRequest.summa <=0)
                {
                    return new BadRequestObjectResult($"Внимание. Сумма должна быть больше 0");
                }

                if (string.IsNullOrEmpty(addMoneyRequest.accountNumber))
                {
                    return new BadRequestObjectResult($"Счет не задан");
                }
                var allAccount = accountManager.GetAllAccountsCapex();
                var oneAccount = allAccount.FirstOrDefault(r => r.AccountNumber == addMoneyRequest.accountNumber);



                if (oneAccount != null && oneAccount.AccountNumber == addMoneyRequest.accountNumber)
                {
                    var res2 = accountManager.AddMoneyToAccount(addMoneyRequest.accountNumber, addMoneyRequest.summa, addMoneyRequest.typeDeposit);
                    if (res2.Data)
                    {
                        return new JsonResult(res2);
                    }
                    else
                    {
                        return new BadRequestObjectResult(res2);
                    }
                }
                else
                {
                    return new BadRequestObjectResult($"Счет или пользователь не найден");
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        /// <summary>
        /// Запрос на пополнение счета
        /// </summary>
        public record AddMoneyRequest
        {
            /// <summary>
            /// Номер счета
            /// </summary>
            public required string accountNumber { get; set; }

            /// <summary>
            /// Логин
            /// </summary>
            //public required string login { get; set; }

            /// <summary>
            /// Сумма пополнения
            /// </summary>
            public required decimal summa { get; set; }

            /// <summary>
            /// Тип депозита: личный или компании
            /// </summary>
            public required TypeDepositEnum typeDeposit { get; set; }
        }



        /// <summary>
        /// Вывести средства с капитального счета
        /// </summary>
        /// <param name="accountNumber">Номер счета</param>
        /// <param name="userName">Имя пользователя</param>
        /// <param name="summa">Сумма для вывода</param>
        /// <returns></returns>
        [HttpPut]
        [Route("/api/account/withdraw")]
        //[Produces("application/json")]
        //[Consumes("application/json")]
        [Authorize]
        public IActionResult GetMoneyFromAccount(AddMoneyRequest getMoneyFromAccount)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                if (string.IsNullOrEmpty(getMoneyFromAccount.accountNumber))
                {
                    return new BadRequestObjectResult($"Счет не задан");
                }
                if (getMoneyFromAccount.summa <= 0)
                {
                    return new BadRequestObjectResult($"Сумма должна быть больше 0");
                }
                var res2 = accountManager.GetMoneyFromAccount(getMoneyFromAccount.accountNumber, getMoneyFromAccount.summa, getMoneyFromAccount.typeDeposit);
                if (res2.Data)
                {
                    return new JsonResult(res2);
                }
                else
                {
                    return new BadRequestObjectResult(res2);
                }

            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        public record GetMoneyFromAccountRequest
        {
            public string accountNumber;
            public decimal summa;
        }


        /// <summary>
        /// Добавить новый торговый счет
        /// </summary>
        /// <param name="acc">Новый торговый счет</param>
        /// <returns></returns>
        [HttpPost]
        [Route("newTrade")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [Authorize]
        public IActionResult AddAccountTrade(NewAccountTradeRequest acc)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                if (acc.leverage < 1)
                {
                    return new BadRequestObjectResult("Параметр Leverage должен быть не меньше 1");
                }

                AccountTrade newAccount = new AccountTrade()
                {
                    AccountCapex = acc.accountCapex,
                    AccountNumber = acc.accountNumber,
                    Board = acc.board,
                    Leverage = acc.leverage,
                    CanTrade = true,
                    MaxDayLossPercent = acc.maxDayLossPercent,
                    MaxDayLossPercentAfterClearing = acc.maxDayLossPercent,

                    MaxDayLossAbsolut = acc.maxDayLossAbsolut,
                    MaxDayLossAbsolutAfterClearing = acc.maxDayLossAbsolut,

                    Office = acc.office,
                    OfficePayOut = acc.officePayOut,

                    ReferrerAccountCapex = acc?.referrerAccountCapex??"",
                    ReferrerPayOut = acc?.referrerPayOut??0,

                    Limit = acc.limit,
                    AllowOvernightTransfer = acc.allowOvernightTransfer,
                    CancelPenaltyDrawdown = acc.cancelPenaltyDrawdown,
                    RobotRiskManagerOn = acc.robotRiskManagerOn,
                    CanTradeAfterClearing = true,
                    DateBlockTradeSession = DateTime.UtcNow.AddHours(3),
                };

                var res = accountManager.TryAddAccountTrade(newAccount);

                if (res.Data)
                {
                    var a = accountManager.GetAccountTrade(acc.accountNumber, acc.board);
                    return new JsonResult(a);
                }
                else
                {
                    return new BadRequestObjectResult(res.Message);
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        public record NewAccountTradeRequest
        {

            public required BoardEnum board { get; set; }
            public required string accountNumber { get; set; }
            public required string accountCapex { get; set; }
            public required int leverage { get; set; }
            public decimal maxDayLossPercent { get; set; }

            public decimal maxDayLossAbsolut { get; set; }
            public string office { get; set; }
            public decimal officePayOut { get; set; }
            public string? referrerAccountCapex { get; set; }
            public decimal? referrerPayOut { get; set; }
            public decimal limit { get; set; }
            public bool canTrade { get; set; }
            public bool allowOvernightTransfer { get; set; }
            public bool cancelPenaltyDrawdown { get; set; }
            public bool robotRiskManagerOn { get; set; }


        }

        /// <summary>
        /// Изменить торговый счет
        /// </summary>
        /// <param name="acc">Измененный торговый счет</param>
        /// <returns></returns>
        [HttpPut]
        [Route("updateTrade")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [Authorize]
        public IActionResult UpdateAccountTrade(UpdateAccountTradeRequest acc)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                if (acc == null)
                {
                    return new BadRequestObjectResult("Требуется задать параметры торгового счета");
                }
                AccountTrade? a = accountManager.GetAccountTrade(acc.accountNumber, acc.board);
                if (a == null)
                {
                    return new BadRequestObjectResult($"Торговй счет {acc.accountNumber} на {acc.board} не найден");
                }
                AccountTrade newAccount = new AccountTrade()
                {
                    
                    AccountNumber = acc.accountNumber,
                    Board = acc.board,

                    Leverage = acc.leverage ?? a.Leverage,
                    MaxDayLossPercent = acc.maxDayLossPercent ?? a.MaxDayLossPercent,
                    MaxDayLossAbsolut = acc.maxDayLossAbsolut ?? a.MaxDayLossAbsolut,
                    Office = acc.office ?? a.Office,
                    OfficePayOut = acc.officePayOut ?? a.OfficePayOut,
                    ReferrerAccountCapex = acc.referrerAccountCapex ?? a.ReferrerAccountCapex,
                    ReferrerPayOut = acc.referrerPayOut ?? a.ReferrerPayOut,
                    Limit = acc.limit ?? a.Limit,
                    AllowOvernightTransfer = acc.allowOvernightTransfer ?? a.AllowOvernightTransfer,
                    CancelPenaltyDrawdown = acc.cancelPenaltyDrawdown ?? a.CancelPenaltyDrawdown,
                    RobotRiskManagerOn = acc.robotRiskManagerOn ?? a.RobotRiskManagerOn,
                    CanTradeAfterClearing = acc.canTrade ?? a.CanTradeAfterClearing
                };

                var res = accountManager.TryUpdateAccountTrade(newAccount);

                if (res.Data)
                {
                    var updatedAccount = accountManager.GetAccountTrade(acc.accountNumber, acc.board);
                    return new JsonResult(updatedAccount);
                }
                else
                {
                    return new BadRequestObjectResult(res.Message);
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        public record UpdateAccountTradeRequest
        {
            public required string accountNumber { get; set; }
            public required BoardEnum board { get; set; }
            public int? leverage { get; set; }
            public decimal? maxDayLossPercent { get; set; }
            public decimal? maxDayLossAbsolut { get; set; }
            public string? office { get; set; }
            public decimal? officePayOut { get; set; }
            public string? referrerAccountCapex { get; set; }
            public decimal? referrerPayOut { get; set; }
            public decimal? limit { get; set; }
            public bool? canTrade { get; set; }
            public bool? allowOvernightTransfer { get; set; }
            public bool? cancelPenaltyDrawdown { get; set; }
            public bool? robotRiskManagerOn { get; set; }


        }



        /// <summary>
        /// Добавить новый капитальный  счет
        /// </summary>
        /// <param name="account">Новый капитальный счет</param>
        /// <returns></returns>
        [HttpPost]
        [Route("newCapex")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [Authorize]
        public IActionResult AddAccountCapex(NewAccountCapexRequest account)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                

                AccountCapex newAccount = new ()
                {
                    AccountNumber = account.accountNumber,
                    UserName = account.login,
                    DepositTrader = account.depositTrader,
                    DepositCompany = account.depositCompany,
                    FullDepositCompany = account.depositCompany,
                    FullDepositTrader = account.depositTrader
                };

                var res = accountManager.TryAddAccountCapex(newAccount);

                if (res.Data)
                {
                    var a = accountManager.GetAccountsCapexUser(account.login);
                    return new JsonResult(a);
                }
                else
                {
                    return new BadRequestObjectResult(res.Message);
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }

        public record NewAccountCapexRequest
        {

            public required string accountNumber { get; set; }
            //public required string accountCapex { get; set; }

            public required string login { get; set; }

            public required int depositCompany { get; set; }
            public required int depositTrader { get; set; }

        }



        /// <summary>
        /// Разрешить, запретить пользователю торговлю на одном определенном счете
        /// </summary>
        /// <param name="accountCanTradeRequest">Счет, Разрешение</param>
        /// <returns></returns>
        [HttpPut]
        [Route("setCanTrade")]
        [Produces("application/json")]
        [Consumes("application/json")]
        [Authorize]
        public IActionResult SetAccountCanTrade(AccountCanTradeRequest accountCanTradeRequest)
        {
            try
            {
                if (!JwtProvider.IsValidBearerToken(HttpContext.Request?.Headers.Authorization, users))
                {
                    return new UnauthorizedResult();
                }
                var res = accountManager.SetCanTrade(accountCanTradeRequest.AccountNumber, accountCanTradeRequest.board, accountCanTradeRequest.CanTrade);

                if (res.Data)
                {
                    return new JsonResult(res);
                }
                else
                {
                    return new BadRequestObjectResult(res);
                }
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
        }
        public record AccountCanTradeRequest
        {
            public required string AccountNumber { get; set; }

            public required BoardEnum board { get; set; }

            public required bool CanTrade { get; set; }

        }



       


        /// <summary>
        /// Максимально допустимая просадка за день
        /// </summary>
        public record AccountDayLoss
        {
            /// <summary>
            /// Капитальный счет
            /// </summary>
            public required string AccountCapexNumber { get; set; }

            /// <summary>
            /// Максимальная просадка за день  в процентах
            /// </summary>
            public required decimal maxDayLossPercent { get; set; }

            /// <summary>
            /// Максимальная просадка за день в валюте(рублях)
            /// </summary>
            public required decimal maxDayLossAbsolut { get; set; }

        }


        AccountManager accountManager;

        ServerService serverService;

        ServiceDB serviceDB;

        System.Collections.Concurrent.ConcurrentDictionary<string, LiteInvest.Entity.ServerEntity.User>? users;

        public AccountController(AccountManager _accountManager, ServerService _serverService, ServiceDB _serviceDB)
        {
            accountManager = _accountManager;
            serviceDB = _serviceDB;
            serverService = _serverService;
            users = serviceDB.GetUsersContext();
        }
    }
}
