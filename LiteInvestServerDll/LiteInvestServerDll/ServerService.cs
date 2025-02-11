namespace LiteInvestServerDll;

using PlazaEngine.Engine;

using System.Collections.Concurrent;
using System.Text.Json;
using LiteInvest.Entity.Helpers;
using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ServerEntity;
using LiteInvestServerDll.Options;

public class ServerService
{
    ConcurrentDictionary<string, SecurityApi> Securities = new();
    ///База юзеров 
    ConcurrentDictionary<string, User> UsersContext = new ConcurrentDictionary<string, User>();
    //База ордеров по юзеру
    ConcurrentDictionary<string, ConcurrentDictionary<string, Order>> Orders = new();
    //База ордеров по юзеру
    ConcurrentDictionary<string, ConcurrentDictionary<string, Trade>> Trades = new();
    //ключ - юзер
    //ключ 2 - sec ID
    ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>> OpenedPositions = new();
    ConcurrentDictionary<string, ConcurrentDictionary<string, List<Pos>>> ClosedPositions = new();

    //это словарь наоборот
    // ключ - sec_id
    // далее по юзерам идет разбивка. 
    ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>> PositionsForProfit = new();

    ConcurrentDictionary<string, PositionOnBoard> RealPositions = new();

    PlazaConnector plaza = null;

    object savelocker = new object();

    static string data = "C:\\ServerData";
    DirectoryInfo directoryInfo = new DirectoryInfo(data);

    PlazaOptions plazasimulation = new PlazaOptions(false);


    string securitiesBdName = $"{data}\\securities.xml";
    string userdBdName = $"{data}\\users.xml";
    string ordersBdName = $"{data}\\orders.xml";
    string tradesBdName = $"{data}\\trades.xml";

    string openedpositionsdbname = $"{data}\\openedpositions.xml";


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


    static JwtOptions jwtOptions { get; set; } = new JwtOptions("T6zPWbOrHXVEbOgJtZr91GqTLSAMPy52Y88+VD3RG90wrFO0bH2M/k5JAkDdvBB1", 24);
    public JwtProvider jwtProvider { get; set; } = new JwtProvider(jwtOptions);

    ///PLAZA WORDER MAIN

    /* // Тест сохранения 
    Orders.TryAdd("1", new ConcurrentDictionary<string, Order>());
    Orders["1"] = new ConcurrentDictionary<string, Order>();
    Orders["1"]["2"] = new Order();

    try
    {
        Helper.SaveXml(Orders, ordersBdName);
    }
    catch (Exception ex)
    {

    }*/

    async void LogMessageAsync(string message)
    {
        var dt = DateTime.Now.ToString("H:mm:ss.fff");
        await Console.Out.WriteLineAsync($"{dt} {message}").ConfigureAwait(false);
    }

