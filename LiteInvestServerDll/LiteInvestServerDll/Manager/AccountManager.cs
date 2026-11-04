using LiteInvest.Entity.Helpers;
using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ReportEntity;
using LiteInvest.Entity.Server2;
using LiteInvest.Entity.ServerEntity;

using Microsoft.EntityFrameworkCore;

using PlazaEngine.Engine;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvestServerDll
{
    public class AccountManager : NotifyPropertyBase
    {

        #region Constructor

        // Для трейдеров у кого депозит более 10 000 рублей перенос допустим не более, чем на 5 плечо и не более, чем на 5 млн сверх депозита.
        private const decimal penaltySummaPosition = 5 * 1000000m;    
        private const decimal penaltySummaPositionLeverage = 5m;
        private const decimal penaltyDepositAllowTransferOverClearing = 1000000;

        private ServerService serverService;

        private ServiceDB serviceDB;

        private IDbContextFactory<AppDBContext> dbContextFactory;

        private ReportManager reportManager;

        /// <summary>
        /// Dependence Injection
        /// </summary>
        /// <param name="_serverService"></param>
        /// <param name="_serviceDB"></param>
        public void Open(ServerService _serverService, ServiceDB _serviceDB, IDbContextFactory<AppDBContext> _dbContextFactory)
        {
            serverService = _serverService;
            serviceDB = _serviceDB;

            accountsTrade = serviceDB.GetAccountsTrade();
            accountsCapex = serviceDB.GetAccountsCapex();

            dbContextFactory = _dbContextFactory;


            clearingList = serviceDB.GetClearingList();

            serviceDB.AccountsTradeEventHandler(this);
            serviceDB.ClearingEventHandler(this);

            Helper.CreateTimerAndStart(Clearing, 30000, true);

            reportManager = new ReportManager(_serverService);
        }




        /// <summary>
        /// Создать сервис
        /// </summary>
        public AccountManager()
        {
            
        }

        private List<AccountTrade> accountsTrade { get; set; }

        private List<AccountTrade> AccountsTrade
        {
            get => accountsTrade;

            set { }
        }

        private List<AccountCapex> accountsCapex { get; set; }

        private List<AccountCapex> AccountsCapex
        {
            get => accountsCapex;

            set { }
        }

        private ConcurrentDictionary<DateOnly, List<ClearingResult>> clearingList;
        private ConcurrentDictionary<DateOnly, List<ClearingResult>> ClearingList
        {
            get => clearingList;

        }

        #endregion Constructor

        #region Управление счетами из АПИ

        /// <summary>
        /// Попробовать добавить торговый  счет для одной площадки
        /// </summary>
        /// <param name="account"></param>
        /// <returns>False - Ошибка при добавлении</returns>
        public ServerResponce<bool> TryAddAccountTrade(AccountTrade account)
        {
            if (AccountsTrade.Exists(a => a.AccountNumber == account.AccountNumber && a.Board == account.Board))
            {
                return new ServerResponce<bool>(false, $"Внимание. Номер счета {account.AccountNumber}  уже существует на {account.Board}.");
            }
            
            if (string.IsNullOrWhiteSpace(account.AccountCapex)
                   ||!AccountsCapex.Exists(a => a.AccountNumber == account.AccountCapex))   // 
            {
                return new ServerResponce<bool>(false, $"Внимание. Заданный капитальный счет {account.AccountCapex} не обнаружен.");
            }

            _=TransactionManager.AddTransaction(new Transaction()
            {
                AccountName = account.AccountNumber,
                Description = $"TryAddAccountTrade",
                Module = "AccountManager",
                ReasonChange = ReasonChangeEnum.RestAPI,
                UserName = "",
                ValueBegin = 0,
                ValueChange = 0,
                ValueEnd = 0
            });
            
            AccountsTrade.Add(account);
            
            DBManager.TryAddObjectToDB(account);

            OnPropertyChanged();

            return new ServerResponce<bool>(true, "Added");
        }

        /// <summary>
        /// Добавить капитальный счет
        /// </summary>
        /// <param name="newAccount">Капитальный счет</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ServerResponce<bool> TryAddAccountCapex(AccountCapex newAccount)
        {
            if (AccountsCapex.Exists(a => a.AccountNumber == newAccount.AccountNumber))
            {
                return new ServerResponce<bool>(false, $"Внимание. Номер счета {newAccount.AccountNumber}  уже существует в базе данных");
            }
            _ = TransactionManager.AddTransaction(new Transaction()
            {
                AccountName = newAccount.AccountNumber,
                Description = $"TryAddAccountCapex",
                Module = "AccountManager",
                ReasonChange = ReasonChangeEnum.RestAPI,
                UserName = newAccount.UserName,
                ValueBegin = 0,
                ValueChange = 0,
                ValueEnd = newAccount.DepositCompany + newAccount.DepositTrader
            });
            AccountsCapex.Add(newAccount);
            OnPropertyChanged();

            //AccountCapex? acc = AccountsCapex.FirstOrDefault(a => a.AccountNumber == newAccount.AccountNumber);

            DBManager.TryAddObjectToDB(newAccount);

            ReportCapexHistoryTransaction historyTransaction = new ReportCapexHistoryTransaction()
            {
                AccountCapex = newAccount.Guid,
                TransactionDate = Helper.DateTimeMSK,
                TransactionDescription = "Пополнение счета",
                TransactionSumma = newAccount.DepositTrader+ newAccount.DepositCompany
            };
            DBManager.TryAddObjectToDB(historyTransaction);

            return new ServerResponce<bool>(true, "Added");
        }

        /// <summary>
        /// установить разрешение торговли
        /// </summary>
        /// <param name="accountNumber">Номер счета</param>
        /// <param name="canTrade">True - Разрешить торговать</param>
        /// <returns>False - Ошибка при добавлении</returns>
        public ServerResponce<bool> SetCanTrade(string accountNumber, BoardEnum board, bool canTrade)
        {
            var account = AccountsTrade.Find(a => a.AccountNumber == accountNumber && a.Board==board);
            if (account == null)
            {
                return new ServerResponce<bool>(false, $"Внимание. Счет {accountNumber} не найден");
            }
            _ = TransactionManager.AddTransaction(new Transaction()
            {
                AccountName = accountNumber,
                Description = $"TryAddAccountCapex",
                Module = "SetCanTrade",
                ReasonChange = ReasonChangeEnum.RestAPI,
                UserName = "",
                ValueBegin = 0,
                ValueChange = 0,
                ValueEnd = 0
            });

            account.CanTrade = canTrade;
            account.CanTradeAfterClearing = canTrade;

            if (canTrade && account.DateBlockTradeSession.Date == Helper.DateTimeMSK.Date && account.TypeBlockTradeSession != TypeBlockTradeSessionEnum.Robot)
            {
                account.DateBlockTradeSession = DateTime.MinValue;
            }

            OnPropertyChanged();
            return new ServerResponce<bool>(true, "OK");
        }

        public ServerResponce<bool> BlockTradeToday(string accountNumber, BoardEnum board, TypeBlockTradeSessionEnum typeBlock)
        {
            var account = AccountsTrade.Find(a => a.AccountNumber == accountNumber && a.Board == board);
            if (account == null)
            {
                return new ServerResponce<bool>(false, $"Внимание. Счет {accountNumber} не найден");
            }
            account.DateBlockTradeSession = Helper.DateTimeMSK;
            account.TypeBlockTradeSession = typeBlock;

            OnPropertyChanged();
            return new ServerResponce<bool>(true, "OK");
        }

        /// <summary>
        /// Установить новый баланс
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="accountNumber"></param>
        /// <returns></returns>
        public ServerResponce<bool> SetDeposit(string accountCapex, TypeDepositEnum typeDeposit, decimal newBalance)
        {
            var acc = AccountsCapex.Find(a => a.AccountNumber == accountCapex);
            if (acc == null)
            {
                return new ServerResponce<bool>(false, $"Внимание. Счет {accountCapex} не найден");
            }
            lock (accountChangeLocker)
            {
                
                   var trans = new Transaction()
                   {
                       AccountName = acc.AccountNumber,
                       Description = $"SetDeposit",
                       Module = "AccountManager",
                       ReasonChange = ReasonChangeEnum.RestAPI,
                       UserName = acc.UserName,
                       ValueBegin = acc.DepositTrader,
                       ValueChange = newBalance,
                       ValueEnd = newBalance,
                   };
                if (typeDeposit == TypeDepositEnum.DepositCompany)
                {
                    trans.ValueBegin = acc.DepositCompany;
                    trans.PropertyName = "DepositCompany";

                    acc.DepositCompany = newBalance;

                    trans.ValueEnd = acc.DepositCompany;
                }
                else if (typeDeposit == TypeDepositEnum.DepositTrader)
                {
                    trans.ValueBegin = acc.DepositTrader;
                    trans.PropertyName = "DepositTrader";

                    acc.DepositTrader = newBalance;
                    
                    trans.ValueEnd = acc.DepositTrader;
                }
                _=TransactionManager.AddTransaction(trans);
            }
            OnPropertyChanged();
            return new ServerResponce<bool>(true, "OK");
        }

        /// <summary>
        /// Торговй Счет 
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public AccountTrade GetAccountTrade(string accountNumber, BoardEnum board)
        {
            AccountTrade? res = AccountsTrade.FirstOrDefault(a => a.AccountNumber == accountNumber && a.Board == board);

            return (AccountTrade)res?.Clone();
        }

        /// <summary>
        /// Капитальный счет пользователя
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public List<AccountCapex>? GetAccountsCapexUser(string userName)
        {
            IEnumerable<AccountCapex>? res = AccountsCapex.Where(a => a.UserName == userName);//.Clone();
            return (List<AccountCapex>?)res?.Clone();
        }

        /// <summary>
        /// Все капитальные счета 
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public List<AccountCapex>? GetAllAccountsCapex()
        {
            List<AccountCapex> res = (List<AccountCapex>)AccountsCapex.Clone();
            return res;
        }

        /// <summary>
        /// Торговые счета пользователя
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public List<AccountTrade> GetAccountsTradeUser(string userName)
        {
            IEnumerable<AccountCapex>? accCapex = AccountsCapex.Where(a => a.UserName == userName);
            if ((accCapex?.Count()??0) == 0)
            {
                return new List<AccountTrade>();
            }
            List<AccountTrade> res = new List<AccountTrade>();
            foreach (var acc in accCapex)
            {
                res.AddRange(AccountsTrade.Where(a => a.AccountCapex == acc.AccountNumber));
            }
            return (List<AccountTrade>)res.Clone();
        }


        /// <summary>
        /// Список  всех торговых счетов (копии)
        /// </summary>
        /// <returns></returns>
        public List<AccountTrade> GetAllAccountsTrade()
        {
            return (List<AccountTrade>)AccountsTrade.Clone();
        }


        object accountChangeLocker = new object();

       


        /// <summary>
        /// Вывести средства с торгового счета
        /// </summary>
        /// <param name="accountCapexNumber"></param>
        public ServerResponce<bool> GetMoneyFromAccount(string accountCapexNumber, decimal summa, TypeDepositEnum typeDeposit)
        {
            AccountCapex? acc = AccountsCapex.FirstOrDefault(a => a.AccountNumber == accountCapexNumber);
            if (acc == null)
            {
                return new ServerResponce<bool>(false, $"Ошибка. Пополнить можно капитальный счет трейдера. Баланс не изменен.");
            }
            if (typeDeposit == TypeDepositEnum.DepositTrader && (acc.DepositTrader  < PlazaOptions.MyGetMoneyMin || acc.DepositTrader < summa))
            {
                return new ServerResponce<bool>(false, "Не достаточно средств на балансе для вывода.");
            }
            if (typeDeposit == TypeDepositEnum.DepositCompany && acc.DepositCompany < summa)
            {
                return new ServerResponce<bool>(false, "Не достаточно средств на балансе для вывода.");
            }
            lock (accountChangeLocker)
            {
                if (typeDeposit == TypeDepositEnum.DepositCompany)
                {
                    decimal newBalance = acc.DepositCompany - summa;
                    _ = TransactionManager.AddTransaction(
                       new Transaction()
                       {
                           AccountName = acc.AccountNumber,
                           Description = $"GetMoneyFromAccount",
                           Module = "AccountManager",
                           PropertyName = "DepositCompany",
                           ReasonChange = ReasonChangeEnum.RestAPI,
                           UserName = acc.UserName,
                           ValueBegin = acc.DepositCompany,
                           ValueChange = summa,
                           ValueEnd = newBalance,
                       });
                    acc.DepositCompany = newBalance;
                    acc.FullDepositCompany = acc.FullDepositCompany - summa;
                }
                else if(typeDeposit == TypeDepositEnum.DepositTrader)
                {

                    decimal newBalance = acc.DepositTrader - summa;

                    _ = TransactionManager.AddTransaction(
                       new Transaction()
                       {
                           AccountName = acc.AccountNumber,
                           Description = $"GetMoneyFromAccount",
                           Module = "AccountManager",
                           PropertyName = "DepositTrader",
                           ReasonChange = ReasonChangeEnum.RestAPI,
                           UserName = acc.UserName,
                           ValueBegin = acc.DepositTrader,
                           ValueChange = summa,
                           ValueEnd = newBalance,
                       });
                    acc.DepositTrader = newBalance;
                    acc.FullDepositTrader = acc.FullDepositTrader - summa;
                }
            }
            
            DBManager.AddOrUpdateObjectToDB<AccountCapex>(acc);

            DBManager.TryAddObjectToDB(new ReportCapexHistoryTransaction()
            {
                AccountCapex = acc.Guid,
                TransactionDate = Helper.DateTimeMSK,
                TransactionDescription = "Вывод средств со счета",
                TransactionSumma = summa
            });

            OnPropertyChanged();
            return new ServerResponce<bool>(true, $"Вывели с баланса средства {summa}");
        }

        /// <summary>
        /// Пополнить счета
        /// </summary>
        /// <param name="accountCapexNumber"></param>
        /// <param name="userName"></param>
        /// <param name="summa"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ServerResponce<bool> AddMoneyToAccount(string accountCapexNumber, decimal summa, TypeDepositEnum typeDeposit)
        {
            AccountCapex? acc = AccountsCapex.FirstOrDefault(a => a.AccountNumber == accountCapexNumber);
            if (acc == null)
            {
                return new ServerResponce<bool>(false, $"Ошибка. Пополнить можно капитальный счет. Сумма не зачисленна.");
            }
            lock (accountChangeLocker)
            {
                var trans=new Transaction()
                  {
                      AccountName = acc.AccountNumber,
                      Description = $"AddMoneyToAccount",
                      Module = "AccountManager",
                      PropertyName = "Deposit",
                      ReasonChange = ReasonChangeEnum.RestAPI,
                      UserName = acc.UserName,
                      ValueBegin = 0,
                      ValueChange = summa,
                      ValueEnd = 0,
                  };
                if (typeDeposit == TypeDepositEnum.DepositCompany )
                {
                    trans.PropertyName = "DepositCompany";
                    trans.ValueBegin = acc.DepositCompany;
                    acc.DepositCompany = acc.DepositCompany + summa;
                    acc.FullDepositCompany = acc.FullDepositCompany + summa;
                    trans.ValueEnd = acc.DepositCompany;
                }
                else if (typeDeposit == TypeDepositEnum.DepositTrader)
                {
                    trans.PropertyName = "DepositTrader";
                    trans.ValueBegin = acc.DepositTrader;
                    acc.DepositTrader = acc.DepositTrader + summa;
                    acc.FullDepositTrader = acc.FullDepositTrader + summa;
                    trans.ValueEnd = acc.DepositTrader;
                }
                _ = TransactionManager.AddTransaction(trans);
            }
            OnPropertyChanged();

            DBManager.AddOrUpdateObjectToDB(acc);

            DBManager.TryAddObjectToDB(new ReportCapexHistoryTransaction()
            {
                AccountCapex = acc.Guid,
                TransactionDate = Helper.DateTimeMSK,
                TransactionDescription = "Пополнение счета",
                TransactionSumma = summa
            });

            return new ServerResponce<bool>(true, $"Зачислили на баланс {typeDeposit} средства {summa}");

        }

        public ServerResponce<bool> TryUpdateAccountTrade(AccountTrade newAccount)
        {
            AccountTrade? acc = AccountsTrade.FirstOrDefault(a => a.AccountNumber == newAccount.AccountNumber && a.Board == newAccount.Board);
            if (acc == null)
            {
                return new ServerResponce<bool>(false, $"Внимание. Счет {newAccount.AccountNumber} не найден");
            }
            lock (accountChangeLocker)
            {
                acc.Leverage = Math.Clamp(newAccount.Leverage, 1, int.MaxValue);
                acc.MaxDayLossAbsolutAfterClearing = newAccount.MaxDayLossAbsolut;
                acc.MaxDayLossPercentAfterClearing = newAccount.MaxDayLossPercent;
                acc.Office = newAccount.Office;
                acc.OfficePayOut = newAccount.OfficePayOut;
                acc.ReferrerPayOut = newAccount.ReferrerPayOut;
                acc.ReferrerAccountCapex = newAccount.ReferrerAccountCapex;
                acc.Limit = newAccount.Limit;
                acc.CanTradeAfterClearing = newAccount.CanTradeAfterClearing;
                acc.AllowOvernightTransfer = newAccount.AllowOvernightTransfer;
                acc.CancelPenaltyDrawdown = newAccount.CancelPenaltyDrawdown;
                acc.RobotRiskManagerOn = newAccount.RobotRiskManagerOn;
            }
            OnPropertyChanged();
            return new ServerResponce<bool>(true, "Updated");
        }

        #endregion Управление счетами из АПИ

        #region Клиринг

        private Dictionary<BoardEnum, DateTime> TimeLastClearingBoard = new();

        /// <summary>
        /// Проводим Клиринг,   во время биржевого клиринга и ночью
        /// </summary>
        private void Clearing()
        {
            /*ToDo Задачи на клиринг
                     * 1. Высчитать из PnL закрытых позиций комиссии и пэйаут, а остаток зачислить на депозит трейдеру. Позиции отметить как прошедшие клиринг.
                     * 1.1. Если депозит трейдера = 0, то списывать убыток с депозита компании.
                     * 2. Зачислить комисии на отдельное свойство торгового счета
                     * 3. Зачислить Пэйаут на отдельное свойство торгового счета
                     * 3.1. Начислить штрафы за перенос позиции, за превышение просадки
                     * 4. Проинформировать Лайт о удержанных комиссиях и пэйауте ??????????? Как?????
                     * 5. Зачислить пополнения/переводы на депозиты,
                     * 6. Применить новые просадки после клиринга
                     * 7. Сбросить запрет торговли 
                     * 
                     * 
                    */

            //     return; 

            if (PlazaOptions.Simulations || !serverService.GetConnectorAllDataIsReady(BoardEnum.FortsMoex)) //ToDo  Test it
            {
                return;
            }

            bool needSave = false;

            foreach (BoardEnum board in Enum.GetValues(typeof(BoardEnum)))
            {
                if (PlazaOptions.TimeClearing.TryGetValue(board, out var timeClearing))
                {
                    if (!TimeLastClearingBoard.TryGetValue(board, out DateTime timeLastClearing))
                    {
                        timeLastClearing = DateTime.MinValue;
                        TimeLastClearingBoard.Add(board, timeLastClearing);
                    }
                    if (Helper.DateTimeMSK.Date > timeLastClearing.Date // сегодня на площадке клиринг еще не делали и время пора сделать
                            && Helper.DateTimeMSK.TimeOfDay.Subtract(timeClearing).TotalMinutes >= 0 // и сейчас время стало больше или равно, чем время клиринга
                            && Helper.DateTimeMSK.TimeOfDay.Subtract(timeClearing).TotalMinutes < 15
                            )
                    {

                        // очистить раздел report базы данных от сегодняшних клиринговых данных
                        DeleteFromReportClearingDataBoardTodayFromDB(board);

                        //Клиринг открытых позиций и штраф за перенос открытых позиций
                        needSave = ClearingPenaltyOpenPosition(needSave, board);

                        //Клиринг закрытых позиций
                        needSave = ClearingClosePosition(needSave, board);

                        //Клиринг капитальных счетов
                        needSave = ClearingCapexAccount(needSave, board);

                        //Клиринг торговых счетов
                        needSave = ClearingTradeAccount(needSave, board);

                        TimeLastClearingBoard[board] = Helper.DateTimeMSK;

                        StartReportCreate(board);

                    }
                }
            }

            if (needSave)
            {
                //OnPropertyChanged("Clearing");
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Удаляю из базы данных сведения о сегодняшнем клиринге по указанной площадке. Если по одной площадке делаю клиринг несколько раз, чтоб данные не задваивались
        /// </summary>
        /// <param name="board"></param>
        private void DeleteFromReportClearingDataBoardTodayFromDB(BoardEnum board)
        {
            try
            {
                DBManager.GetDBContext(db =>
                {
                    db.ReportFinRezBoard.RemoveRange(db.ReportFinRezBoard.Where(r =>
                                r.Board == board
                                && r.DateReport.Date == Helper.DateTimeMSK.Date

                    ));
                });
                DBManager.GetDBContext(db =>
                {
                    db.ReportFinRezInstrument.RemoveRange(db.ReportFinRezInstrument.Where(r =>
                                r.Board == board
                                && r.DateReport.Date == Helper.DateTimeMSK.Date

                    ));
                });

                DBManager.GetDBContext(db =>
                {
                    db.ReportOrderDay.RemoveRange(db.ReportOrderDay.Where(r =>
                                r.Board == board
                                && r.DateReport.Date == Helper.DateTimeMSK.Date

                    ));
                });

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                ServerLogger.Log(ex.Message + ex.StackTrace);
            }
        }

        private void StartReportCreate(BoardEnum board)
        {
            reportManager.CreateReportBoard(board);
        }

        /// <summary>
        /// Клиринг открытых позиций и штрафов за перенос
        /// </summary>
        /// <param name="needSave"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        private bool ClearingPenaltyOpenPosition(bool needSave, BoardEnum board)
        {
            //ключ 1 - юзер
            //ключ 2 - sec ID
            ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>>?  allOpenPosition = serverService.GetAllOpenPositions();

            //ключ 1 - юзер
            //ключ 2 - sec ID
            ConcurrentDictionary<string, ConcurrentDictionary<string, List<Pos>>>? allClosePosition = serverService.GetAllClosePositions()!;

           
            if ((allOpenPosition?.Count??0) > 0)
            {
                foreach (var positionsUser in allOpenPosition!)
                {
                    string userLogin = positionsUser.Key;
                    AccountCapex? accCapex = AccountsCapex.FirstOrDefault(a => a.UserName == userLogin);
                    if (accCapex == null)
                    {
                        ServerLogger.Log($"Во время клиринга открытых позиций не найден капитальный счет для {userLogin} на площадке {board}");
                        continue;
                    }

                    AccountTrade? accTrade = AccountsTrade.FirstOrDefault(a => a.AccountCapex == accCapex?.AccountNumber && a.Board == board);
                    if (accTrade == null)
                    {
                        ServerLogger.Log($"Во время клиринга открытых позиций не найден торговый счет для {userLogin} на площадке {board}");
                        continue;
                    }

                    decimal deposit = accCapex.DepositCompany + accCapex.DepositTrader;
                    
                    decimal summaPosition = 0;  // Для трейдеров у кого депозит более 10 000 рублей перенос допустим не более, чем на 5 плечо и не более, чем на 5
                                                // млн сверх депозита.При наступлении любого из двух условий списывается штраф по ставке РЕПО.При расчете
                                                // штрафа учитывается суммарный объем по всем счетам и площадкам.  

                    foreach (var positionsSec in positionsUser.Value)
                    {
                        var pos = positionsSec.Value;

                        if (pos == null || pos.Board != board) 
                        {
                            continue;
                        }
                        if (pos.Open == false && pos.CurrentPos == 0)  // позиция закрыта, но почему-то находится в списке открытых (костыль)
                        {
                            // перенесем эту открытую позицию в закрытые, она пройдет клиринг в закрытых позициях
                            allOpenPosition[userLogin].TryRemove(positionsSec.Key, out _);  // удалили из списка открытых

                            if (!allClosePosition.ContainsKey(userLogin))
                            {
                                allClosePosition.TryAdd(userLogin, new());
                            }
                            if (!allClosePosition[userLogin].ContainsKey(pos.SecId))
                            {
                                allClosePosition[userLogin].TryAdd(pos.SecId, new());
                            }
                            allClosePosition[userLogin][pos.SecId].Add(pos);    // положили в список закрытых

                            needSave = true;

                            continue;   // с той позицией здесь больше ничего не делаем, она в списке закрытых будет обработана

                           
                        }
                        if (pos.DateCleared.Date == Helper.DateTimeMSK.Date)    // сегодня уже делали клиринг по этой позиции
                        {
                            continue;
                        }
                       
                        /*
                        Условия переноса позиций через основной клиринг и формулы расчета штрафов
                        Формула расчета штрафа за перенос позиций при личном депозите менее 10 000 рублей:
                        Дано:      
                        СП – сумма переноса      
                        КЛ – кол-во лотов      
                        ЦЛ – цена лота 
                        Пример расчета возьмем 1 к GMNK      
                        Формула: СП = КЛ * ЦЛ / шаг цены * цену шага СП * РЕПО (не изменяется) * 25 / 365 – сумма штрафа за перенос.
                        Пример: (9400 * 0.12 * 25) / 365 = 77,2 руб.      
                        Данная формула актуальна для всех площадок. 
                        */
                       
                        if (deposit < penaltyDepositAllowTransferOverClearing)
                        {
                            decimal penalty = pos.CurrentPos * pos.AverageEntry / pos.PriceStep * pos.PriceStepCost * PlazaOptions.REPO / 100 * 25 / 365;

                            ReportCapexHistoryTransaction historyTransaction = new()
                            {
                                AccountCapex = accCapex.Guid,
                                AccountTrade = accTrade.Guid,
                                Board = board,
                                TransactionDate = Helper.DateTimeMSK,
                                TransactionSumma = penalty * -1,
                                TransactionDescription = $"Списание средств с {accCapex.AccountNumber} на площадке {board} за перенос позиции через клиринг."
                            };
                            DBManager.TryAddObjectToDB(historyTransaction);

                            AddOrSubMoneyToCapex(accCapex, penalty * -1);   // засчитали штраф
                            needSave = true;
                        }
                        else
                        {

                            /*
                                Формула расчета штрафа за превышение объема перенесенных позиций (акции и фьючерсы) через основной
                                клиринг.
                                Для трейдеров у кого депозит более 10 000 рублей перенос допустим не более, чем на 5 плечо и не более, чем на 5
                                млн сверх депозита. При наступлении любого из двух условий списывается штраф по ставке РЕПО. При расчете
                                штрафа учитывается суммарный объем по всем счетам и площадкам. На площадке "Фьючерсы" перенос фиксируется
                                на вечернем клиринге, на площадке "Акции" - на основном клиринге.*/

                            summaPosition += pos.AverageEntry / pos.PriceStep * pos.PriceStepCost * pos.CurrentPos;

                        }

                        
                        pos.DateCleared = Helper.DateTimeMSK;
                        reportManager.CreateReportTradeDay(pos, userLogin, accCapex, accTrade);
                        reportManager.TransferTradeToReportFinRezInstrument(pos, accTrade);
                        reportManager.CreateReportTransferClearingPosition(pos, userLogin, accCapex, accTrade);
                    }

                    if (!accTrade.AllowOvernightTransfer && (summaPosition - deposit >= penaltySummaPosition || summaPosition/deposit > penaltySummaPositionLeverage))
                    {
                        decimal penalty = (summaPosition - deposit) * PlazaOptions.REPO / 100 * 25 / 365;
                        if (penalty > 0)
                        {
                            ReportCapexHistoryTransaction historyTransaction = new()
                            {
                                AccountCapex = accCapex.Guid,
                                AccountTrade = accTrade.Guid,
                                Board = board,
                                TransactionDate = Helper.DateTimeMSK,
                                TransactionSumma = penalty * -1,
                                TransactionDescription = $"Списание средств с {accCapex.AccountNumber} на площадке {board} за перенос позиции через клиринг."
                            };
                            DBManager.TryAddObjectToDB(historyTransaction);

                            AddOrSubMoneyToCapex(accCapex, penalty * -1);   // засчитали штраф
                            needSave = true;
                        }
                    }
                    if (!accTrade.CancelPenaltyDrawdown)
                    {
                        var finrez = serverService.GetFinanceResult(userLogin)?.FirstOrDefault(f=> f.Board == board);
                        if (finrez != null)
                        {
                            decimal allowDrawDown = Math.Min(accTrade.MaxDayLossAbsolut, accTrade.MaxDayLossPercent * deposit / 100);

                            if (finrez.DrawDown > allowDrawDown)
                            {
                                decimal penaltyDrawDown = (finrez.DrawDown - allowDrawDown) * PlazaOptions.PenaltyOverDrawDown / 100;
                                ReportCapexHistoryTransaction historyTransaction = new()
                                {
                                    AccountCapex = accCapex.Guid,
                                    AccountTrade = accTrade.Guid,
                                    Board = board,
                                    TransactionDate = Helper.DateTimeMSK,
                                    TransactionSumma = penaltyDrawDown * -1,
                                    TransactionDescription = $"Списание средств с {accCapex.AccountNumber} на площадке {board} за превышение размера просадки."
                                };
                                DBManager.TryAddObjectToDB(historyTransaction);

                                AddOrSubMoneyToCapex(accCapex, penaltyDrawDown * -1);   // засчитали штраф
                                needSave = true;
                            }
                        }
                    }
                    

                }
            }
            return needSave;
        }

        /// <summary>
        /// Клиринг закрытых позиций
        /// </summary>
        /// <param name="needSave"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        private bool ClearingClosePosition(bool needSave, BoardEnum board)
        {
            //ключ 1 - юзер
            //ключ 2 - sec ID
            ConcurrentDictionary<string, ConcurrentDictionary<string, List<Pos>>>? allClosePosition = serverService.GetAllClosePositions();
            // Клиринг закрытых позиций
            if (allClosePosition != null)
            {
                foreach (var positionsUser in allClosePosition)
                {
                    string userLogin = positionsUser.Key;
                    AccountCapex? accCapex = AccountsCapex.Where(a => a.UserName == userLogin)?.FirstOrDefault();
                    AccountTrade? accTrade = AccountsTrade.Where(a => a.AccountCapex == accCapex?.AccountNumber && a.Board == board).FirstOrDefault();
                    if (accCapex == null || accTrade == null)   // ошибка при проведении клиринга, нет счетов у трейдера на площадке
                    {
                        ServerLogger.Log($"Для {userLogin} во время клиринга не найден капитальный {accCapex} или торговый {accTrade} счет на площадке {board}", "ClearingAttention");
                        continue;
                    }
                    decimal summaPnL = 0;
                    
                    
                    List<ReportFinRezInstrument> reportFinRezInstrumentList = new ();

                    foreach (var positionsSec in positionsUser.Value)
                    {
                        string SecID = positionsSec.Key;
                        
                        for (int i = 0; i < positionsSec.Value.Count; i++)
                        {
                            Pos? pos = positionsSec.Value[i];
                            if (pos.Cleared)    // позиция уже прошла клиринг
                            {
                                continue;
                            }
                            if (pos.Board != board) // позиция с другой площадки
                            {
                                continue;
                            }

                            var reportFinRezInstrument = new ReportFinRezInstrument();
                            reportFinRezInstrumentList.Add(reportFinRezInstrument);
                            SecurityApi? sec = serverService.GetSecurity(SecID);
                            reportFinRezInstrument.SecId = sec?.Isin ?? SecID;

                            reportFinRezInstrument.AccountTrade = accTrade.Guid;
                            reportFinRezInstrument.Board = board;
                            reportFinRezInstrument.CommissionSumma = 0;
                            reportFinRezInstrument.DateReport = Helper.DateTimeMSK;
                            reportFinRezInstrument.FinResSumma = 0;


                            ClearingResult clearingResult = new ClearingResult();
                            clearingResult.Board = board;
                            clearingResult.UserLogin = userLogin;
                            clearingResult.AccountCapex = accCapex.AccountNumber;
                            clearingResult.AccountTrade = accTrade.AccountNumber;
                            clearingResult.PositionId = pos.Guid;
                            clearingResult.DateTimeClearing = Helper.DateTimeMSK;

                            decimal PnL = pos.RealizedPnl;

                            reportFinRezInstrument.Marga += PnL;


                            clearingResult.PnL = PnL;
                            PositionManager.ReCalculateCommission(pos);
                            //pos.RealizedPnl = 0;
                            PnL = PnL - pos.Commission;
                            clearingResult.CommissionSumma = pos.Commission;
                            clearingResult.PayOut = 0;

                            reportFinRezInstrument.CommissionSumma += pos.Commission;

                            if (PnL > 0)    // взять пэйаут
                            {
                                decimal officePayOut = accTrade.OfficePayOut * PnL / 100;
                                decimal referrerPayOut = accTrade.ReferrerPayOut * PnL / 100;
                                accTrade.TotalOfficePayOut += officePayOut;
                                accTrade.TotalReferrerPayOut += referrerPayOut;
                                PnL = PnL - officePayOut - referrerPayOut;
                                clearingResult.PayOut = officePayOut + referrerPayOut;

                                ReportCapexHistoryTransaction? reportCapexHistoryTransaction = new ReportCapexHistoryTransaction();
                                
                                reportCapexHistoryTransaction.AccountCapex = accCapex.Guid;
                                reportCapexHistoryTransaction.AccountTrade = accTrade.Guid;
                                reportCapexHistoryTransaction.Board = board;
                                reportCapexHistoryTransaction.ExchangeRate = 1; // обменный курс, мосбиржа в рублях, джругих вариантов пока нет
                                reportCapexHistoryTransaction.TransactionDate = Helper.DateTimeMSK;
                                reportCapexHistoryTransaction.TransactionDescription = $"PayOut (счёт {accCapex.AccountNumber}, {board}).";
                                reportCapexHistoryTransaction.TransactionSumma = clearingResult.PayOut;

                                DBManager.TryAddObjectToDB(reportCapexHistoryTransaction);
                            }

                            reportFinRezInstrument.FinResSumma += PnL;

                            lock (accountChangeLocker)
                            {
                                clearingResult.DepositCompanyBeforeClearing = accCapex.DepositCompany;
                                clearingResult.DepositTraderBeforeClearing = accCapex.DepositTrader;
                                // зачислить PnL
                                summaPnL += PnL;
                             
                                clearingResult.DepositCompanyAfterClearing = accCapex.DepositCompany;
                                clearingResult.DepositTraderAfterClearing = accCapex.DepositTrader;
                            }



                            clearingResult.Description = "Clearing closed positions";
                            clearingResult.Penalty = 0;
                            AddClearingResult(clearingResult);

                            reportManager.CreateReportTradeDay(pos, userLogin, accCapex, accTrade);

                            needSave = true;

                            pos.Cleared = true;
                            pos.DateCleared = Helper.DateTimeMSK;

                        }
                    }

                    DBManager.GetDBContext( db => db.ReportFinRezInstrument.AddRange(reportFinRezInstrumentList));   

                    AddOrSubMoneyToCapex(accCapex, summaPnL);


                }
            }

            return needSave;
        }

        /// <summary>
        /// Списать или зачислить деньги, прибыль, штрафы со счетов
        /// </summary>
        /// <param name="accCapex">Единый капитальный счет</param>
        /// <param name="money">Зачисления со знаком +, штрафы комиссии со знаком минус </param>
        private static void AddOrSubMoneyToCapex(AccountCapex accCapex, decimal money)
        {
            accCapex.DepositTrader += money;
            accCapex.DepositCompany += accCapex.DepositTrader;
            accCapex.DepositTrader = 0;

            if (accCapex.DepositCompany > accCapex.FullDepositCompany)
            {
                accCapex.DepositTrader += accCapex.DepositCompany - accCapex.FullDepositCompany;
                accCapex.DepositCompany = accCapex.FullDepositCompany;
            }
        }

        /// <summary>
        /// Клиринг капитальных счетов
        /// </summary>
        /// <param name="needSave"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        private bool ClearingCapexAccount(bool needSave, BoardEnum board)
        {
            // Клиринг капитальных счетов во время ночного клиринга 
            if (board == BoardEnum.StockMoex)       // тут на самом деле происходит клиринг всех капитальных счетов, без привязки к бирже, просто сделаем это ночью
            {
                for (int i = 0; i < AccountsCapex.Count; i++)
                {
                    AccountCapex? accCapex = AccountsCapex[i];
                    if (accCapex == null) continue;

                    ClearingResult clearingResult = new ClearingResult();
                    clearingResult.UserLogin = accCapex.UserName;
                    clearingResult.AccountTrade = "";
                    clearingResult.AccountCapex = accCapex.AccountNumber;
                    clearingResult.DateTimeClearing = Helper.DateTimeMSK;
                    clearingResult.DepositTraderBeforeClearing = accCapex.DepositTrader;
                    clearingResult.DepositCompanyBeforeClearing = accCapex.DepositCompany;
                    clearingResult.Description = "Clearing of capital accounts, crediting funds, replenishing an account";
                    clearingResult.Board = board;
                    clearingResult.PositionId = Guid.Empty;

                    lock (accountChangeLocker)
                    {
                        accCapex.DepositCompany += accCapex.AddDepositCompanyAfterClearing;
                        accCapex.FullDepositCompany += accCapex.AddDepositCompanyAfterClearing;
                        accCapex.AddDepositCompanyAfterClearing = 0;

                        accCapex.DepositTrader += accCapex.AddDepositTraderAfterClearing;
                        accCapex.FullDepositTrader += accCapex.AddDepositTraderAfterClearing;
                        accCapex.AddDepositTraderAfterClearing = 0;
                    }
                    AddClearingResult(clearingResult);
                }
                needSave = true;
            }

            return needSave;
        }


        /// <summary>
        /// Клиринг торговых счетов
        /// </summary>
        /// <param name="needSave"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        private bool ClearingTradeAccount(bool needSave, BoardEnum board)
        {
            // Клиринг торговых счетов по площадкам
            for (int i = 0; i < accountsTrade.Count; i++)
            {
                var accTrade = accountsTrade[i];
                if (accTrade == null) continue;
                if (accTrade.Board == board)
                {
                    AccountCapex? accCapex = AccountsCapex.Where(a => a.AccountNumber == accTrade.AccountCapex)?.FirstOrDefault();
                    ClearingResult clearingResult = new ClearingResult();
                    clearingResult.Board = board;
                    clearingResult.UserLogin = accCapex?.UserName ?? "Not set";
                    clearingResult.AccountCapex = accCapex?.AccountNumber ?? "Not set";
                    clearingResult.AccountTrade = accTrade.AccountNumber;
                    clearingResult.Description = "Clearing of trading accounts";
                    clearingResult.DateTimeClearing = Helper.DateTimeMSK;

                    lock (accountChangeLocker)
                    {
                        accTrade.CanTrade = accTrade.CanTradeAfterClearing;
                        accTrade.MaxDayLossAbsolut = accTrade.MaxDayLossAbsolutAfterClearing;
                        accTrade.MaxDayLossPercent = accTrade.MaxDayLossPercentAfterClearing;
                        accTrade.DateLastClearing = Helper.DateTimeMSK;
                        accTrade.DateBlockTradeSession = DateTime.MinValue;
                    }

                    AddClearingResult(clearingResult);
                    needSave = true;
                }
            }

            return needSave;
        }

        private void AddClearingResult(ClearingResult clearingResult)
        {
            /*Не требуется отдельная запись в XML
             * if (!ClearingList.TryGetValue(DateOnly.FromDateTime(DateTimeMSK), out var list))
            {
                list = new List<ClearingResult>();
                ClearingList.TryAdd(DateOnly.FromDateTime(DateTimeMSK), list);
            }
            list.Add(clearingResult);*/


            ServerLogger.Log(clearingResult.ToString(), "ClearingResult");
        }

        #endregion Клиринг


    }
}


