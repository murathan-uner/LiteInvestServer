namespace LiteInvestServerDll;

using PlazaEngine.Engine;

using System.Collections.Concurrent;
using System.Text.Json;
using LiteInvest.Entity.Helpers;
using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ServerEntity;
using LiteInvest.Entity.Server2;
using System.Runtime.InteropServices;

public class ServerService
{

    public bool crypto { get; set; } = false;

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
        string basicuser = "samujan1@yandex.ru";

        if (!UsersContext.ContainsKey(admin))
            UsersContext.TryAdd(admin, new User(admin, "adminPass#1R") { Admin = true, CanTrade = false });

        if (!UsersContext.ContainsKey(basicuser))
            UsersContext.TryAdd(basicuser, new User(basicuser, "pass2") { Admin = false, CanTrade = true });

       

   
    }


    private bool simulation { get; set; }

    public void Start(bool _simulation)
    {
        simulation = _simulation;

		Console.WriteLine($"simulation PLAZA {_simulation}");

		plaza = new PlazaConnector("02mMLX144T2yxnfzEUrCjUKzXKciQKJ", _simulation, testTrading: false, appname: "osaApplication")
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


				var user = UsersContext[username];
				foreach (var orderaction in user.PrivateOrderEventsForUsers.Values)
				{
					orderaction?.Invoke(plazaOrder);
				}

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

		plaza.NewTickCollectionEvent += ticksDictionary =>
		{
			//NOTE: Проще проверить все тики
			//Или из подписки найти обновленные тики. 
			//вопрос.. блять

			//-----------------------------
			//TODO: Рефакторить!

			if (ticksDictionary == null)
				return;


			foreach (var tick in ticksDictionary)
			{
				// if(tick.Value.Count!=0)
				// LogMessageAsync($"tiks arrive {tick.Key} count = {tick.Value.Count()} priceFirst = {tick.Value.First().Price}");
			}

			try
			{




				//-----------------------------

				//проверяем всех наших подписантов
				foreach (var secIdsubcription in SubscriptionsForTicks)
				{
					//LogMessageAsync($"sec {secIdsubcription.Key}");
					//в тиках есть тики, которые мы должны отправить

					//

					if (ticksDictionary.ContainsKey(secIdsubcription.Key) && ticksDictionary[secIdsubcription.Key].Count != 0)
					{

						var ticks = ticksDictionary[secIdsubcription.Key];


						foreach (var action in secIdsubcription.Value)
						{
							//TODO: временная заплатка

							// LogMessageAsync($"{socket.Key} Sending pack of ticks");
							action.Value?.Invoke(ticks);

						}


					}
				}
			}
			catch (Exception ex)
			{
				LogMessageAsync($"Problems with websocket TICKS {ex.Message}");
			}
		};

		plaza.MarketDepthChangeEvent += orderbook =>
		{

			if (orderbook == null)
				return;

			try
			{
				if (SubscriptionsForOrderBook.ContainsKey(orderbook.SecurityId)
				&& SubscriptionsForOrderBook[orderbook.SecurityId] != null
				&& SubscriptionsForOrderBook[orderbook.SecurityId].Count != 0)
				{
					foreach (var subscription in SubscriptionsForOrderBook[orderbook.SecurityId].Values)
					{
						subscription?.Invoke(orderbook);
					}
				}

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


    #region Подписки(взамен сокетов)

    /// <summary>
    /// ключ - айди инструмента
    /// следющий ключ - хеш
    /// (есть проблемы, если наш юзер отклбчится как то криво, мы не сможем удалить это)...
    /// </summary>
    ConcurrentDictionary<string,  ConcurrentDictionary<string,Action<MarketDepth>>> SubscriptionsForOrderBook { get; set; } = new();
	ConcurrentDictionary<string,  ConcurrentDictionary<string, Action<List<Trade>>>> SubscriptionsForTicks { get; set; } = new();



	/// <summary>
	/// TODO: надо сделать проверку ключа.
	/// </summary>
	public async Task<SubscriptionAnswer> SubscribeForPrivateOrders(string username, Action<Order> action)
    {
        if (!UsersContext.ContainsKey(username))
            return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

        string guid = Guid.NewGuid().ToString();
        var result = UsersContext[username].PrivateOrderEventsForUsers.TryAdd(guid, action);

        return new SubscriptionAnswer()
        {
            Success = result,
            Id = guid
        };

    }

	public async Task<SubscriptionAnswer> RemoveSubscribtionForPrivateOrders(string username, string id)
	{
		if (!UsersContext.ContainsKey(username))
			return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

        if (!UsersContext[username].PrivateOrderEventsForUsers.ContainsKey(id))
			return new SubscriptionAnswer { Success = true, ErrorMessage = "No Such Subscription", Id = id };

		var result = UsersContext[username].PrivateOrderEventsForUsers.TryRemove(id,out var _);

		return new SubscriptionAnswer()
		{
			Success = result,
			Id = id
		};

	}

    /// <summary>
    /// Проблема этого метода, что секьюритис самой плазы долго получаем... 
    /// </summary>
    /// <param name="secid"></param>
    /// <param name="action"></param>
    /// <returns></returns>
	public async Task<SubscriptionAnswer> SubscribeForOrderBook(string secid/*, string username,*/ ,Action<MarketDepth> action)
	{
		//if (!UsersContext.ContainsKey(username))
		//	return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

		if (!SubscriptionsForOrderBook.ContainsKey(secid))
			SubscriptionsForOrderBook[secid] = new();


        if (!plaza.Securities.ContainsKey(secid) )
            return new SubscriptionAnswer() { Success = false, ErrorMessage = "No Sec Found" };

        plaza.RegisterMarketDepth(plaza.Securities[secid], simulation);

		string guid = Guid.NewGuid().ToString();
        //проверить будет ли это автоматом работать
        var result = SubscriptionsForOrderBook[secid].TryAdd(guid, action);

		return new SubscriptionAnswer()
		{
			Success = result,
			Id = guid
		};

	}

    public async Task<SubscriptionAnswer> SubscribeForTicks(string secid, Action<List<Trade>> action)
    {
		//if (!UsersContext.ContainsKey(username))
		//	return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };


		if (!plaza.Securities.ContainsKey(secid))
			return new SubscriptionAnswer() { Success = false, ErrorMessage = "No Sec Found" };

        plaza.TryRegisterTicks(plaza.Securities[secid], simulation);

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


	public async Task<SubscriptionAnswer> RemoveSubscribeForOrderBook(string secid,  string id)
	{
		//проверить будет ли это автоматом работать
		var result = SubscriptionsForOrderBook[secid].TryRemove(id, out var _);

		return new SubscriptionAnswer()
		{
			Success = result,
			Id = id
		};

	}

	public async Task<SubscriptionAnswer> RemoveSubscribeForTicks(string secid,  string id)
	{
		//if (!UsersContext.ContainsKey(username))
		//	return new SubscriptionAnswer { Success = false, ErrorMessage = "No User Found", Id = "" };

		//проверить будет ли это автоматом работать
		var result = SubscriptionsForTicks[secid].TryRemove(id, out var _);

		return new SubscriptionAnswer()
		{
			Success = result,
			Id = id
		};

	}


	#endregion

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

	//public ReturnType<bool> Register(UserCredentials userCredentials)
	//{
	//    if (UsersContext.ContainsKey(userCredentials.LoginEmail))
	//    {
	//        return new ReturnType<bool>(false, "User already exists");
	//    }

	//    UsersContext.TryAdd(userCredentials.LoginEmail, new User(userCredentials.LoginEmail, userCredentials.Password)
	//    {
	//        Limit = 20000,
	//        CanTrade = true,
	//        Admin = false,
	//    });

	//    return new ReturnType<bool>(true);
	//}

	

	public async Task <LoginInfo> Login(string login, string pass)
    {
        if (!UsersContext.ContainsKey(login))
        {
            return new LoginInfo { errorMessage = "User doesn't exist" };
        }

        var user = UsersContext[login];

        if (user.Password != pass)
        {
			return new LoginInfo { errorMessage = "Incorrect password" };
        }

        var authResponse = jwtProvider.GenerateToken(user);

        return authResponse;
    }

    public bool Logout()
    {
        try
        {
            //TODO: по идее здесь можно вырубить все подписки юзера и т.д.

            return true;

        }
        catch (Exception ex)
        {
            return false;
        }
		return false;
	}

    //public ReturnType<List<KeyValuePair<string, User>>> RiskManagerGetUsers(string userName)
    //{
    //    if (!UsersContext.ContainsKey(userName))
    //        return new ReturnType<List<KeyValuePair<string, User>>>(null, "User not found");

    //    if (!UsersContext[userName].Admin)
    //        return new ReturnType<List<KeyValuePair<string, User>>>(null, "No Admin rights");

    //    try
    //    {
    //        var usersList = UsersContext.ToList().Where(a => !a.Value.Admin).ToList();
    //        return new ReturnType<List<KeyValuePair<string, User>>>(usersList);
    //    }
    //    catch (Exception ex)
    //    {
    //        return new ReturnType<List<KeyValuePair<string, User>>>(null, ex.Message);
    //    }
    //}

    //public ReturnType<bool> RiskManagerChangeLimitUser(string userName, string usernametochange, decimal newlimit)
    //{
    //    if (!UsersContext.ContainsKey(userName))
    //        return new ReturnType<bool>(false, "User not found");

    //    if (!UsersContext[userName].Admin)
    //        return new ReturnType<bool>(false, "No Admin rights");

    //    try
    //    {
    //        UsersContext[usernametochange].Limit = newlimit;
    //        return new ReturnType<bool>(true);
    //    }
    //    catch (Exception ex)
    //    {
    //        return new ReturnType<bool>(false, ex.Message);
    //    }
    //}

    //public ReturnType<bool> RiskManagerCanTrade(string userName, string usernametochange, bool cantrade)
    //{
    //    if (!UsersContext.ContainsKey(userName))
    //        return new ReturnType<bool>(false, "User not found");

    //    if (!UsersContext[userName].Admin)
    //        return new ReturnType<bool>(false, "No Admin rights");

    //    if (UsersContext[usernametochange].Admin)
    //    {
    //        return new ReturnType<bool>(false, "You cannot change settings for admin to trade");
    //    }

    //    try
    //    {
    //        UsersContext[usernametochange].CanTrade = cantrade;
    //        return new ReturnType<bool>(true);
    //    }
    //    catch (Exception ex)
    //    {
    //        return new ReturnType<bool>(false, ex.Message);
    //    }
    //}

    //public ReturnType<bool> RiskManagerCloseAllPositions(string userName)
    //{
    //    if (!UsersContext.ContainsKey(userName))
    //        return new ReturnType<bool>(false, "User not found");

    //    if (!UsersContext[userName].Admin)
    //        return new ReturnType<bool>(false, "No Admin rights");

    //    if (plaza == null)
    //        return new ReturnType<bool>(false, "Plaza Not Ready");

    //    try
    //    {
    //        foreach (var pos in RealPositions.Values)
    //        {
    //            var sec = plaza.Securities[pos.SecurityId];
    //            var order = new Order(sec, pos.XPosValueCurrent > 0 ? Side.Sell : Side.Buy, pos.XPosValueCurrent, plaza.Portfolio.Number, userName);
    //            LogMessageAsync("Sending close orders of positions " + order.ToString());
    //        }
    //        return new ReturnType<bool>(true);
    //    }
    //    catch (Exception ex)
    //    {
    //        return new ReturnType<bool>(false, ex.Message);
    //    }
    //}

    public async Task<Order> SendOrder(ClientOrder clientOrder, string userName)
    {
        try
        {
            if (!UsersContext.ContainsKey(userName))
                return new Order { Error = "User not found" };

            var user = UsersContext[userName];

            if (!user.CanTrade)
				return new Order { Error = "User cannot trade!" };

            if (!plaza.Securities.ContainsKey(clientOrder.SecID))
				return new Order { Error = "No Security Id Found" };
	
            var price = (decimal)clientOrder.Price;
            var sec = plaza.Securities[clientOrder.SecID];

            Order plazaOrder = clientOrder.Market ?
               new Order(sec, clientOrder.Side, clientOrder.Volume, plaza.Portfolio.Number, userName) :
                new Order(sec, clientOrder.Side, clientOrder.Volume, price, plaza.Portfolio.Number, userName);

            if (clientOrder.NumberOrderId != null && clientOrder.NumberOrderId != 0)
                plazaOrder.NumberUserOrderId = (int)clientOrder.NumberOrderId;

            LogMessageAsync($"Sending Order {userName} price={plazaOrder.PriceOrder} ");

            await plaza.ExecuteOrderAsync(plazaOrder).ConfigureAwait(false);

            return plazaOrder;
        }
        catch (Exception ex)
        {
            return  new Order { Error = ex.Message };
        }
    }

    public List<Pos> GetOpenPositions(string userName)
    {
        try
        {
            if (!OpenedPositions.ContainsKey(userName))
                return new List<Pos>() { };

            var positions = OpenedPositions[userName].Values.ToList();
            return positions;
        }
        catch (Exception ex)
        {
            return new List<Pos>() { };
        }
    }


    public ServerResponce<List<Order>> GetOrders(string userName)
    {
        try
        {
            if (!Orders.ContainsKey(userName))
                return new ServerResponce<List<Order>>(new List<Order>(),"No such user");

            var result = Orders[userName].Values.Where(o => o.State == Order.OrderStateType.Activ || o.State == Order.OrderStateType.Partial).ToList();
            return new ServerResponce<List<Order>>(result ?? new List<Order>());
        }
        catch (Exception ex)
        {
            return new ServerResponce<List<Order>>(null, ex.Message);
        }
    }

    public ServerResponce<bool> CancelOrder(string userName, Order order)
    {
        try
        {
            if (!UsersContext.ContainsKey(userName))
                return new ServerResponce<bool>(false, "User not found");

            plaza.CancelOrder(order.NumberUserOrderId);
            return new ServerResponce<bool>(true);
        }
        catch (Exception ex)
        {
            return new ServerResponce<bool>(false, ex.Message);
        }
    }

    public async Task<ServerResponce<List<SecurityApi>>> GetAllSecurities()
    {
        if (Securities == null || Securities.Count == 0)
            return new ServerResponce<List<SecurityApi>>(null, "No Securities");

        return new ServerResponce<List<SecurityApi>>(Securities.Values.OrderBy(s => s.ShortName).ToList());
    }

    #endregion mapMethods

    public void Dipose()
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
