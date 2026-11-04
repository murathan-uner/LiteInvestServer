


using Aggregate;

using ConnectorService;

using LiteInvest.Entity.Helpers;
using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.Server2;
using LiteInvest.Entity.ServerEntity;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using PlazaEngine.Engine;

using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;

using static System.Runtime.InteropServices.JavaScript.JSType;


namespace LiteInvestServerDll
{
    public class ServerService : IDisposable
    {
        private const string MASTER_TAB_ID = "main";
        public static string SERVER_WARNING_USER_NOT_LOGGED_IN = "user not logged in";
        public static string SERVER_WARNING_LOADING_ORDERS = "wait for loading";
        public static string SERVER_ERROR_NO_USER = "no user";
        public static string SERVER_ERROR_NULL_SECURITY = "null security";

        #region Variables ConcurrentDictionarys

        public bool crypto { get; set; } = false;

        
        ConcurrentDictionaryDB<SecurityApi> Securities
        {
            get => serviceDB?.GetSecuritys();
        }

        ///База юзеров 
        //ConcurrentDictionary<string, User> UsersContext
        ConcurrentDictionaryDB<User> UsersContext
        {
            get => serviceDB?.GetUsersContext();
        }
        //База ордеров по юзеру
        ConcurrentDictionary<string, ConcurrentDictionary<int, Order>> Orders
        {
            get => serviceDB?.GetOrders();
        }
        //База сделок по юзеру
        // key 1 - username,
        // key 2 - tradeMarketNumber

        ConcurrentDictionary<string, ConcurrentDictionary<string, MyTrade>> Trades
        {
            get => serviceDB?.GetTrades();
        }

        /// <summary>
        /// ключ 1 - юзер
        /// ключ 2 - sec ID
        /// </summary>
        ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>> OpenedPositions
        {
            get => serviceDB?.GetOpenedPositions();
        }

        //ключ - юзер
        //ключ 2 - sec ID
        ConcurrentDictionary<string, ConcurrentDictionary<string, List<Pos>>> ClosedPositions
        {
            get => serviceDB?.GetClosedPositions();
        } //= new();


        ConcurrentDictionary<string, PositionOnBoard> RealPositions = new();

        

        PlazaConnector plaza = null;
        AggregateGlass aggregateGlass;
        AggregateCluster aggregateCluster;

        object savelocker = new object();



        public class ResponseHandMade
        {
            public bool IsSuccessfull { get; set; }
            public string Message { get; set; }

            public ResponseHandMade(bool isSuccessfull, string message)
            {
                IsSuccessfull = isSuccessfull;
                Message = message;
            }
        }

        async void LogMessageAsync(string message)
        {
            var dt = DateTime.Now.ToString("H:mm:ss.fff");
            await Console.Out.WriteLineAsync($"{dt} {message}").ConfigureAwait(false);
        }

        public List<int> ScaleList;


        PositionManager positionManager;

        RiskManager riskManager;

        PlazaOptions plazaOptions;

        ServiceDB serviceDB;

        public AccountManager accountManager;

        

        private readonly IDbContextFactory<AppDBContext> dbContextFactory;

        #endregion variablesConcurrentDictionarys


        #region Constructor, start, init
        public ServerService(IDbContextFactory<AppDBContext> _dbContextFactory)
        {
            dbContextFactory = _dbContextFactory;
            using (var db = dbContextFactory.CreateDbContext()) { /* Initialization connect DB */ }
        }



        public void Start(PlazaOptions _plazaOptions, ServiceDB _serviceDB, AccountManager _accountManager)
        {
            serviceDB = _serviceDB;
            serviceDB.Open();
            
            accountManager = _accountManager;
            accountManager.Open(this, serviceDB, dbContextFactory);

            plazaOptions = _plazaOptions;

            riskManager = new RiskManager(this, UsersContext);
            

            positionManager = new PositionManager(this);

            AddUserAdmin();

            bool simulation = _plazaOptions.Simulation;

            Console.WriteLine($"simulation PLAZA {simulation}");

            plaza = new PlazaConnector("02mMLX144T2yxnfzEUrCjUKzXKciQKJ", simulation, testTrading: false, appname: "osaApplication")
            {
                Limit = 30,
                LoadTicksFromStart = false,
            };



            ScaleList = PlazaOptions.ScaleList;
            aggregateGlass = AggregateGlass.Build(plaza, ScaleList);
            aggregateGlass.NewInsideQuotesEvent += AggregateGlass_NewInsideQuotesEvent;

            aggregateCluster = AggregateCluster.Build(plaza, ScaleList);
            aggregateCluster.NewInsideClusterEvent += AggregateCluster_NewInsideClusterEvent;

            plaza.UpdatePosition += pos =>
            {
                RealPositions[pos.SecurityId] = pos;
                LogMessageAsync($"New Pos Info sec_id={pos.SecurityId} {pos.XPosValueCurrent} ");
            };

            plaza.TicksLoadedEvent += Plaza_TicksLoadedEvent; ;
            plaza.NewMyTradeEvent += ProcessNewMyTrade;
            plaza.OrderLoadedEvent += Plaza_OrderLoadedEvent;
            plaza.OrderChangedEvent += Plaza_OrderChangedEvent;
            plaza.SecuritiesLoadedEvent += Plaza_SecuritiesLoadedEvent;

            plaza.NewTickCollectionEvent += ticksDictionary =>
            {
                if (ticksDictionary == null)
                    return;
               
                try
                {

                    //проверяем всех наших подписантов
                    foreach (var secIdsubcription in SubscriptionsForTicks)
                    {
                    
                        if (ticksDictionary.ContainsKey(secIdsubcription.Key) && ticksDictionary[secIdsubcription.Key].Count != 0)
                        {

                            var ticks = ticksDictionary[secIdsubcription.Key];

                            foreach (var action in secIdsubcription.Value)
                            {
                                //TODO: временная заплатка

                                // LogMessageAsync($"{socket.Key} Sending pack of ticks");
                                Task.Run(()=> action.Value?.Invoke(ticks));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessageAsync($"Problems with websocket TICKS {ex.Message}");
                }
            };


            plaza.UpdateSecurity += sec =>
            {
                var secInDB = DBManager.GetObjectFromDB<SecurityApi>(query => query.Where(o => o.id == sec.Id)).FirstOrDefault();

                if (secInDB != null)
                {
                    secInDB.id = sec.Id;
                    secInDB.ShortName = sec.ShortName;
                    secInDB.ClassCode = sec.ClassCode;
                    secInDB.Isin = sec.Name;
                    secInDB.FullName = sec.FullName;
                    secInDB.Type = sec.Type.ToString();
                    secInDB.Lot = sec.Lot;
                    secInDB.PriceStep = sec.PriceStep;
                    secInDB.PriceStepCost = sec.PriceStepCost;
                    secInDB.Decimals = sec.Decimals;
                    secInDB.PriceLimitHigh = sec.PriceLimitHigh;
                    secInDB.PriceLimitLow = sec.PriceLimitLow;
                    secInDB.DateUpdateFromMoex = sec.Expiration;
                }
                else
                {
                    secInDB = new SecurityApi()
                    {
                        id = sec.Id,
                        ShortName = sec.ShortName,
                        ClassCode = sec.ClassCode,
                        Isin = sec.Name,
                        FullName = sec.FullName,
                        Type = sec.Type.ToString(),
                        Lot = sec.Lot,
                        PriceStep = sec.PriceStep,
                        PriceStepCost = sec.PriceStepCost,
                        Decimals = sec.Decimals,
                        PriceLimitHigh = sec.PriceLimitHigh,
                        PriceLimitLow = sec.PriceLimitLow,
                        DateUpdateFromMoex = sec.Expiration
                    };
                }
                Securities[sec.Id] = secInDB;
            };


            if (plaza.Emulation)
            {
                foreach (var sec in Securities)
                {
                    plaza.Securities.TryAdd(sec.Key, new Security(sec.Value.ShortName, sec.Value.FullName, SecurityType.Futures, sec.Value.ClassCode, sec.Value.Lot)
                    {
                        Id = sec.Value.id,
                        Name =sec.Value.ShortName,// "Emulation Security",
                        ShortName = sec.Value.ShortName,
                        PriceStep = sec.Value.PriceStep,
                        PriceLimitLow = sec.Value.PriceLimitLow,
                        PriceLimitHigh = sec.Value.PriceLimitHigh,
                        PriceStepCost = sec.Value.PriceStepCost != 0? sec.Value.PriceStepCost : 1 ,

                    });
                }
            }

            plaza.Connect();

        }

        private bool plazaAllTicksLoaded = false;
        private void Plaza_TicksLoadedEvent()
        {
            plazaAllTicksLoaded = true;
        }

        public bool GetConnectorAllDataIsReady(BoardEnum board)
        {
            if (board == BoardEnum.FortsMoex)
            {
                return plazaAllTicksLoaded && plazaAllOrderLoaded && plazaAllSecuritiesLoaded;
            }
            return false;
        }

        private bool plazaAllSecuritiesLoaded = false;

        private void Plaza_SecuritiesLoadedEvent()
        {
            foreach (var item in Securities)
            {
                var sec = item.Value;
                if (sec.DateUpdateFromMoex < DateTime.UtcNow.AddHours(3).Date)
                {
                    Securities.TryRemove(item);
                }

            }
            plazaAllSecuritiesLoaded = true;
        }

        #endregion Constructor, start, init 

        #region Users, Tabs, Instruments, Instrument profiles

        // Data Structure
        // User --> Tabs --> Instruments --> Profiles
        //      User : Tabs | 1 : n
        //      Tab : Instruments | 1 : n
        //      Instrument : Profile | 1 : 1

        private void AddUserAdmin()
        {
            string admin = "adminadminov";
            string basicuser = "samujan1@yandex.ru";

            if (!UsersContext.ContainsKey(admin))
                UsersContext.TryAdd(admin, new User(admin, "adminPass#1R") { Admin = true, CanInput = true });
            UsersContext.TryGetValue(admin, out var user);
            user.CanInput = true;

            if (!UsersContext.ContainsKey(basicuser))
                UsersContext.TryAdd(basicuser, new User(basicuser, "pass2") { Admin = false, CanInput = true });
            UsersContext.TryGetValue(basicuser, out user);
            user.CanInput = true;
        }


        /// <summary>
        /// Взять пользователя по его имени
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public User? GetUser(string userName)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out var user))
            {
                return user;
            }
            return null;
        }

