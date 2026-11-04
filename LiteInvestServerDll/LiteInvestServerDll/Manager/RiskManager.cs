using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.Server2;
using LiteInvest.Entity.ServerEntity;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using LiteInvest.Entity.Helpers;
using System.Numerics;

namespace LiteInvestServerDll
{

    public class RiskManager
    {
        private ServerService serverService;
        private AccountManager accountManager;
        private ConcurrentDictionary<string, User> Users;

        /// <summary>
        /// Финансовый результат по всем трейдерам
        /// key 1 - userLogin, key 2 - BoardEnum
        /// </summary>
        private ConcurrentDictionary<string, ConcurrentDictionary<BoardEnum, FinanceResult>> FinResultsUsers = new();
        
        private ConcurrentDictionary<string, (string userName,  Action<List<FinanceResult>> Action)> FinanceResultEventsForUsers;

        public RiskManager(ServerService _serverService, ConcurrentDictionary<string, User> usersContext)
        {
            serverService = _serverService;
            accountManager = serverService.accountManager;
            Users = usersContext;
            Helper.CreateTimerAndStart(CalculateFinResultAndRobot, 5000, true);
        }

        /// <summary>
        /// Get Finance Result for user on one board
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        public FinanceResult GetFinanceResult(string userName, BoardEnum board)
        {
            if (FinResultsUsers.TryGetValue(userName, out ConcurrentDictionary<BoardEnum, FinanceResult>? resBoards))
            {
                if (!resBoards.TryGetValue(board, out FinanceResult? result))
                {
                    return result;
                }
            }
            return new FinanceResult() { AccountCapex="Not set", Board = board, UserName = userName };
        }

        /// <summary>
        /// Get Finance Result for user on all board
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public List<FinanceResult> GetFinanceResult(string userName)
        {
            if (FinResultsUsers.TryGetValue(userName, out ConcurrentDictionary<BoardEnum, FinanceResult>? resBoards))
            {
                return resBoards.Values.ToList();
            }
            return new List<FinanceResult>();
        }

        /// <summary>
        /// Получить финансовй результат по всем счетам и пользователям
        /// </summary>
        /// <returns></returns>
        public List<FinanceResult> GetFinanceResult()
        {
            var res = new List<FinanceResult>();
            var f = FinResultsUsers.Values.ToList();

            f.Select(f => { res.AddRange(f.Values.ToList()); return true; }).Count();

            return res;

        }

        /// <summary>
        /// Calculate finance result
        /// </summary>
        private void CalculateFinResultAndRobot()
        {
            try
            {
                if (Users == null)
                {
                    return;
                }
                foreach (var item in Users)
                {
                    User user = item.Value;
                    if (user == null || string.IsNullOrWhiteSpace(user.Login))
                    {
                        continue;
                    }
                    string userName = user.Login;

                    CalculateUnrelizedPnLPositions(userName);

                    if (!FinResultsUsers.ContainsKey(userName))
                    {
                        FinResultsUsers.TryAdd(userName, new ConcurrentDictionary<BoardEnum, FinanceResult>());
                    }

                    BoardEnum board = BoardEnum.FortsMoex;
                    if (!FinResultsUsers[userName].TryGetValue(board, out var finrez))
                    {
                        finrez = new FinanceResult();
                        FinResultsUsers[userName].TryAdd(board, finrez);
                    }

                    finrez.Board = board;
                    finrez.UserName = userName;

                    AccountCapex? accCapex = accountManager?.GetAccountsCapexUser(userName).FirstOrDefault();
                    AccountTrade? accTrade = accountManager?.GetAccountsTradeUser(userName).Where(a => a.Board == board).FirstOrDefault();
                    if (accCapex != null)
                    {
                        finrez.AccountCapex = accCapex.AccountNumber;

                        if (accTrade != null)
                        {
                            finrez.AccountTrade = accTrade.AccountNumber;

                            decimal drawDown = GetDrawDownUser(userName, board);
                            finrez.DrawDown = drawDown;

                            decimal blockMoney = GetLoadDepositMoney(user, board);
                            var leverage = Math.Clamp(accTrade.Leverage, 1, int.MaxValue);      // если плечо меньше 1 или 0 это ошибка

                            finrez.DepositMoney = (accCapex.DepositCompany + accCapex.DepositTrader) * leverage - blockMoney;

                            var MarginAndCommission = PositionManager.GetSummaMarginAndCommission(userName, board);
                            finrez.Commission = MarginAndCommission.Commission;
                            finrez.Margin = MarginAndCommission.Margin;
                        }
                        else
                        {
                            finrez.DrawDown = 0;
                            finrez.DepositMoney = 0;
                            finrez.Commission = 0;
                            finrez.Margin = 0;
                        }
                    }
                    else
                    {
                        finrez.AccountCapex = "Not set";
                    }

                    Task.Run(() => RotobCloseMarginCall(user, finrez, accCapex, accTrade));

                    // Console.WriteLine($"{user.Login} {finrez}");
                }
            }
            catch (Exception ex)
            {
                ServerLogger.Log(ex.Message + ex.StackTrace);
            }

            try
            {
                SendFinanceResultToSubscripting();
            }
            catch(Exception ex)
            { 
                ServerLogger.Log(ex.Message);
            }
        }