    public ServerService()
    {

        if (!Directory.Exists(data))
            System.IO.Directory.CreateDirectory(directoryInfo.ToString());


        //TODO: Перепишу всю эту часть, когда сделаю нормальное сохранение. 

        if (File.Exists(securitiesBdName))
            Securities = Helper.ReadXml<ConcurrentDictionary<string, SecurityApi>>(securitiesBdName);

        if (File.Exists(userdBdName))
            UsersContext = Helper.ReadXml<ConcurrentDictionary<string, User>>(userdBdName);

        if (File.Exists(ordersBdName))
            Orders = Helper.ReadXml<ConcurrentDictionary<string, ConcurrentDictionary<string, Order>>>(ordersBdName);


        //Trades = Helper.ReadXml<ConcurrentDictionary<string, ConcurrentDictionary<string, Trade>>>(tradesBdName);

        if (File.Exists(openedpositionsdbname))
            OpenedPositions = Helper.ReadXml<ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>>>(openedpositionsdbname);



        //тестовое сохраннение 
        //OpenedPositions = new ConcurrentDictionary<string, ConcurrentDictionary<string, Pos>>();
        //OpenedPositions.TryAdd("somekey", new ConcurrentDictionary<string, Pos>());
        //OpenedPositions["somekey"].TryAdd("pos1", new Pos() { secid = "1", secName = "pa" });


        //ClosedPositions = Helper.ReadXml<ConcurrentDictionary<string, ConcurrentDictionary<string, List<Pos>>>>($"{data}\\{nameof(ClosedPositions)}.xml");

        string admin = "adminadminov";

        if (!UsersContext.ContainsKey(admin))
            UsersContext.TryAdd(admin, new User(admin, "adminPass#1R") { Admin = true, CanTrade = false });

        Console.WriteLine($"simulation PLAZA {plazasimulation.Simulation}");

        plaza = new PlazaConnector("02mMLX144T2yxnfzEUrCjUKzXKciQKJ", plazasimulation.Simulation, testTrading: false, appname: "osaApplication")
        {
            Limit = 30,
            LoadTicksFromStart = false,
        };

        plaza.UpdatePosition += pos =>
        {
            RealPositions[pos.SecurityId] = pos;
            LogMessageAsync($"New Pos Info sec_id={pos.SecurityId} {pos.XPosValueCurrent} ");
        };

        plaza.TicksLoadedEvent += () => LogMessageAsync($"Ticks Ready To Go!");



        plaza.NewMyTradeEvent += ProcessNewMyTrade;
        plaza.OrderLoadedEvent += () => LogMessageAsync($"Orders Loaded!");

        plaza.OrderChangedEvent += async (plazaOrder, reason) =>
        {

            if (plazaOrder == null)
                return;

            var username = plazaOrder.Comment;

            try
            {
                if (!UsersContext.ContainsKey(username) || username == string.Empty)
                    return;

                if (!Orders.ContainsKey(username))
                    Orders.TryAdd(username, new());

                Orders[username][plazaOrder.ExchangeOrderId] = plazaOrder;
                LogMessageAsync($"Order add to DB {plazaOrder} id={plazaOrder.ExchangeOrderId}");
            }
            catch (Exception ex)
            {
                LogMessageAsync($"Problems adding order {ex.Message}");
            }

            //-----------------------------
          

            LogMessageAsync($"New Order user ({username}) {plazaOrder.State} number = {plazaOrder.ExchangeOrderId} error ={reason}");

         
            //далее по подпискам на сокеты мы должны отправить инфу о новом состоянии юзера..
        };

        plaza.MarketDepthChangeEvent += orderbook =>
        {

            if (orderbook == null)
                return;

            try
            {

            
            }
            catch (Exception ex)
            {
                LogMessageAsync("Order Book WebSockets error->" + ex.Message);
            }
        };


        Helper.CreateTimerAndStart(CalculatePnls, 5000);
        Helper.CreateTimerAndStart(SaveDb, 5000);

        plaza.UpdateSecurity += sec =>
        {
            Securities[sec.Id] = new SecurityApi()
            {
                id = sec.Id,
                ShortName = sec.ShortName,
                ClassCode = sec.ClassCode,
                Isin = sec.Name,
                FullName = sec.FullName,
                Type = sec.Type.ToString(),
                Lot = sec.Lot,
                PriceStep = sec.PriceStep,
                Decimals = sec.Decimals,
                PriceLimitHigh = sec.PriceLimitHigh,
                PriceLimitLow = sec.PriceLimitLow,
            };
        };


        if (plaza.Emulation)
        {
            foreach (var sec in Securities)
            {
                plaza.Securities.TryAdd(sec.Key, new Security(sec.Value.ShortName, sec.Value.FullName, SecurityType.Futures, sec.Value.ClassCode, sec.Value.Lot)
                {
                    Id = sec.Value.id,
                    Name = "Emulation Security",
                    ShortName = sec.Value.ShortName,
                    PriceStep = sec.Value.PriceStep,
                    PriceLimitLow = sec.Value.PriceLimitHigh,
                    PriceLimitHigh = sec.Value.PriceLimitLow,
                    PriceStepCost = 1,

                });
            }
        }

        plaza.Connect();
    }

    #region HelperMethods

    void CalculatePnls()
    {
        try
        {
            if (plaza == null || plaza.ticksplaza == null)
                return;

            foreach (var pos in PositionsForProfit)
            {

                if (!plaza.ticksplaza.AllTicks.ContainsKey(pos.Key))
                    continue;

                var tickPrice = plaza.ticksplaza.AllTicks[pos.Key].Price;

                foreach (var userPos in pos.Value)
                {
                    if (userPos.Value == null)
                        continue;

                    userPos.Value.CalculateUnrealizedPnl(tickPrice);
                    // LogMessageAsync($"PNL updated for {userPos.Key} sec_id ={pos.Key}");
                }
            }
        }
        catch (Exception ex)
        {
            LogMessageAsync("CalculatePnls error->" + ex.Message);
        }
    }