        /// <summary>
        /// Get the best buy/sell price or if it is zero/null, get the last price
        /// </summary>
        /// <param name="secId"></param>
        /// <returns></returns>
        public (decimal BestBid, decimal BestAsk) GetBestBidAskPrice(string secId)
        {
            (MarketDepthLevel BestBid, MarketDepthLevel BestAsk) bestPrice =  aggregateGlass.GetBestBidAskPrice(secId);

            if (bestPrice.BestBid == null || bestPrice.BestAsk == null || bestPrice.BestBid?.Price == 0 || bestPrice.BestAsk?.Price == 0)
            {
                decimal lastPrice = GetLastPrice(secId);
                return (lastPrice, lastPrice);
            }
            return (bestPrice.BestBid.Price, bestPrice.BestAsk.Price);
        }

        public decimal GetLastPrice(string secId)
        {
            if (plaza?.ticksplaza?.AllTicks.TryGetValue(secId,out var t)??false)
            {
                decimal lastPrice = t?.Price ?? 0;
                return lastPrice;
            }
            return 0;
        }

        public List<SecurityApi> GetAllSecurities()
        {
            if (Securities == null || Securities.IsEmpty)
                return [];

            return [.. Securities.Values.OrderBy(s => s.ShortName)];
        }

        public SecurityApi? GetSecurity(string secId)
        {
            if (Securities.TryGetValue(secId, out var sec))
            {
                return sec;
            }
            return null;
        }