        /// <summary>
        /// Робот закрытия позиций по просадкам
        /// </summary>
        /// <param name="user"></param>
        /// <param name="finrez"></param>
        /// <param name="accCapex"></param>
        /// <param name="accTrade"></param>
        private void RotobCloseMarginCall(User user, FinanceResult finrez, AccountCapex? accCapex, AccountTrade? accTrade)
        {
            try
            {
                if (!(accTrade?.RobotRiskManagerOn ?? false)) // если робот выключен ;-)
                {
                    return;
                }
                if (accCapex == null)   // если капитальный счет не найден... черт возьми
                {
                    return;
                }
                if (accTrade.DateBlockTradeSession.Date == Helper.DateTimeMSK.Date && accTrade.TypeBlockTradeSession == TypeBlockTradeSessionEnum.Robot)   // попытка закрыть позиции делается один раз
                {
                    return;
                }
                
                if (finrez.SummaFinRes < finrez.DrawDown * -1)
                {
                    serverService?.CloseAllPosition(user.Login);
                    serverService?.OnAttentionMessageAsync(AttentionMessageType.Info, user.Login, user.Instruments.FirstOrDefault().Value.FirstOrDefault().Value.Id, "Недопустимая просадка, робот закрывает все позиции и запрещает торговлю");

                    accountManager.BlockTradeToday(accTrade.AccountNumber, accTrade.Board??finrez.Board, TypeBlockTradeSessionEnum.Robot);
                }
            }
            catch (Exception ex)
            {
                ServerLogger.Log(ex.Message);
            }
        }

        private void CalculateUnrelizedPnLPositions(string userName)
        {
            //  Считаем не реализованный фин.рез.
            var openPos = serverService.GetOpenPositionsForUser(userName);
            for (int i = 0; i < (openPos?.Count??0); i++) 
            {
                Pos pos = openPos[i];
                string secids = pos.SecId;
                var bestPrice = serverService.GetBestBidAskPrice(secids);
                decimal price = pos.Side == Side.Buy ?  bestPrice.BestBid : bestPrice.BestAsk;
                if (price != 0)
                {
                    PositionManager.CalculateUnrealizedPnl(pos, price);
                }
            }
        }

        /// <summary>
        /// Permissible trader drawdown
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        private decimal GetDrawDownUser(string userName, BoardEnum board)
        {
            decimal drawDown = 0;
            AccountCapex? accCapex = accountManager.GetAccountsCapexUser(userName)?.First();
            AccountTrade? accTrade = accountManager.GetAccountsTradeUser(userName).Where(a => a.Board == board).FirstOrDefault();
            if (accCapex != null && accTrade != null)
            {
                drawDown = Math.Min(accTrade.MaxDayLossAbsolut, (accCapex.DepositTrader + accCapex.DepositCompany) * accTrade.MaxDayLossPercent / 100);
            }
            return drawDown;
        }

        /// <summary>
        /// Can the user place an order?
        /// </summary>
        /// <param name="newOrder"></param>
        /// <param name="user"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        internal ServerResponce<bool> CanUserPlaceOrder(Order newOrder, User user, BoardEnum board)
        {
            AccountTrade? accTrade = accountManager.GetAccountsTradeUser(user.Login).FirstOrDefault(a => a.Board == board);
            if (accTrade == null || !accTrade.CanTrade)
            {
                return new ServerResponce<bool>(false, $"Торговля {user.Login} на площадке {board} запрещена или нет торгового счета трейдера");
            }
            if (accTrade.DateBlockTradeSession.Date == Helper.DateTimeMSK.Date && accTrade.TypeBlockTradeSession == TypeBlockTradeSessionEnum.Robot)
            {
                return new ServerResponce<bool>(false, $"Сегодня торговля {user.Login} на площадке {board} запрещена роботом из-за просадки");
            }
            if (accTrade.DateBlockTradeSession.Date == Helper.DateTimeMSK.Date && accTrade.TypeBlockTradeSession != TypeBlockTradeSessionEnum.Robot)
            {
                return new ServerResponce<bool>(false, $"Сегодня торговля {user.Login} на площадке {board} запрещена. Завтра запрет будет снят");
            }

            AccountCapex? accCapex = accountManager.GetAccountsCapexUser(user.Login)?.First();
            if (accCapex == null)
            {
                return new ServerResponce<bool>(false, $"Для трейдера {user.Login} не найден единый капитальный счет");
            }

            List<Pos>? openPosition = serverService.GetOpenPositionsForUser(user.Login);
            if (openPosition.Exists(p => p.SecId == newOrder.SecurityId && p.CurrentPos >= newOrder.Volume && p.Side != newOrder.Side))    // close exists open position
            {
                return new ServerResponce<bool>(true, "OK");
            }

            decimal needMoney = GetLoadDepositMoney(user, board, newOrder);
            var leverage = Math.Clamp(accTrade.Leverage, 1, int.MaxValue);
            decimal maxDeposit = (accCapex.DepositTrader + accCapex.DepositCompany) * leverage;
            decimal limit = accTrade.Limit;

            if (needMoney > maxDeposit || needMoney > limit)
            {
                return new ServerResponce<bool>(false, $"Не достаточно средств ({maxDeposit:#######0.##}) на счете, либо превышен лимит ({accTrade.Limit}) для выставления ордера");
            }


            return new ServerResponce<bool>(true, "OK");
        }