    void SaveDb()
    {
        lock (savelocker)
        {
            try
            {
                Helper.SaveXml(Securities, securitiesBdName);
                //Helper.SaveXml(Trades, tradesBdName);
                Helper.SaveXml(Orders, ordersBdName);
                Helper.SaveXml(UsersContext, userdBdName);
                Helper.SaveXml(OpenedPositions, openedpositionsdbname);
                //Helper.SaveXml(ClosedPositions, $"{data}\\{nameof(ClosedPositions)}.xml");
            }
            catch (Exception ex)
            {
                LogMessageAsync("DB ERROR ->" + ex.Message);
            }
        }
    }

    void ProcessNewMyTrade(MyTrade newMytrade)
    {
        try
        {
            var username = newMytrade.Comment;

            if (!UsersContext.ContainsKey(username))
                return;

            if (!OpenedPositions.ContainsKey(username))
                OpenedPositions.TryAdd(username, new ConcurrentDictionary<string, Pos>());

            OpenedPositions[username].TryGetValue(newMytrade.SecurityId, out var posvalue);

            if (posvalue == null)
            {
                var secname = Securities.ContainsKey(newMytrade.SecurityId) ? Securities[newMytrade.SecurityId].Isin : "Empty";

                OpenedPositions[username][newMytrade.SecurityId] = new Pos()
                {
                    secid = newMytrade.SecurityId,
                    secName = secname
                };
                LogMessageAsync($"Creating new Pos for {username} ");
            }

            var resultAddingTrade = OpenedPositions[username][newMytrade.SecurityId].AddTrade(newMytrade);
            LogMessageAsync($"Update for {username} sec_id={newMytrade.SecurityId} pos {OpenedPositions[username][newMytrade.SecurityId].PosValue}");

            //-------------------------- Позиции для подсчета прибыли ------------------------//

            PositionsForProfit.TryGetValue(newMytrade.SecurityId, out var valuekeypair);
            if (valuekeypair == null) PositionsForProfit[newMytrade.SecurityId] = new();
            PositionsForProfit[newMytrade.SecurityId][username] = OpenedPositions[username][newMytrade.SecurityId];

            //---------------------------Позиции для подсчета прибыли -----------------------//

            if (resultAddingTrade != null && resultAddingTrade.Closed)
            {
                if (!ClosedPositions.ContainsKey(username))
                    ClosedPositions[username] = new();

                var closedpos = OpenedPositions[username][newMytrade.SecurityId];

                if (!ClosedPositions[username].ContainsKey(newMytrade.SecurityId))
                    ClosedPositions[username][newMytrade.SecurityId] = new List<Pos>();

                ClosedPositions[username][newMytrade.SecurityId].Add(closedpos);

                if (OpenedPositions[username].TryRemove(newMytrade.SecurityId, out var _))
                {
                    LogMessageAsync($"Pos Deleted {username} sec_id={newMytrade.SecurityId} ");
                }

                //удаляем вообще какие либо позы юзера, потому что потом будет
                //по этому словарю будем считать прибыль сами
                if (OpenedPositions[username].Values.Count == 0)
                    OpenedPositions.TryRemove(username, out var _);

                if (PositionsForProfit.ContainsKey(newMytrade.SecurityId) && PositionsForProfit[newMytrade.SecurityId].ContainsKey(username))
                {
                    PositionsForProfit[newMytrade.SecurityId].TryRemove(username, out var _);

                    //удаляем вообще все намеки на остатки... 
                    if (PositionsForProfit[newMytrade.SecurityId].Values.Count == 0)
                        PositionsForProfit.TryRemove(newMytrade.SecurityId, out _);
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
            LogMessageAsync("Process new My Trade error ->" + ex.Message);
        }
    }
    #endregion HelperMethods

    #region mapMethods

    public class ReturnType<T>
    {
        public T Data { get; set; }
        public string Message { get; set; }

        public ReturnType(T data, string message = "")
        {
            Data = data;
            Message = message;
        }
    }

    public ReturnType<bool> Register(UserCredentials userCredentials)
    {
        if (UsersContext.ContainsKey(userCredentials.LoginEmail))
        {
            return new ReturnType<bool>(false, "User already exists");
        }

        UsersContext.TryAdd(userCredentials.LoginEmail, new User(userCredentials.LoginEmail, userCredentials.Password)
        {
            Limit = 20000,
            CanTrade = true,
            Admin = false,
        });

        return new ReturnType<bool>(true);
    }

    public ReturnType<LoginInfo> Login(string login, string pass)
    {
        if (!UsersContext.ContainsKey(login))
        {
            return new ReturnType<LoginInfo>(null, "User doesn't exist");
        }

        var user = UsersContext[login];

        if (user.Password != pass)
        {
            return new ReturnType<LoginInfo>(null, "Incorrect password");
        }

        var authResponse = jwtProvider.GenerateToken(user);

        return new ReturnType<LoginInfo>(authResponse);
    }

    public ReturnType<bool> Logout()
    {
        try
        {

        }
        catch (Exception ex)
        {
            return new ReturnType<bool>(false, ex.Message);
        }
        return new ReturnType<bool>(true);
    }

    public ReturnType<List<KeyValuePair<string, User>>> RiskManagerGetUsers(string userName)
    {
        if (!UsersContext.ContainsKey(userName))
            return new ReturnType<List<KeyValuePair<string, User>>>(null, "User not found");

        if (!UsersContext[userName].Admin)
            return new ReturnType<List<KeyValuePair<string, User>>>(null, "No Admin rights");

        try
        {
            var usersList = UsersContext.ToList().Where(a => !a.Value.Admin).ToList();
            return new ReturnType<List<KeyValuePair<string, User>>>(usersList);
        }
        catch (Exception ex)
        {
            return new ReturnType<List<KeyValuePair<string, User>>>(null, ex.Message);
        }
    }

    public ReturnType<bool> RiskManagerChangeLimitUser(string userName, string usernametochange, decimal newlimit)
    {
        if (!UsersContext.ContainsKey(userName))
            return new ReturnType<bool>(false, "User not found");

        if (!UsersContext[userName].Admin)
            return new ReturnType<bool>(false, "No Admin rights");

        try
        {
            UsersContext[usernametochange].Limit = newlimit;
            return new ReturnType<bool>(true);
        }
        catch (Exception ex)
        {
            return new ReturnType<bool>(false, ex.Message);
        }
    }

    public ReturnType<bool> RiskManagerCanTrade(string userName, string usernametochange, bool cantrade)
    {
        if (!UsersContext.ContainsKey(userName))
            return new ReturnType<bool>(false, "User not found");

        if (!UsersContext[userName].Admin)
            return new ReturnType<bool>(false, "No Admin rights");

        if (UsersContext[usernametochange].Admin)
        {
            return new ReturnType<bool>(false, "You cannot change settings for admin to trade");
        }

        try
        {
            UsersContext[usernametochange].CanTrade = cantrade;
            return new ReturnType<bool>(true);
        }
        catch (Exception ex)
        {
            return new ReturnType<bool>(false, ex.Message);
        }
    }

    public ReturnType<bool> RiskManagerCloseAllPositions(string userName)
    {
        if (!UsersContext.ContainsKey(userName))
            return new ReturnType<bool>(false, "User not found");

        if (!UsersContext[userName].Admin)
            return new ReturnType<bool>(false, "No Admin rights");

        if (plaza == null)
            return new ReturnType<bool>(false, "Plaza Not Ready");

        try
        {
            foreach (var pos in RealPositions.Values)
            {
                var sec = plaza.Securities[pos.SecurityId];
                var order = new Order(sec, pos.XPosValueCurrent > 0 ? Side.Sell : Side.Buy, pos.XPosValueCurrent, plaza.Portfolio.Number, userName);
                LogMessageAsync("Sending close orders of positions " + order.ToString());
            }
            return new ReturnType<bool>(true);
        }
        catch (Exception ex)
        {
            return new ReturnType<bool>(false, ex.Message);
        }
    }

    public async Task<ReturnType<Order>> SendOrder(ClientOrder clientOrder, string userName)
    {
        try
        {
            if (!UsersContext.ContainsKey(userName))
                return new ReturnType<Order>(null, "User not found");

            var user = UsersContext[userName];

            if (!user.CanTrade)
                return new ReturnType<Order>(null, "User cannot trade!");

            if (!plaza.Securities.ContainsKey(clientOrder.SecID))
                return new ReturnType<Order>(null, "No Security Id Found");

            var price = (decimal)clientOrder.Price;
            var sec = plaza.Securities[clientOrder.SecID];

            Order plazaOrder = clientOrder.Market ?
               new Order(sec, clientOrder.Side, clientOrder.Volume, plaza.Portfolio.Number, userName) :
                new Order(sec, clientOrder.Side, clientOrder.Volume, price, plaza.Portfolio.Number, userName);

            if (clientOrder.NumberOrderId != null && clientOrder.NumberOrderId != 0)
                plazaOrder.NumberUserOrderId = (int)clientOrder.NumberOrderId;

            LogMessageAsync($"Sending Order {userName} price={plazaOrder.PriceOrder} ");

            await plaza.ExecuteOrderAsync(plazaOrder).ConfigureAwait(false);

            return new ReturnType<Order>(plazaOrder);
        }
        catch (Exception ex)
        {
            return new ReturnType<Order>(null, ex.Message);
        }
    }

    public ReturnType<List<Pos>> GetOpenPositions(string userName)
    {
        try
        {
            if (!OpenedPositions.ContainsKey(userName))
                return new ReturnType<List<Pos>>(new List<Pos>(), "");

            var positions = OpenedPositions[userName].Values.ToList();
            return new ReturnType<List<Pos>>(positions);
        }
        catch (Exception ex)
        {
            return new ReturnType<List<Pos>>(new List<Pos>(), ex.Message);
        }
    }

    public ReturnType<bool> OpenInstrument(string userName, SecurityApi sec)
    {
        try
        {
            if (UsersContext[userName].OpenedInstruments == null)
                UsersContext[userName].OpenedInstruments = new List<SecurityApi>();

            UsersContext[userName].OpenedInstruments.Add(sec);
            return new ReturnType<bool>(true, $"Added {sec.id}");
        }
        catch (Exception ex)
        {
            return new ReturnType<bool>(false, ex.Message);
        }
    }

    public ReturnType<List<SecurityApi>> GetUserInstruments(string userName)
    {
        try
        {
            return new ReturnType<List<SecurityApi>>(UsersContext[userName].OpenedInstruments ?? new List<SecurityApi>());
        }
        catch (Exception ex)
        {
            return new ReturnType<List<SecurityApi>>(null, ex.Message);
        }
    }

    public ReturnType<bool> CloseInstrument(string userName, SecurityApi sec)
    {
        try
        {
            if (UsersContext[userName].OpenedInstruments == null || UsersContext[userName].OpenedInstruments.Count == 0)
                return new ReturnType<bool>(true, "");

            var secFound = UsersContext[userName].OpenedInstruments.FirstOrDefault(s => s.SpecialHash == sec.SpecialHash);

            if (secFound != null)
                UsersContext[userName].OpenedInstruments.Remove(sec);

            return new ReturnType<bool>(true);
        }
        catch (Exception ex)
        {
            return new ReturnType<bool>(false, ex.Message);
        }
    }

    public ReturnType<List<Order>> GetOrders(string userName)
    {
        try
        {
            if (!Orders.ContainsKey(userName))
                return new ReturnType<List<Order>>(new List<Order>());

            var result = Orders[userName].Values.Where(o => o.State == Order.OrderStateType.Activ || o.State == Order.OrderStateType.Partial).ToList();
            return new ReturnType<List<Order>>(result ?? new List<Order>());
        }
        catch (Exception ex)
        {
            return new ReturnType<List<Order>>(null, ex.Message);
        }
    }

    public ReturnType<bool> CancelOrder(string userName, Order order)
    {
        try
        {
            if (!UsersContext.ContainsKey(userName))
                return new ReturnType<bool>(false, "User not found");

            plaza.CancelOrder(order.NumberUserOrderId);
            return new ReturnType<bool>(true);
        }
        catch (Exception ex)
        {
            return new ReturnType<bool>(false, ex.Message);
        }
    }

    public ReturnType<List<SecurityApi>> GetAllSecurities()
    {
        if (Securities == null || Securities.Count == 0)
            return new ReturnType<List<SecurityApi>>(new List<SecurityApi>(), "No Security");

        return new ReturnType<List<SecurityApi>>(Securities.Values.OrderBy(s => s.ShortName).ToList());
    }

    #endregion mapMethods

    public void ServerServiceDipose()
    {
        try
        {

            //TODO: переписать этот кошмар... 

            SaveDb();

            if (plaza != null)
                plaza.Dispose();
        }
        catch (Exception ex)
        {

        }
    }
}