        public TabProfile? GetUserMasterTab(string userName)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user))
            {
                return user.MasterTab;
            }

            return null;
        }

        //public ConcurrentDictionary<string, TabProfile>? GetUserTabs(string userName)
        //{
        //    if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user))
        //    {
        //        return user.Tabs;
        //    }

        //    return null;
        //}
        public List<TabProfile>? GetUserTabs(string userName)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user))
            {
                return user.Tabs;
            }

            return null;
        }

        public static TabProfile GenerateDefaultTab(string tabId)
        {
            var profile = new TabProfile
            {
                Id = tabId
            };

            return profile;
        }

        /// <summary>
        /// Get table based dialog profile by user name and dialog type
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public DialogProfile? GetDialogProfile(string userName, DialogType type)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user))
            {
                return type switch
                {
                    DialogType.InstrumentPicker => user.InstrumentPickerProfile,
                    DialogType.ActiveOrders => user.ActiveOrdersDialogProfile,
                    DialogType.ActivePositions => user.ActivePositionsDialogProfile,
                    _ => null
                };
            }
            return null;
        }

        /// <summary>
        /// Get user favorite security profiles by user name
        /// </summary>
        /// <param name="userName">logged in user name</param>
        /// <returns></returns>
        public ConcurrentDictionary<string /* security id */, SecurityProfile> GetUserFavoriteInstruments(string userName)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user))
            {
                return user.FavoriteInstruments ?? [];
            }

            return [];
        }

        /// <summary>
        /// Get user favorite security profile by user name and security id
        /// </summary>
        /// <param name="userName">logged in user name</param>
        /// <param name="secId">security id</param>
        /// <returns></returns>
        public SecurityProfile? GetUserFavoriteInstrument(string userName, string secId)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user) && !string.IsNullOrEmpty(secId))
            {
                if (user.FavoriteInstruments == null)
                    return null;

                if (user.FavoriteInstruments.TryGetValue(secId, out SecurityProfile? profile))
                    return profile;
            }

            return null;
        }

        /// <summary>
        /// Get user recent security profiles by user name
        /// </summary>
        /// <param name="userName">logged in user name</param>
        /// <returns></returns>
        public ConcurrentDictionary<string /* security id */, (int /* order index */, SecurityProfile)> GetUserRecentInstruments(string userName)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user))
            {
                return user.RecentInstruments ?? [];
            }

            return [];
        }

        /// <summary>
        /// Get user recent security profile by user name and security id
        /// </summary>
        /// <param name="userName">logged in user name</param>
        /// <param name="secId">security id</param>
        /// <returns></returns>
        public SecurityProfile? GetUserRecentInstrument(string userName, string secId)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user) && !string.IsNullOrEmpty(secId))
            {
                if (user.RecentInstruments == null)
                    return null;

                if (user.RecentInstruments.TryGetValue(secId, out (int, SecurityProfile) value))
                    return value.Item2;
            }

            return null;
        }

        /// <summary>
        /// Get user's all security profiles
        /// </summary>
        /// <param name="userName">logged in user name</param>
        /// <returns></returns>
        public ConcurrentDictionary<string /* tab id */, ConcurrentDictionary<string /* security special hash */, SecurityProfile>> GetUserInstruments(string userName)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user))
            {
                return user.Instruments ?? [];
            }

            return [];
        }

        /// <summary>
        /// Get user's all security profiles by tab id
        /// </summary>
        /// <param name="userName">logged in user name</param>
        /// <param name="tabId">null: master tab, GUID: child tab</param>
        /// <returns></returns>
        public ConcurrentDictionary<string /* security special hash */, SecurityProfile> GetUserInstrumentsByTab(string userName, string? tabId)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user))
            {
                var instruments = user.Instruments ?? [];

                string _tabId = string.IsNullOrEmpty(tabId) ? MASTER_TAB_ID : tabId;

                if (instruments.TryGetValue(_tabId, out var tabInstruments))
                {
                    return tabInstruments;
                }
            }

            return [];
        }

        /// <summary>
        /// Get user security profile by user name and instrument's GUID
        /// </summary>
        /// <param name="userName">logged in user name</param>
        /// <param name="tabId">tab id</param>
        /// <param name="specialHash">dynamically generated security's GUID</param>
        /// <returns></returns>
        public SecurityProfile? GetUserInstrument(string userName, string? tabId, string specialHash)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user) && !string.IsNullOrEmpty(specialHash))
            {
                var instruments = user.Instruments;
                if (instruments == null)
                    return null;

                string _tabId = string.IsNullOrEmpty(tabId) ? MASTER_TAB_ID : tabId;

                if (instruments.TryGetValue(_tabId, out var tabInstruments))
                {
                    if (tabInstruments.TryGetValue(specialHash, out var profile))
                    {
                        return profile;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get user security profile by user name and security id
        /// </summary>
        /// <param name="userName">logged in user name</param>
        /// <param name="specialHash">security id</param>
        /// <returns></returns>
        public SecurityProfile? GetUserInstrumentBySecId(string userName, string? tabId, string secId)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user) && !string.IsNullOrEmpty(secId))
            {
                var instruments = user.Instruments;
                if (instruments == null)
                    return null;

                string _tabId = string.IsNullOrEmpty(tabId) ? MASTER_TAB_ID : tabId;

                if (instruments.TryGetValue(_tabId, out var tabInstruments))
                {
                    var profile = tabInstruments.Values.FirstOrDefault(p => p.Id == secId);
                    return profile;
                }
            }
            return null;
        }

        public SecurityProfile GenerateDefaultInstrument(string secId)
        {
            var profile = new SecurityProfile
            {
                Id = secId,
                Scale = 0,
                LikeTradesVolume = [.. plazaOptions.LikeTradesVolume],
                LikeTradesVolumeIndex = 0,
                AutomaticScrollFlag = false
            };

            return profile;
        }

        public ConcurrentDictionary<string, int>? GetUsersInstrumentOrders(string userName, string? tabId)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user))
            {
                var orders = user.InstrumentOrders;
                if (orders == null)
                    return null;

                string _tabId = string.IsNullOrEmpty(tabId) ? MASTER_TAB_ID : tabId;

                if (orders.TryGetValue(_tabId, out var tabInstrumentOrders))
                {
                    return tabInstrumentOrders;
                }
            }

            return null;
        }

        /// <summary>
        /// Add/Update child tab profile
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="tabId"></param>
        public async void SaveMasterTab(string userName, TabProfile tab)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user))
                return;

            await Task.Run(() =>
            {
                user.MasterTab = tab;
            });
        }

        /// <summary>
        /// Add/Update child tab profile
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="tabId"></param>
        public async void SaveTab(string userName, string tabId)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || string.IsNullOrEmpty(tabId))
                return;

            await Task.Run(() =>
            {
                user.Tabs ??= [];
                //user.Tabs[tabId] = GenerateDefaultTab(tabId);
                if (!user.Tabs.Exists(x => x.Id == tabId))
                {
                    Console.WriteLine($">>> Saved a default tab: {tabId}");
                    user.Tabs.Add(GenerateDefaultTab(tabId));
                }
            });
        }

        /// <summary>
        /// Add/Update child tab profile
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="tab"></param>
        public async void SaveTab(string userName, TabProfile tab)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || string.IsNullOrEmpty(tab.Id))
                return;

            await Task.Run(() =>
            {
                //if (tab.Left < 0) tab.Left = 0;
                //if (tab.Top < 0) tab.Top = 0;
                //if (tab.Width < 0) tab.Width = 0;
                //if (tab.Height < 0) tab.Height = 0;
                
                // TODO - detect tab's miniziation
                if (tab.Left < -1000 && tab.Top < -1000)
                {
                    tab.Left = 0;
                    tab.Top = 0;
                    tab.Width = 0;
                    tab.Height = 0;
                }
                if (tab.Width < 0) tab.Width = 0;
                if (tab.Height < 0) tab.Height = 0;

                user.Tabs ??= [];
                //user.Tabs[tab.Id] = tab;
                var index = user.Tabs.FindIndex(t => t.Id == tab.Id);
                if (index != -1)
                {
                    Console.WriteLine($">>> Updated tab: {tab.Caption}, {tab.Id}");
                    user.Tabs[index] = tab;
                }
                else
                {
                    Console.WriteLine($">>> Added tab: {tab.Caption}, {tab.Id}");
                    user.Tabs.Add(tab);
                }
            });
        }

        /// <summary>
        /// Add/Update child tabs profile
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="tabs"></param>
        public async void SaveTabs(string userName, List<TabProfile> tabs)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user))
                return;

            await Task.Run(() =>
            {
                user.Tabs ??= [];
                foreach (var tab in tabs)
                {
                    if (string.IsNullOrEmpty(tab.Id))
                        continue;

                    // detect tab's miniziation
                    if (tab.Left < -1000 && tab.Top < -1000)
                    {
                        tab.Left = 0;
                        tab.Top = 0;
                        tab.Width = 0;
                        tab.Height = 0;
                    }
                    if (tab.Width < 0) tab.Width = 0;
                    if (tab.Height < 0) tab.Height = 0;

                    //user.Tabs[tab.Id] = tab;
                    var index = user.Tabs.FindIndex(t => t.Id == tab.Id);
                    if (index != -1)
                    {
                        Console.WriteLine($">>> Updated tab(F): {tab.Caption}, {tab.Id}");
                        tab.Caption = user.Tabs[index].Caption; // to avoid missing caption change from child
                        user.Tabs[index] = tab;
                    }
                    else
                    {
                        Console.WriteLine($">>> Added tab(F): {tab.Caption}, {tab.Id}");
                        user.Tabs.Add(tab);
                    }
                }
            });
        }

        /// <summary>
        /// Save user table based dialog profile
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="type"></param>
        /// <param name="profile"></param>
        public async void SaveDialogProfile(string userName, DialogType type, DialogProfile? profile)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user))
            {
                await Task.Run(() =>
                {
                    switch (type)
                    {
                        case DialogType.InstrumentPicker:
                            user.InstrumentPickerProfile = (InstrumentPickerProfile?)profile;
                            break;
                        case DialogType.ActiveOrders:
                            user.ActiveOrdersDialogProfile = (ActiveOrdersDialogProfile?)profile;
                            break;
                        case DialogType.ActivePositions:
                            user.ActivePositionsDialogProfile = (ActivePositionsDialogProfile?)profile;
                            break;
                    }
                });
            }
        }

        /// <summary>
        /// Save user favorite securities
        /// Called when
        ///     - adding a favorite
        ///     - the instrument already exists in the favorite list and closing it
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="secId"></param>
        /// <param name="profile">null: adding a favorite</param>
        public async void SaveUserFavoriteInstrument(string userName, string secId, SecurityProfile? profile)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || string.IsNullOrEmpty(secId))
                return;

            await Task.Run(() =>
            {
                user.FavoriteInstruments ??= [];
                profile ??= GenerateDefaultInstrument(secId);
                user.FavoriteInstruments[secId] = profile;
            });
        }

        /// <summary>
        /// Add user recent security or replace the order to the top of the list
        /// Called when
        ///     - closing an instrument
        ///     - opening a new instrument
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="secId"></param>
        /// <param name="profile"></param>
        public async void SaveUserRecentInstrument(string userName, string secId, SecurityProfile profile)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || string.IsNullOrEmpty(secId))
                return;

            await Task.Run(() =>
            {

                // D, C, B, A(closing) => A, D, C, B
                // D, C, B, A => opening E => E, D, C, B, A

                user.RecentInstruments ??= [];
                int lastOrderIndex;
                if (user.RecentInstruments.TryGetValue(secId, out (int, SecurityProfile) value))
                {
                    if (user.RecentInstruments[secId].Item1 == user.RecentInstrumentsLastOrderIndex)
                        // current profile is the last one, so do not change the order index
                        lastOrderIndex = user.RecentInstrumentsLastOrderIndex ?? 0;
                    else
                        lastOrderIndex = (user.RecentInstrumentsLastOrderIndex ?? -1) + 1;
                }
                else
                {
                    lastOrderIndex = (user.RecentInstrumentsLastOrderIndex ?? -1) + 1;
                }

                user.RecentInstruments[secId] = (lastOrderIndex, profile);
                user.RecentInstrumentsLastOrderIndex = lastOrderIndex;
            });
        }

        /// <summary>
        /// Save user instrument profile
        /// Called every time any setting's changed
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="tabId"></param>
        /// <param name="specialHash"></param>
        /// <param name="profile"></param>
        public async void SaveUserInstrument(string userName, string? tabId, string specialHash, SecurityProfile profile)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || string.IsNullOrEmpty(specialHash))
                return;

            string _tabId = string.IsNullOrEmpty(tabId) ? MASTER_TAB_ID : tabId;

            await Task.Run(() =>
            {
                user.Instruments ??= [];
                var tabDict = user.Instruments.GetOrAdd(_tabId, _ => new ConcurrentDictionary<string, SecurityProfile>());
                tabDict[specialHash] = profile;
            });
        }

        public async void SaveUserInstrumentsOrder(string userName, string? tabId, List<SecurityApi> secs)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || secs.Count == 0)
                return;

            string _tabId = string.IsNullOrEmpty(tabId) ? MASTER_TAB_ID : tabId;

            await Task.Run(() =>
            {
                user.InstrumentOrders ??= [];
                var tabDict = user.InstrumentOrders.GetOrAdd(_tabId, _ => new ConcurrentDictionary<string, int>());

                for (var i = 0; i < secs.Count; i++)
                {
                    var sec = secs[i];
                    tabDict[sec.SpecialHash] = i;
                }
            });
        }

        // When adding new instrument
        public async void SaveUserInstrumentOrder(string userName, string? tabId, string specialHash)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || string.IsNullOrEmpty(specialHash))
                return;

            string _tabId = string.IsNullOrEmpty(tabId) ? MASTER_TAB_ID : tabId;

            await Task.Run(() =>
            {
                user.InstrumentOrders ??= [];
                var tabDict = user.InstrumentOrders.GetOrAdd(_tabId, _ => new ConcurrentDictionary<string, int>());

                if (tabDict.IsEmpty)
                {
                    tabDict[specialHash] = 0;
                }
                else if (!tabDict.ContainsKey(specialHash))
                {
                    var lastOrder = tabDict.OrderByDescending(x => x.Value).FirstOrDefault().Value;
                    tabDict[specialHash] = lastOrder + 1;
                }
            });
        }

        public async void ReplaceUserInstrumentOrder(string userName, string? tabId, string oldSpecialHash, string newSpecialHash)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || string.IsNullOrEmpty(oldSpecialHash) || string.IsNullOrEmpty(newSpecialHash))
                return;
            if (user.InstrumentOrders == null || !user.InstrumentOrders.ContainsKey(oldSpecialHash))
                return;

            string _tabId = string.IsNullOrEmpty(tabId) ? MASTER_TAB_ID : tabId;

            await Task.Run(() =>
            {
                if (user.InstrumentOrders.TryGetValue(_tabId, out var tabDict))
                {
                    var order = tabDict.FirstOrDefault(x => x.Key == oldSpecialHash).Value;
                    tabDict.TryRemove(oldSpecialHash, out _);
                    tabDict[newSpecialHash] = order;
                }
            });
        }

        /// <summary>
        /// Remove child tab id
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="tabId"></param>
        public async void DeleteTab(string userName, string tabId)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || string.IsNullOrEmpty(tabId) || user.Tabs == null)
                return;

            await Task.Run(() =>
            {
                //user.Tabs.TryRemove(tabId, out _);
                var item = user.Tabs.FirstOrDefault(t => t.Id == tabId);
                if (item != null)
                {
                    user.Tabs.Remove(item);
                }
            });
        }

        /// <summary>
        /// Remove user table based dialog profile
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="type"></param>
        public async void DeleteDialogProfile(string userName, DialogType type)
        {
            if (!string.IsNullOrWhiteSpace(userName) && UsersContext.TryGetValue(userName, out User? user))
            {
                await Task.Run(() =>
                {
                    switch (type)
                    {
                        case DialogType.InstrumentPicker:
                            user.InstrumentPickerProfile = null;
                            break;
                        case DialogType.ActiveOrders:
                            user.ActiveOrdersDialogProfile = null;
                            break;
                        case DialogType.ActivePositions:
                            user.ActivePositionsDialogProfile = null;
                            break;
                    }
                });
            }
        }

        public async void DeleteUserFavoriteInstrument(string userName, string secId)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || string.IsNullOrEmpty(secId))
                return;

            await Task.Run(() =>
            {
                user.FavoriteInstruments.TryRemove(secId, out _);
            });
        }

        public async void DeleteUserRecentInstrument(string userName, string secId)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || string.IsNullOrEmpty(secId))
                return;

            await Task.Run(() =>
            {
                user.RecentInstruments.TryRemove(secId, out _);
                user.RecentInstrumentsLastOrderIndex = -1;
            });
        }

        /// <summary>
        /// Delete user Instrument
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="tabId"></param>
        /// <param name="specialHash"></param>
        public async void DeleteUserInstrumentAsync(string userName, string? tabId, string specialHash)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || string.IsNullOrEmpty(specialHash))
                return;

            string _tabId = string.IsNullOrEmpty(tabId) ? MASTER_TAB_ID : tabId;

            await Task.Run(() =>
            {
                if (user.Instruments.TryGetValue(_tabId, out var tabDict))
                {
                    tabDict.TryRemove(specialHash, out _);
                }
            });
        }

        /// <summary>
        /// Delete user's all Instruments of a tab
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="tabId"></param>
        public async void DeleteUserInstrumentsAsync(string userName, string? tabId)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user))
                return;

            string _tabId = string.IsNullOrEmpty(tabId) ? MASTER_TAB_ID : tabId;

            await Task.Run(() =>
            {
                user.Instruments.TryRemove(_tabId, out _);
            });
        }

        /// <summary>
        /// Delete user Instrument order
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="tabId"></param>
        /// <param name="specialHash"></param>
        public async void DeleteUserInstrumentOrderAsync(string userName, string? tabId, string specialHash)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user) || string.IsNullOrEmpty(specialHash))
                return;

            string _tabId = string.IsNullOrEmpty(tabId) ? MASTER_TAB_ID : tabId;
            
            await Task.Run(() =>
            {
                if (user.InstrumentOrders.TryGetValue(_tabId, out var tabDict))
                {
                    tabDict.TryRemove(specialHash, out _);
                }
            });
        }

        /// <summary>
        /// Delete user's all Instrument orders for a tab
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="tabId"></param>
        public async void DeleteUserInstrumentOrdersAsync(string userName, string? tabId)
        {
            if (string.IsNullOrWhiteSpace(userName) || !UsersContext.TryGetValue(userName, out User? user))
                return;

            string _tabId = string.IsNullOrEmpty(tabId) ? MASTER_TAB_ID : tabId;

            await Task.Run(() =>
            {
                user.InstrumentOrders.TryRemove(_tabId, out _);
            });
        }

        #endregion Users, Tabs, Instruments, Instrument profiles

        #region HelperMethods

        private void AggregateCluster_NewInsideClusterEvent(InsideClusters insideClusters)
        {
            if (!SubscriptionsForScaledCluster.ContainsKey(insideClusters.SecurityId) // не подписывались на этот инструмент
                || SubscriptionsForScaledCluster[insideClusters.SecurityId] == null
                )
            {
                return;
            }
            foreach (var subscription in SubscriptionsForScaledCluster[insideClusters.SecurityId])
            {
                subscription.Value?.Invoke(insideClusters);
            }
        }
        /// <summary>
        /// Пришел агрегированный стакан 
        /// </summary>
        /// <param name="insideQuotes"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void AggregateGlass_NewInsideQuotesEvent(InsideQuotes insideQuotes)
        {
            if (!SubscriptionsForScaledOrderBook.ContainsKey(insideQuotes.SecurityId) // не подписывались на этот инструмент
                || SubscriptionsForScaledOrderBook[insideQuotes.SecurityId] == null
                )
            {
                return;
            }
            foreach (var subscription in SubscriptionsForScaledOrderBook[insideQuotes.SecurityId])
            {
                subscription.Value?.Invoke(insideQuotes);
            }

        }



        /// <summary>
        /// принудительное закрытие позиций по просадке
        /// </summary>
        public event Action<User> MarginClosePosition;
        #endregion HelperMethods

        #region Subscribtion

        /// <summary>
        /// Подписки на агрегированный сткан
        /// ключ - id инструмента, value - callback метод обработки в клиенте
        /// </summary>
        ConcurrentDictionary<string, ConcurrentDictionary<string, Action<InsideQuotes>>> SubscriptionsForScaledOrderBook { get; set; } = new();

        /// <summary>
        /// key 1 - SecId, key 2 - guid
        /// </summary>
        ConcurrentDictionary<string, ConcurrentDictionary<string, Action<List<Trade>>>> SubscriptionsForTicks { get; set; } = new();

        ConcurrentDictionary<string, ConcurrentDictionary<string, Action<InsideClusters>>> SubscriptionsForScaledCluster { get; set; } = new();

        //ConcurrentDictionary<string, Action> SubscriptionsForInitialOrdersReady { get; set; } = new();

        //public async Task<SubscriptionAnswer> SubscribeForInitialOrdersReady(string username, Action action)
        //{
        //    if (username == null || !UsersContext.ContainsKey(username))
        //        return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

        //    var result = SubscriptionsForInitialOrdersReady.TryAdd(username, action);

        //    return new SubscriptionAnswer()
        //    {
        //        Success = result,
        //        Id = username
        //    };
        //}

        //public async Task<SubscriptionAnswer> RemoveSubscribeForInitialOrdersReady(string username)
        //{
        //    if (username == null || !UsersContext.ContainsKey(username))
        //        return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

        //    var result = SubscriptionsForInitialOrdersReady.TryRemove(username, out var _);

        //    return new SubscriptionAnswer()
        //    {
        //        Success = result,
        //        Id = username
        //    };
        //}

        /// <summary>
        /// 
        /// </summary>
        public async Task<SubscriptionAnswer> SubscribeForPrivateOrders(string username, Action<List<Order>> action)
        {
            if (username == null || !UsersContext.TryGetValue(username, out User? user))
                return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

            string guid = Guid.NewGuid().ToString();
            if (user.PrivateOrderEventsForUsers == null)
            {
                user.PrivateOrderEventsForUsers = new ConcurrentDictionary<string, Action<List<Order>>>();
            }
            var result = user.PrivateOrderEventsForUsers.TryAdd(guid, action);

            return new SubscriptionAnswer()
            {
                Success = result,
                Id = guid
            };

        }

        public async Task<SubscriptionAnswer> RemoveSubscribtionForPrivateOrders(string username, string id)
        {
            if (username == null || !UsersContext.TryGetValue(username, out User? user) || id == null)
                return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

            if (!user.PrivateOrderEventsForUsers.ContainsKey(id))
                return new SubscriptionAnswer { Success = true, ErrorMessage = "No Such Subscription", Id = id };

            var result = user.PrivateOrderEventsForUsers.TryRemove(id, out var _);

            return new SubscriptionAnswer()
            {
                Success = result,
                Id = id
            };

        }

        /// <summary>
        /// TODO: надо сделать проверку ключа.
        /// </summary>
        public async Task<SubscriptionAnswer> SubscribeForFinanceResult(string username, Action<List<FinanceResult>> action)
        {
            if (username == null || !UsersContext.ContainsKey(username))
                return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

            string guid = Guid.NewGuid().ToString();

            bool result = riskManager.SubscribeForFinanceResult(username, action);

            return new SubscriptionAnswer()
            {
                Success = result,
                Id = guid
            };

        }

        public async Task<SubscriptionAnswer> RemoveSubscribtionForFinanceResult(string username, string id)
        {
            if (username == null || !UsersContext.ContainsKey(username) || id == null)
                return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

            bool result = riskManager.UnSubscribeForFinanceResult(username, id);

            return new SubscriptionAnswer()
            {
                Success = result,
                Id = id
            };

        }




        /// <summary>
        /// Subscription aggregate Glass 
        /// </summary>
        /// <param name="secid"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<SubscriptionAnswer> SubscribeForScaledOrderBook(string secid, Action<InsideQuotes> action)
        {
            //if (!UsersContext.ContainsKey(username))
            //	return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

            if (!SubscriptionsForScaledOrderBook.ContainsKey(secid))
                SubscriptionsForScaledOrderBook[secid] = new();


            if (!plaza.Securities.ContainsKey(secid))
                return new SubscriptionAnswer() { Success = false, ErrorMessage = "No Sec Found" };

            aggregateGlass.SubscribAllScaledGlass(secid, plazaOptions.Simulation);

            aggregateGlass.ChangeMinMaxSizeGlassEvent += (s) => { /*Console.WriteLine($"ChangeMinMaxSizeGlassEvent {s}");*/ };


            string guid = Guid.NewGuid().ToString();
            //проверить будет ли это автоматом работать
            var result = SubscriptionsForScaledOrderBook[secid].TryAdd(guid, action);

            return new SubscriptionAnswer()
            {
                Success = result,
                Id = guid
            };

        }

        public async Task<SubscriptionAnswer> RemoveSubscribeForScaledOrderBook(string secid, string id)
        {
            //проверить будет ли это автоматом работать
            SubscriptionsForScaledOrderBook.TryGetValue(secid, out var value);
            bool result = value?.TryRemove(id, out var _) ?? true; // автоматом не работает

            return new SubscriptionAnswer()
            {
                Success = result,
                Id = id
            };

        }

        /// <summary>
        /// Subscription aggregate Glass 
        /// </summary>
        /// <param name="secid"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public async Task<SubscriptionAnswer> SubscribeForAggregateCluster(string secid, Action<InsideClusters> action)
        {
            //if (!UsersContext.ContainsKey(username))
            //	return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

            if (!SubscriptionsForScaledCluster.ContainsKey(secid))
                SubscriptionsForScaledCluster[secid] = new();


            if (!plaza.Securities.ContainsKey(secid))
                return new SubscriptionAnswer() { Success = false, ErrorMessage = "No Sec Found" };



            aggregateCluster.SubscribAllScaledCluster(secid, plazaOptions.Simulation);

            string guid = Guid.NewGuid().ToString();
            //проверить будет ли это автоматом работать
            var result = SubscriptionsForScaledCluster[secid].TryAdd(guid, action);

            return new SubscriptionAnswer()
            {
                Success = result,
                Id = guid
            };

        }

        public SubscriptionAnswer SubscribeForTicks(string secid, Action<List<Trade>> action)
        {
            //if (!UsersContext.ContainsKey(username))
            //	return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };


            if (!plaza.Securities.ContainsKey(secid))
                return new SubscriptionAnswer() { Success = false, ErrorMessage = "No Sec Found" };

            plaza.TryRegisterTicks(plaza.Securities[secid], plazaOptions.Simulation);

            string guid = Guid.NewGuid().ToString();
            //проверить будет ли это автоматом работать

            if (!SubscriptionsForTicks.ContainsKey(secid))
                SubscriptionsForTicks[secid] = new();

            var result = SubscriptionsForTicks[secid].TryAdd(guid, action);

            return new SubscriptionAnswer()
            {
                Success = result,
                Id = guid
            };

        }


        public async Task<SubscriptionAnswer> RemoveSubscribeForTicks(string secid, string id)
        {
            if (id == null)
                return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

            //проверить будет ли это автоматом работать
            SubscriptionsForTicks.TryGetValue(secid, out var value);
            var result = value?.TryRemove(id, out var _) ?? true;

            return new SubscriptionAnswer()
            {
                Success = result,
                Id = id
            };

        }


        #endregion

        #region MyTrade

        void ProcessNewMyTrade(MyTrade newMytrade)
        {
            try
            {
                var username = newMytrade.Comment;

                if (Trades.TryGetValue(username, out var userTrades) && userTrades.ContainsKey(newMytrade.NumberTrade))
                {
                    return; // сделка уже учтена
                }
                else
                {
                    if (userTrades == null)
                    {
                        userTrades = new ConcurrentDictionary<string, MyTrade>();
                        Trades.TryAdd(username, userTrades);
                    }
                    userTrades.TryAdd(newMytrade.NumberTrade, newMytrade);
                }

                if (string.IsNullOrWhiteSpace(username) || !UsersContext.ContainsKey(username))
                    return;

                if (!OpenedPositions.ContainsKey(username))
                    OpenedPositions.TryAdd(username, new ConcurrentDictionary<string, Pos>());

                OpenedPositions[username].TryGetValue(newMytrade.SecurityId, out var posvalue);

                if (posvalue == null)
                {
                    
                    var secname = Securities.ContainsKey(newMytrade.SecurityId) ? Securities[newMytrade.SecurityId].Isin : "Empty";

                    Securities.TryGetValue(newMytrade.SecurityId, out var sec);

                    OpenedPositions[username][newMytrade.SecurityId] = new Pos()
                    {
                        SecId = newMytrade.SecurityId,
                        SecName = secname,
                        Board = newMytrade.Board,
                        PriceStep = sec?.PriceStep ?? 1,
                        PriceStepCost = sec?.PriceStepCost??1,
                        
                    };

                    LogMessageAsync($"Creating new Pos for {username} ");
                }

                var resultAddingTrade = OpenedPositions[username][newMytrade.SecurityId].AddTrade(newMytrade);

                PositionManager.CalculateRealizedPnl(OpenedPositions[username][newMytrade.SecurityId]);

                LogMessageAsync($"Update for {username} sec_id={newMytrade.SecurityId} pos {OpenedPositions[username][newMytrade.SecurityId].PosValue}");

                if (resultAddingTrade != null && resultAddingTrade.Closed)
                {
                    if (!ClosedPositions.ContainsKey(username))
                        ClosedPositions.TryAdd(username, new());

                    var closedpos = OpenedPositions[username][newMytrade.SecurityId];

                    if (!ClosedPositions[username].ContainsKey(newMytrade.SecurityId))
                        ClosedPositions[username][newMytrade.SecurityId] = new List<Pos>();

                    ClosedPositions[username][newMytrade.SecurityId].Add(closedpos);
                    PositionManager.CalculateRealizedPnl(closedpos);

                    closedpos.UnRealizedPnl = 0;
                    closedpos.UnRealizedPnlPoints = 0;

                    if (OpenedPositions[username].TryRemove(newMytrade.SecurityId, out var _))
                    {
                        LogMessageAsync($"Pos Deleted {username} sec_id={newMytrade.SecurityId} ");
                    }

                    //удаляем вообще какие либо позы юзера, потому что потом будет
                    //по этому словарю будем считать прибыль сами
                    try
                    {
                        if (OpenedPositions[username].Values.Count == 0)
                            OpenedPositions.TryRemove(username, out var _);
                    }
                    catch  (Exception ex) // игнор
                    {
                        Console.WriteLine(ex);
                    }
                }

                //добавилось остаточная позиция, как то так... 
                if (resultAddingTrade != null && resultAddingTrade.RestNewTrade != null)
                {
                    ProcessNewMyTrade(resultAddingTrade.RestNewTrade);
                }
            }
            catch (Exception ex)
            {
                LogMessageAsync("Process new My Trade error ->" + ex.Message + ex.StackTrace);
            }
        }

        #endregion MyTrade


        #region Login

        public async Task<LoginInfo> Login(string login, string pass)
        {
            if (!UsersContext.ContainsKey(login))
            {
                return new LoginInfo { errorMessage = "User doesn't exist" };
            }

            var user = UsersContext[login];

            if (!user.CanInput)
            {
                return new LoginInfo { errorMessage = "User access denied" };
            }

            if (user.Password != pass)
            {
                return new LoginInfo { errorMessage = "Incorrect password" };
            }

            var authResponse = JwtProvider.GenerateToken(user);
            user.TimeLastLogin = DateTime.Now;
            user.AccessToken = authResponse.token;
            user.RefreshToken = JwtProvider.GenerateRefreshToken();
            authResponse.refreshToken = user.RefreshToken;
            //Console.WriteLine("Refresh token(1): " + user.RefreshToken);

            return authResponse;
        }


        public (LoginInfo, User?) Login(string token)
        {
            KeyValuePair<string, User> user = UsersContext.FirstOrDefault(u => u.Value.AccessToken == token || u.Value.RefreshToken == token);
            if (user.Key == null || user.Value == null)
            {
                return (new LoginInfo { errorMessage = "User doesn't exist" }, null);
            }

            if (!user.Value.CanInput)
            {
                return (new LoginInfo { errorMessage = "User access denied" }, null);
            }

            var authResponse = JwtProvider.GenerateToken(user.Value);
            authResponse.refreshToken = user.Value.RefreshToken;
            user.Value.TimeLastLogin = DateTime.Now;

            //Console.WriteLine("Refresh token(2): " + user.Value.RefreshToken);

            return (authResponse, user.Value);
        }

        public bool Logout(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token)) return false;

                var user = UsersContext.FirstOrDefault(u => u.Value.AccessToken == token || u.Value.RefreshToken == token);
                if (!string.IsNullOrWhiteSpace(user.Key))
                {
                    user.Value.AccessToken = string.Empty;
                    user.Value.RefreshToken = string.Empty;
                    user.Value.TimeLastLogin = DateTime.MinValue;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        #endregion Login

        #region Order

        /// <summary>
        /// SendOrder on Plaza FORTS MOEX
        /// </summary>
        /// <param name="clientOrder">Order</param>
        /// <param name="userName">Trader</param>
        /// <param name="isRiskManager">true if you are a Risk Manager</param>
        /// <returns></returns>
        public async Task<Order> SendOrder(ClientOrder clientOrder, string userName, bool isRiskManager = false, 
                                                [CallerMemberName] string tag = "",
                                                [CallerFilePath] string sourceFilePath = "",
                                                [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                if (!UsersContext.ContainsKey(userName))
                {
                    return new Order { Error = "Трейдер не найден" };
                }

                var user = UsersContext[userName];

                if (!user.CanInput)
                {
                    OnAttentionMessageAsync(AttentionMessageType.Error, userName, clientOrder.SecID, "Данному трейдеру торговля запрещена.");
                    return new Order { Error = "Данному трейдеру торговля запрещена." };
                }
                if (!plaza.Securities.ContainsKey(clientOrder.SecID))
                {
                    return new Order { Error = "Финансовй инструмент не найден" };
                }
                
                var price = clientOrder?.Price ?? 0;
                var sec = plaza.Securities[clientOrder.SecID];

                Order plazaOrder = clientOrder.Market ?
                   new Order(sec, clientOrder.Side, clientOrder.Volume, plaza.Portfolio.Number, userName) :
                    new Order(sec, clientOrder.Side, clientOrder.Volume, price, plaza.Portfolio.Number, userName);


                if (clientOrder.NumberOrderId != null && clientOrder.NumberOrderId != 0)
                    plazaOrder.NumberUserOrderId = (int)clientOrder.NumberOrderId;

                if (!isRiskManager)
                {
                    var response = riskManager.CanUserPlaceOrder(plazaOrder, user, BoardEnum.FortsMoex);
                    if (!response.Data)
                    {
                        OnAttentionMessageAsync(AttentionMessageType.Error, userName, clientOrder.SecID, response.Message);
                        plazaOrder.State = Order.OrderStateType.Fail;
                        return new Order { Error = response.Message };
                    }
                }

                LogMessageAsync($"Sending Order {userName} price={plazaOrder.PriceOrder} ");

                ServerLogger.Log($"Service Send Order {plazaOrder}, {userName}, {isRiskManager}, {tag}, {sourceFilePath}, {sourceLineNumber}", "SeviceSendOrder");

                await plaza.ExecuteOrderAsync(plazaOrder).ConfigureAwait(false);

                return plazaOrder;
            }
            catch (Exception ex)
            {
                return new Order { Error = ex.Message };
            }
        }

        /// <summary>
        /// Получить все позиции по одному пользователю
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public List<Pos> GetAllPositionsForUser(string userName)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(userName))
                {
                    return new List<Pos>() { };
                }
                var positions = new List<Pos>();
                if (OpenedPositions.TryGetValue(userName, out ConcurrentDictionary<string, Pos>? value))
                {
                    positions = value.Values.ToList();
                }
                if (ClosedPositions.TryGetValue(userName, out ConcurrentDictionary<string, List<Pos>>? value1))
                {
                    value1.Values.Select(p => { positions.AddRange(p); return true; }).Count();
                }
                return positions;
            }
            catch (Exception ex)
            {
                return new List<Pos>() { };
            }
        }

        /// <summary>
        /// Получить открытые позиции для одного пользователя
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public List<Pos> GetOpenPositionsForUser(string userName)
        {
            try
            {
                var positions = new List<Pos>();
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return positions;
                }
                if (OpenedPositions.TryGetValue(userName, out ConcurrentDictionary<string, Pos>? value))
                {
                    positions = value.Values.ToList();
                }
                return positions;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return [];
            }
        }

        /// <summary>
        /// Получить закрытые позиции для одного пользователя
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public List<Pos> GetClosedPositionsForUser(string userName)
        {
            try
            {
                var positions = new List<Pos>();
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return positions;
                }
                if (ClosedPositions.TryGetValue(userName, out ConcurrentDictionary<string, List<Pos>>? value1))
                {
                    value1.Values.Select(p => { positions.AddRange(p); return true; }).Count();
                }
                return positions;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return [];
            }
        }

        public Pos? GetOpenPositionsForUserAndSec(string userName, string secid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return null;
                }
                if (OpenedPositions.TryGetValue(userName, out ConcurrentDictionary<string, Pos>? value))
                {
                    var pos = value.Values.FirstOrDefault(x => x.SecId == secid);
                    return pos;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Получить все позиции 
        /// </summary>
        /// <returns></returns>
        public List<Pos> GetAllPositions()
        {
            try
            {
                List<Pos>? positions = new List<Pos>();
                List<string>? userOpenedPositionsKeys = OpenedPositions.Keys.ToList();

                for (int i = 0; i < userOpenedPositionsKeys.Count; i++)
                {
                    string? userKey = userOpenedPositionsKeys[i];
                    if (OpenedPositions.TryGetValue(userKey, out var pos))
                    {
                        pos.Values.Select(p => { positions.AddRange(p); return true; }).Count();
                    }
                }

                List<string>? userClosedPositionsKeys = ClosedPositions.Keys.ToList();

                for (int i = 0; i < userClosedPositionsKeys.Count; i++)
                {
                    string? userKey = userClosedPositionsKeys[i];
                    if (ClosedPositions.TryGetValue(userKey, out var pos))
                    {
                        pos.Values.Select(p => { positions.AddRange(p); return true; }).Count();
                    }
                }

                return positions;
            }
            catch (Exception ex)
            {
                return new List<Pos>() { };
            }
        }

        /// <summary>
        /// Получить все закрытые позиции 
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, ConcurrentDictionary<string, List<Pos>>>? GetAllClosePositions()
        {
            try
            {
                return ClosedPositions;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Получить все открытые позиции 
        /// ключ 1 - юзер
        /// ключ 2 - sec ID
        /// </summary>
        /// <returns></returns>
        public ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>>? GetAllOpenPositions()
        {
            try
            {
                return OpenedPositions;
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        /// <summary>
        /// Получить все открытые позиции на бирже по портфелю
        /// </summary>
        /// <returns></returns>
        public List<PositionOnBoard> GetBoardPosition()
        {
            var realPositions = RealPositions.Values.ToList();
            realPositions.Select(p =>
            {
                if (string.IsNullOrWhiteSpace(p.SecurityShortName))
                {
                    if (Securities.TryGetValue(p.SecurityId, out var sec))
                    {
                        p.SecurityShortName = sec.ShortName;
                    }
                }
                return true;
            }).Count();

            return realPositions;
        }


        /// <summary>
        /// Close Position User By Security
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="security"></param>
        /// <returns></returns>
        public async Task<ServerResponce<int>> ClosePositionBySecurity(string userName, string? security)
        {
            try
            {
                ///////////////////////////////////////////
                // NEED TO ASK - Why does it use closed position???
                ///////////////////////////////////////////
                var positions = GetAllPositionsForUser(userName).Where(p => p.PosValue != 0).ToList();
                var n = positions == null ? 0 : positions?.Count;

                Console.WriteLine($"ClosePositionBySecurity {n}");

                if (n == 0)
                {
                    return new ServerResponce<int>(0, "Not Open Positions");
                }

                int pc = 0;
                for (int i = 0; i < n; i++)
                {
                    var p = positions[i];
                    if (p.PosValue != 0 && (string.IsNullOrWhiteSpace(security) || p.SecId == security || p.SecName == security || Securities[p.SecId].ShortName == security))
                    {
                        ClientOrder clientOrder = new ClientOrder()
                        {
                            Market = true,
                            SecID = p.SecId,
                            Volume = (int)p.CurrentPos,
                            Side = p.Side == Side.Buy ? Side.Sell : Side.Buy
                        };
                        pc++;

                        Console.WriteLine($"ClosePositionBySecurity-SendOrder {clientOrder.Volume} {clientOrder.Side}");
                        await SendOrder(clientOrder, userName,true);
                        Console.WriteLine($"ClosePositionBySecurity-SentOrder");
                    }
                }

                Console.WriteLine($"Positions closed");
                return new ServerResponce<int>(pc, "Positions closed");


            }
            catch (Exception ex)
            {
                return new ServerResponce<int>(-1, ex.Message);
            }
        }

        /// <summary>
        /// Close All User Position
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public async Task<ServerResponce<int>> CloseAllPosition(string userName)
        {
            try
            {
                var positions = GetAllPositionsForUser(userName).Where(p => p.PosValue != 0).ToList();

                if (positions.Count() == 0)
                {
                    return new ServerResponce<int>(0, "Not Open Positions");
                }

                int pc = 0;
                for (int i = 0; i < positions.Count(); i++)
                {
                    var p = positions[i];
                    if (p.PosValue != 0)
                    {
                        ClientOrder clientOrder = new ClientOrder()
                        {
                            Market = true,
                            SecID = p.SecId,
                            Volume = (int)p.CurrentPos,
                            Side = p.Side == Side.Buy ? Side.Sell : Side.Buy
                        };
                        pc++;
                        await SendOrder(clientOrder, userName, true);
                    }
                }
                
                return new ServerResponce<int>(pc, "Positions closed");


            }
            catch (Exception ex)
            {
                return new ServerResponce<int>(-1, ex.Message);
            }
        }


        private bool plazaAllOrderLoaded = false;

        private void Plaza_OrderLoadedEvent()
        {
            OrdersUpdate();
            plazaAllOrderLoaded = true;
        }

        private void Plaza_OrderChangedEvent(Order order, string? reason)
        {
            Console.WriteLine($"Plaza_OrderChangedEvent {order.PriceOrder} {order.Volume} {order.VolumeExecuted}");
            if (!plazaAllOrderLoaded && !plazaOptions.Simulation)
            {
                // TODO - TEST
                return;
            }
            AttentionMessageType messageType = AttentionMessageType.Info;

            if (order.State == Order.OrderStateType.Fail)
            {
                messageType = AttentionMessageType.Error;
                OnAttentionMessageAsync(messageType, order.Comment, order.SecurityId, reason);
            }

            Console.WriteLine($"{DateTime.Now.ToLongTimeString()}; {order}; {reason}");
            var username = order.Comment;
            if (!Orders.ContainsKey(username))
            {
                Orders[username] = new ConcurrentDictionary<int, Order>();
            }
            Orders[username][order.NumberUserOrderId] = order;
            if (UsersContext.ContainsKey(username) && UsersContext[username].PrivateOrderEventsForUsers != null)
            {
                foreach (var actionOrderEvent in UsersContext[username].PrivateOrderEventsForUsers)
                {
                    Console.WriteLine($"Plaza_OrderChangedEvent-Notify {order.PriceOrder} {order.Volume} {order.VolumeExecuted}");
                    actionOrderEvent.Value?.Invoke(new List<Order>() { order });
                }
            }
        }

        private void OrdersUpdate()
        {
            var orders = plaza.Orders;  //key = NumberUserOrderId
            var allOrdersUsername = orders.Select(o => o.Value.Comment).Distinct().ToList();
            for (int i = 0; i < allOrdersUsername.Count; i++)
            {
                var username = allOrdersUsername[i];
                if (!Orders.ContainsKey(username))
                {
                    Orders[username] = new ConcurrentDictionary<int, Order>();
                }
                var ordersUser = orders.Values.Where(o => o.Comment == username).ToList();

                ordersUser.Select(o => Orders[username].AddOrUpdate(o.NumberUserOrderId, o, (NumberUserOrderId, uo) => { uo.State = o.State; return uo; })).Count();

                if (UsersContext.ContainsKey(username) && UsersContext[username].PrivateOrderEventsForUsers != null)
                {
                    foreach (var actionOrderEvent in UsersContext[username].PrivateOrderEventsForUsers)
                    {
                        actionOrderEvent.Value?.Invoke(ordersUser.Where(o => o.State == Order.OrderStateType.Active || o.State == Order.OrderStateType.Partial).ToList());
                    }
                }
                // check lost actice orders
                var lostOrders = Orders[username].Values.Where((o) =>
                {
                    if ((o.State == Order.OrderStateType.Active
                        || o.State == Order.OrderStateType.Pending
                        || o.State == Order.OrderStateType.Partial)
                        && !orders.ContainsKey(o.NumberUserOrderId))
                    {
                        o.State = Order.OrderStateType.None;
                        return true;
                    }
                    return false;
                }).ToList();
                Debug.WriteLine($"Check lost actice orders {lostOrders.Count()}");
            }
        }

        /// <summary>
        /// Get all order for user
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public ServerResponce<List<Order>?> GetOrdersForUser(string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return new ServerResponce<List<Order>?>([], SERVER_WARNING_USER_NOT_LOGGED_IN); ;
                }
                if (!plazaAllOrderLoaded)
                {
                    return new ServerResponce<List<Order>?>([], SERVER_WARNING_LOADING_ORDERS);
                }
                if (!Orders.ContainsKey(userName))
                {
                    return new ServerResponce<List<Order>?>([], SERVER_ERROR_NO_USER);
                }

                var result = Orders[userName].Values.ToList();
                return new ServerResponce<List<Order>?>(result ?? []);
            }
            catch (Exception ex)
            {
                return new ServerResponce<List<Order>?>(null, ex.Message);
            }
        }

        /// <summary>
        /// Get active orders for user
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public ServerResponce<List<Order>?> GetActiveOrdersForUser(string userName)
        {
            try
            {
                // TODO - test code
                if (!plazaAllOrderLoaded)
                {
                    return new ServerResponce<List<Order>?>(null, SERVER_WARNING_LOADING_ORDERS);
                }

                if (!Orders.TryGetValue(userName, out ConcurrentDictionary<int, Order>? orderDict))
                {
                    return new ServerResponce<List<Order>?>(null);
                }

                var result = orderDict.Values.Where(o => o.State == Order.OrderStateType.Active || o.State == Order.OrderStateType.Partial).ToList();
                return new ServerResponce<List<Order>?>(result);
            }
            catch (Exception ex)
            {
                return new ServerResponce<List<Order>?>(null, ex.Message);
            }
        }

        /// <summary>
        /// Get canceled orders for user
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public ServerResponce<List<Order>?> GetCanceledOrdersForUser(string userName)
        {
            try
            {
                // TODO - test code
                if (!plazaAllOrderLoaded)
                {
                    return new ServerResponce<List<Order>?>(null, SERVER_WARNING_LOADING_ORDERS);
                }

                if (!Orders.TryGetValue(userName, out ConcurrentDictionary<int, Order>? orderDict))
                {
                    return new ServerResponce<List<Order>?>(null);
                }

                var result = orderDict.Values.Where(o => o.State == Order.OrderStateType.Cancel || o.State == Order.OrderStateType.Fail).ToList();
                return new ServerResponce<List<Order>?>(result);
            }
            catch (Exception ex)
            {
                return new ServerResponce<List<Order>?>(null, ex.Message);
            }
        }

        /// <summary>
        /// Get executed orders for user
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public ServerResponce<List<Order>?> GetExecutedOrdersForUser(string userName)
        {
            try
            {
                // TODO - test code
                if (!plazaAllOrderLoaded)
                {
                    return new ServerResponce<List<Order>?>(null, SERVER_WARNING_LOADING_ORDERS);
                }

                if (!Orders.TryGetValue(userName, out ConcurrentDictionary<int, Order>? orderDict))
                {
                    return new ServerResponce<List<Order>?>(null);
                }

                var result = orderDict.Values.Where(o => o.State == Order.OrderStateType.Done).ToList();
                return new ServerResponce<List<Order>?>(result);
            }
            catch (Exception ex)
            {
                return new ServerResponce<List<Order>?>(null, ex.Message);
            }
        }

        /// <summary>
        /// Отменить один ордер
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public ServerResponce<bool> CancelOrder(string userName, int orderId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName) || !UsersContext.ContainsKey(userName))
                {
                    return new ServerResponce<bool>(false, "User not found");
                }

                //plaza.CancelOrder(order.NumberUserOrderId);
                plaza.CancelOrder(orderId);
                return new ServerResponce<bool>(true);
            }
            catch (Exception ex)
            {
                return new ServerResponce<bool>(false, ex.Message);
            }
        }

        /// <summary>
        /// Отменить все ордера пользователя
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public ServerResponce<int> CancelAllUserOrder(string userName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userName))
                {
                    return new ServerResponce<int>(0, "User not found");
                }

                if (!UsersContext.ContainsKey(userName))
                    return new ServerResponce<int>(0, "User not found");

                var orders = plaza.Orders;
                var count = orders.Where(pair =>
                {
                    var o = pair.Value;
                    if (o.Comment == userName && (o.State == Order.OrderStateType.Active
                                                 || o.State == Order.OrderStateType.Pending
                                                 || o.State == Order.OrderStateType.Partial))
                    {
                        plaza.CancelOrder(o.NumberUserOrderId);
                        return true;
                    }
                    return false;

                }).Count();

                
                return new ServerResponce<int>(count, "Orders canceled");

            }
            catch (Exception ex)
            {
                return new ServerResponce<int>(0, ex.Message);
            }
        }


        #endregion Order

        #region FinanceResult

        public List<FinanceResult> GetFinanceResult(string userName)
        {
            if (userName == null || !UsersContext.ContainsKey(userName))
            {
                return new List<FinanceResult>();
            }
            List<FinanceResult> r = riskManager.GetFinanceResult(userName);
            return r;
        }


        public List<FinanceResult> GetAllFinanceResult()
        {
            List<FinanceResult> r = riskManager.GetFinanceResult();
            return r;
        }



        #endregion

        #region AttentionMessage

        /// <summary>
        /// Activate an event with a message to the user
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="message"></param>
        /// <param name="userName"
        public async Task OnAttentionMessageAsync(AttentionMessageType messageType, string userName, string secId,  string message)
        {
            try
            {
                await Task.Run(() => AttentionMessageForTraderEvent?.Invoke(messageType, userName, secId, message)).ConfigureAwait(false);
                ServerLogger.Log($"{userName}; {message}", "AttentionMessage");
            }
            catch (Exception ex)
            {
                ServerLogger.Log(ex.Message);
            }
        }



        /// <summary>
        /// Trader event with attention grabbing message
        /// string 1 - username,string 2  - SecId,  string 3 - message
        /// 
        /// </summary>
        public event Action<AttentionMessageType, string, string, string> AttentionMessageForTraderEvent;

        #endregion AttentionMessage

        #region Dispose

        public void Dispose()
        {
            try
            {

                //TODO: переписать этот кошмар... 

                if (plaza != null)
                {
                    plaza.Stop();
                    plaza.Dispose();
                }
                serviceDB?.Close();
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {

            }
        }

        #endregion Dispose
    }

    #region Enum

    /// <summary>
    /// AttentionMessageType
    /// </summary>
    public enum AttentionMessageType
    {
        CriticalError,     // Red
        Error,  // Black
        Info,    // Grey
        IntermediatePriority    //blue
    }

    #endregion Enum
}