        /// <summary>
        /// Get the amount of the loaded deposit
        /// </summary>
        /// <param name="user"></param>
        /// <param name="board"></param>
        /// <param name="newOrder"></param>
        /// <returns></returns>
        public decimal GetLoadDepositMoney(User user, BoardEnum board, Order? newOrder = null)
        {
            decimal summaBlockMoney = 0;
            List<Order>? activeOrders = serverService.GetOrdersForUser(user.Login).Data;
            if (activeOrders != null)
                activeOrders = activeOrders.Where(o => o.State == Order.OrderStateType.Active).ToList();

            if (activeOrders != null && newOrder != null)
            {
                activeOrders.Add(newOrder);
            }
            List<Pos>? openPosition = serverService.GetOpenPositionsForUser(user.Login);
            Dictionary<string, decimal> instrumentLots = new Dictionary<string, decimal>();

            for (int i = 0; i < (openPosition?.Count??0); i++)
            {
                Pos? pos = openPosition![i];
                if (pos == null)
                {
                    continue;
                }
                var sec = serverService.GetSecurity(pos.SecId);

                if (!instrumentLots.ContainsKey(pos.SecId))
                {
                    instrumentLots.Add(pos.SecId, 0);
                }
                summaBlockMoney = summaBlockMoney + pos.CurrentPos * pos.AverageEntry / (sec?.PriceStep ?? 1) * (sec?.PriceStepCost ?? 1);

                if (pos.Side == Side.Buy)
                {
                    instrumentLots[pos.SecId] = instrumentLots[pos.SecId] + pos.CurrentPos;
                }
                else
                {
                    instrumentLots[pos.SecId] = instrumentLots[pos.SecId] - pos.CurrentPos;
                }
            }

            for (int i = 0; i < (activeOrders?.Count??0); i++)
            {
                Order order = activeOrders![i];
                var sec = serverService.GetSecurity(order.SecurityId);
                decimal priceOrder = order.PriceOrder;
                if (priceOrder == 0)
                {
                    var p = serverService.GetBestBidAskPrice(order.SecurityId);
                    priceOrder = p.BestBid;
                }
                if (instrumentLots.ContainsKey(order.SecurityId))
                {
                    decimal oldLots = instrumentLots[order.SecurityId];
                    instrumentLots[order.SecurityId] = instrumentLots[order.SecurityId] + order.Volume * (order.Side == Side.Buy ? 1 : -1);
                    if (Math.Abs(oldLots) < Math.Abs(instrumentLots[order.SecurityId]))
                    {
                        summaBlockMoney = summaBlockMoney + priceOrder * (Math.Abs(instrumentLots[order.SecurityId]) - Math.Abs(oldLots)) / (sec?.PriceStep ?? 1) * (sec?.PriceStepCost ?? 1);
                    }
                }
                else
                {
                    summaBlockMoney = summaBlockMoney + priceOrder * order.Volume / (sec?.PriceStep ?? 1) * (sec?.PriceStepCost ?? 1);
                }
            }

            return summaBlockMoney;
        }

        /// <summary>
        /// Subscribe For Finance Result from Front
        /// </summary>
        /// <param name="username"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public bool SubscribeForFinanceResult(string username, Action<List<FinanceResult>> action)
        {
            string guid = Guid.NewGuid().ToString();
            if (FinanceResultEventsForUsers == null)
            {
                FinanceResultEventsForUsers = new ConcurrentDictionary<string, (string userName, Action<List<FinanceResult>> Action)>();
            }
            var res = FinanceResultEventsForUsers.TryAdd(guid, (username, action));
            return res;
        }

        /// <summary>
        /// Remove subscribe For Finance Result from Front
        /// </summary>
        /// <param name="username"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool UnSubscribeForFinanceResult(string username, string id)
        {
            bool res =  FinanceResultEventsForUsers.TryRemove(id, out var _);
            return res;
        }

        private void SendFinanceResultToSubscripting()
        {
            if (FinanceResultEventsForUsers == null)
            {
                return;
            }
            foreach (var actionEvent in FinanceResultEventsForUsers)
            {
                bool r = FinResultsUsers.TryGetValue(actionEvent.Value.userName, out var result);
                if (r && result != null)
                {
                    actionEvent.Value.Action?.Invoke(result.Values.ToList());
                }
            }
        }

       
    }


}
