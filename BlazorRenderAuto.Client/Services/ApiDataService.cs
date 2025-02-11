using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Web;
using Binance.Net.Clients;
using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ServerEntity;

using RestSharp;
using Websocket.Client;
using System;
using System.Collections.ObjectModel;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects.Sockets;
using Binance.Net.Objects.Models.Spot;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects.Models.Spot.Socket;
using DevExpress.Xpo.Helpers;
using CryptoExchange.Net.Interfaces;


namespace BlazorRenderAuto.Client.Services
{

	public static class UriExtensions
	{
		/// <summary>
		/// Adds the specified parameter to the Query String.
		/// </summary>
		/// <param name="url"></param>
		/// <param name="paramName">Name of the parameter to add.</param>
		/// <param name="paramValue">Value for the parameter to add.</param>
		/// <returns>Url with added parameter.</returns>
		public static Uri AddParameter(this Uri url, string paramName, string paramValue)
		{
			var uriBuilder = new UriBuilder(url);
			var query = HttpUtility.ParseQueryString(uriBuilder.Query);
			query[paramName] = paramValue;
			uriBuilder.Query = query.ToString();

			return uriBuilder.Uri;
		}
	}

	public class ApiDataService:IDisposable
	{
		//TODO: перенести все нормально в настройки

		public int openedInstruments { get; set; } = 0;

		string mainadress = "http://188.72.77.60:3000/";
		Uri websocketurl = new Uri("ws://188.72.77.60:5000/");
		//WebsocketClient websocketClient;

		RestClient client;

		private BinanceRestClient binanceRestClient;
		private BinanceSocketClient binanceSocketClient;

		//TODO: Можно тоже соединить напрямую с проектом сервера
		static string loginrequest = "Common/Login";
		static string logoutrequest = "Common/LogOut";
		static string getinstruments = "Trading/GetAllSecurities";
		static string openInstrument = "Trading/OpenInstrument";
		static string closeInstrument = "Trading/CloseInstrument";
		static string userInstuments = "Trading/GetUserInstruments";

		static string sendOrder = "Trading/SendOrder";
		static string cancelOrder = "Trading/CancelOrder";

		static string getMyOrders = "Trading/GetOrders";
		static string getOpenPositions = "Trading/GetOpenPositions";

		//GetOpenPositions
		//GetUserInstruments

		private ConcurrentDictionary<string, SecurityApi> Securities { get; set; }

		string token = "";

		public Action<string, List<TradeApi>> NewTicks;

		/// <summary>
		/// Мой новый ордер
		/// </summary>
		public Action<Order> NewMyOrder { get; set; }

		public List<Order> AllActiveOrders { get; set; }
		public WebsocketClient orderwebcocketclient { get; private set; }

		private ConcurrentDictionary<int, WebsocketClient> AllWebSockets = new();
		private ConcurrentDictionary<int, UpdateSubscription> AllCryptoSockets = new();
		public Action<List<MarketDepthLevel>, List<MarketDepthLevel>,string> NewQuotes { get; set; }


		/// <summary>
		/// КРИПТА ИЛИ НЕТ
		/// НАСТРОЙКА ИСПОЛЬЗУЕТСЯ ДЛЯ ВСЕГО ПРОЕКТА
		/// TODO: вынеси настройки нормально
		/// </summary>
		public bool crypto { get; set; } = true;

		public ApiDataService()
		{
			client = new RestClient(mainadress);
			Securities = new ConcurrentDictionary<string, SecurityApi>();

			if (crypto)
			{
				binanceRestClient = new BinanceRestClient();
				binanceSocketClient = new BinanceSocketClient();

			}
			// websocketClient = new WebsocketClient(websocketurl);
		}

		public Action<SecurityApi, double, double> NewMaxMin { get; set; }
		public void SetNewMaxMin(SecurityApi security, double max, double min)
		{
			NewMaxMin?.Invoke(security, max, min);
		}

		public Action <SecurityApi,int>? NewScale { get; set; }
		public void UpdateMaxMinWithNewScale(SecurityApi sec,int newscale)
		{
			NewScale?.Invoke(sec,newscale);
		}

		public Action <SecurityApi,ICollection<MarketDepthLevel>, Dictionary<decimal, int>> BuildNewTable { get; set; }
		//initialquotes
		public void SendInitialQuotes(SecurityApi sec, ICollection<MarketDepthLevel> _initialOrderBook, Dictionary<decimal, int> indexesQuotes)
		{
			BuildNewTable?.Invoke(sec,_initialOrderBook, indexesQuotes);
		}

		public async Task<LoginInfo> LogIn(string login, string pass)
		{

			//return new LoginInfo(){ expirationTime = DateTime.Now + TimeSpan.FromDays(1),token = "sucesstoken"};

			var request = new RestRequest(loginrequest)
				.AddQueryParameter("login", login, false)
				.AddQueryParameter("pass", pass, false);

			try
			{
				var response = await client.PostAsync<LoginInfo>(request);
				Console.WriteLine($"Token received {response.token}");
				token = response.token;
				return response;
			}
			catch (Exception ex)
			{
				return null;
			}
		}

		public async Task<System.Net.HttpStatusCode> LogOut(string login)
		{
			var request = new RestRequest(logoutrequest).AddQueryParameter("login", login, false);

			try
			{
				var response = await client.PostAsync(request);
                Console.WriteLine($"Logged Out {response.StatusCode}");
                token = "";

				return response.StatusCode;
			}
            catch (Exception ex)
            {
                return System.Net.HttpStatusCode.InternalServerError;
            }
        }

		public async Task<Order?> SendOrder(ClientOrder order)
		{
			try
			{
				var request = new RestRequest(sendOrder);

				request.AddHeader("liteinvest", token);
				request.AddBody(order);

				var response = await client.PostAsync(request);

				if (!response.IsSuccessful)
				{
					Console.WriteLine(response.ErrorMessage);
				}

				var r = JsonConvert.DeserializeObject<Order>(response.Content);

				return r;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message); return null;
			}
		}

		public async Task<ClientOrder?> CancelOrder(Order order)
		{
			try
			{

				var request = new RestRequest(cancelOrder);

				request.AddHeader("liteinvest", token);
				request.AddBody(order);

				var response = await client.PostAsync(request);

				if (!response.IsSuccessful)
				{
					Console.WriteLine(response.ErrorMessage);
				}

				var r = JsonConvert.DeserializeObject<ClientOrder>(response.Content);

				return r;
			}
			catch (Exception ex) { Console.WriteLine(ex.Message); return null; }

			//TODO: Сделать десериализацию нормальную 
			//var answer = JsonConvert.DeserializeObject(response.Content);
		}

		public async Task<List<Order>> GetActiveOrders()
		{

			try
			{
				var request = new RestRequest(getMyOrders);

				request.AddHeader("liteinvest", token);

				var response = await client.GetAsync(request);

				if (!response.IsSuccessful)
				{
					Console.WriteLine(response.ErrorMessage);
				}

				var r = JsonConvert.DeserializeObject<List<Order>>(response.Content);

				return r;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"{ex.Message}");
				return null;
			}

			//TODO: Сделать десериализацию нормальную 
			//var answer = JsonConvert.DeserializeObject(response.Content);
		}

		public async Task<List<Pos>> GetPositions()
		{

			try
			{
				var request = new RestRequest(getOpenPositions);

				request.AddHeader("liteinvest", token);

				var response = await client.GetAsync(request);

				if (!response.IsSuccessful)
				{
					Console.WriteLine(response.ErrorMessage);
				}

				var r = JsonConvert.DeserializeObject<List<Pos>>(response.Content);

				return r;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return null;
			}

			//TODO: Сделать десериализацию нормальную 
			//var answer = JsonConvert.DeserializeObject(response.Content);
		}

		public Action<SecurityApi> NewSecOpened { get; set; }


		
		public Task OpenInstrument(SecurityApi sec)
		{

			var clone = (SecurityApi)sec.Clone();

			
			clone.SpecialHash = (Guid.NewGuid().ToString()) + DateTime.Now;
			NewSecOpened?.Invoke(clone);

			return Task.CompletedTask;
		}

		public async Task<IEnumerable<SecurityApi>> GetInstruments()
		{
			if (!crypto)
			{
				var request = new RestRequest(getinstruments);

				//request.AddCookie("liteinvest", token,"/", "188.72.77.60:3000");
				request.AddHeader("liteinvest", token);

				var response = await client.GetAsync(request);

				if (!response.IsSuccessful)
				{
					Console.WriteLine(response.ErrorMessage);
					return null;
				}


				// var r = await JsonSerializer.DeserializeAsync<SecurityApi>(response.st);

				var instruments = JsonConvert.DeserializeObject<IEnumerable<SecurityApi>>(response.Content);

				foreach (var instr in instruments)
					Securities[instr.id] = instr;

				return instruments;
			}
			else
			{
				var instruments = await binanceRestClient.UsdFuturesApi.CommonFuturesClient.GetSymbolsAsync();

				foreach (var instument in instruments.Data)
					Securities[instument.Name] = new SecurityApi()
					{
						Isin = instument.Name,
						PriceStep = (decimal)instument.PriceStep,
						id = instument.Name,
						Lot = (decimal)instument.MinTradeQuantity
					};


				return Securities.Values.ToList();
			}

		}

		public async Task<int?> SubcribeTick(string secid)
		{
			try
			{

				if (!crypto)
				{


					var webscoketrequest =
						websocketurl
							.AddParameter("stream", "public_trades")
							.AddParameter("sec_id", secid)
							.AddParameter("liteinvest", token);


					var websocketClient = new WebsocketClient(webscoketrequest) { IsReconnectionEnabled = true };

					websocketClient.MessageReceived.Subscribe(msg =>
					{
						try
						{
							// var ticks = JsonConvert.DeserializeObject<List<Trade>>(msg.Text);
							var ticks = JsonConvert.DeserializeObject<List<TradeApi>>(msg.Text);
							NewTicks?.Invoke(ticks[0].SecurityId, ticks);
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex.ToString());
						}

					});

					Console.WriteLine(webscoketrequest);

					var hash = websocketClient.GetHashCode();
					AllWebSockets.TryAdd(hash, websocketClient);
					await websocketClient.Start();
					return hash;
				}
				else
				{
					var res = await binanceSocketClient.UsdFuturesApi.ExchangeData.SubscribeToTradeUpdatesAsync(secid, ProcessCryptoTick);
					AllCryptoSockets[res.GetHashCode()] = res.Data;
					Console.WriteLine($"{secid} subscription {res.Success}");
					return res.GetHashCode();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				return null;
			}

		}

		private void ProcessCryptoTick(DataEvent<BinanceStreamTrade> tick)
		{

			var tradeTicks = new List<TradeApi>();
			tradeTicks.Add(new TradeApi()
			{
				Price = tick.Data.Price,
				SecurityId = tick.Symbol.ToUpper(),
				Volume = tick.Data.Quantity, 
				Time = DateTime.Now,// tick.Data.TradeTime, 
				Side = tick.Data.BuyerIsMaker ? Side.Buy:Side.Sell
			});
			NewTicks?.Invoke(tick.Symbol.ToUpper(), tradeTicks);
		}

		public async Task<int?> SubscribePrivateOrders()
		{
			try
			{

				var webscoketrequest =
					websocketurl
						.AddParameter("stream", "my_orders")
						.AddParameter("liteinvest", token);

				orderwebcocketclient = new WebsocketClient(webscoketrequest){IsReconnectionEnabled = true,};

				Console.WriteLine("Order socket " + webscoketrequest);

				orderwebcocketclient.MessageReceived.Subscribe(async msg =>
				{
					try
					{
						//почему то не хочет десериализовывать стакан нормальнь
						var order = JsonConvert.DeserializeObject<Order>(msg.Text, new JsonSerializerSettings() { CheckAdditionalContent = true, });
						NewMyOrder?.Invoke(order);

					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
				});

				var hash = orderwebcocketclient.GetHashCode();
				AllWebSockets.TryAdd(hash, orderwebcocketclient);

				Console.WriteLine("Subcribing ORDERS ->" + webscoketrequest);

				await orderwebcocketclient.Start();
				return hash;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return null;
			}
		}


		public async Task<int?> SubscribeOrderBook(string secid)
		{
			try
			{

				if (!crypto)
				{
					var webscoketrequest =
						websocketurl
							.AddParameter("stream", "orderbook")
							.AddParameter("sec_id", secid)
							.AddParameter("liteinvest", token);

					var websocketClient = new WebsocketClient(webscoketrequest){IsReconnectionEnabled = true, };

					websocketClient.MessageReceived.Subscribe(async msg =>
					{
						try
						{
							//почему то не хочет десериализовывать стакан нормальнь
							var md = JsonConvert.DeserializeObject<MarketDepth>(msg.Text,
								new JsonSerializerSettings() { CheckAdditionalContent = true, });

							NewQuotes?.Invoke(md.Bids, md.Asks, md.SecurityId);

						}
						catch (Exception ex)
						{
							Console.WriteLine($" SOCKET EXCEPTION -> {ex.Message}");
						}
					});

					websocketClient.ReconnectionHappened.Subscribe(info =>
					{
						Console.WriteLine($" Reconnection Happened! -> {info.ToString()}");
					});

					Console.WriteLine($"Subscribing Order book {secid}");
					Console.WriteLine(webscoketrequest);

					var hash = websocketClient.GetHashCode();
					AllWebSockets.TryAdd(hash, websocketClient);

					await websocketClient.Start();
					return hash;
				}
				else
				{

					//Обычный стакан
					//var res=binanceSocketClient.UsdFuturesApi.ExchangeData.SubscribeToPartialOrderBookUpdatesAsync(secid,20, 100, (binanceOrderbok)=>
					//{
					//	ProcessCryptoOrderBook(binanceOrderbok.Data, binanceOrderbok.Symbol);
					//});

					//Getting full order book
					var getfullorderbook = await binanceRestClient.UsdFuturesApi.ExchangeData.GetOrderBookAsync(secid);

					//TODO: Should be accurate with sec id in process FULL ORDER BOOK
					ProcessCryptoOrderBook(getfullorderbook.Data, secid);

					var res = await binanceSocketClient.UsdFuturesApi.ExchangeData.SubscribeToOrderBookUpdatesAsync(secid, 100, (binanceOrderbok) =>
					{
						ProcessCryptoOrderBook(binanceOrderbok.Data, binanceOrderbok.Symbol);
					});

					

					Console.WriteLine($"Result websocket {res.Data.SocketId} order book res = {res.Success}");


					Console.WriteLine($"Result gettting FULL orderbook res = {getfullorderbook.Success}");

					AllCryptoSockets[res.GetHashCode()] = res.Data;

					return res.GetHashCode();


				}

			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return null;
			}
		}


		private async void ProcessCryptoOrderBook(IBinanceOrderBook marketOrderBook,string symbol)
		{

			List<MarketDepthLevel> bids = new List<MarketDepthLevel>();
			List<MarketDepthLevel> asks = new List<MarketDepthLevel>();


			foreach (var cryptolevel in marketOrderBook.Bids)
			{
				bids.Add(new MarketDepthLevel(){Bid = cryptolevel.Quantity,Price= cryptolevel.Price});
			}


			foreach (var cryptolevel in marketOrderBook.Asks)
			{
				asks.Add(new MarketDepthLevel() { Ask = cryptolevel.Quantity, Price = cryptolevel.Price });
			}

			//Todo: можно делать один перебор
			NewQuotes?.Invoke(bids.OrderByDescending(s => s.Price).ToList(), asks, symbol.ToUpperInvariant());
		}

		public async void StopPrivateWebSocket(int? websocketId)
		{
			if (websocketId == null && !crypto)
			{
				Console.WriteLine("NUll PRIVATE WEBSOCKET FOR CLOSE !");
				return;
			}

			var websocket = AllWebSockets.TryGetValue((int)websocketId, out var foundwebsocket);

			if (foundwebsocket == null)
			{
				Console.WriteLine("No WEBSOCKET FOUND With HASH!");
				return;
			}

			var res = await foundwebsocket.StopOrFail(status: WebSocketCloseStatus.NormalClosure, "");

			Console.WriteLine(!res
				? $"Problems with closing websocket ID {websocketId}"
				: $"Closed success {websocketId}");

			AllWebSockets.Remove((int)websocketId, out var _);
		}

		public async void StopWebSocket(int? websocketId)
		{
			if (crypto)
			{
				if (AllCryptoSockets.ContainsKey((int) websocketId))
					AllCryptoSockets[(int)websocketId].CloseAsync();

				Console.WriteLine($"Crypto unsubscribe id = {websocketId} ");
				return;

			}


			if (websocketId == null && !crypto)
			{
				Console.WriteLine("NUll WEBSOCKET!");
				return;
			}

			var websocket = AllWebSockets.TryGetValue((int)websocketId, out var foundwebsocket);

			if (foundwebsocket == null)
			{
				Console.WriteLine("No WEBSOCKET FOUND With HASH!");
				return;
			}

			var res = await foundwebsocket.StopOrFail(status: WebSocketCloseStatus.NormalClosure, "");

			Console.WriteLine(!res
				? $"Problems with closing websocket ID {websocketId}"
				: $"Closed success {websocketId}");

			AllWebSockets.Remove((int)websocketId,out var _);
		}

		public void Dispose()
		{
			client = null;
			Securities = null;
			GC.SuppressFinalize(this);
		}

		public Action LastwindowOpened { get; set; }

		public void LastWindowWasOpened()
		{
			LastwindowOpened?.Invoke();
		}

		public Action <SecurityApi,int> ScrollClusters { get; set; }

		public void ScrollClustes(SecurityApi secmain, int bestbidIndex)
		{
			ScrollClusters?.Invoke(secmain,bestbidIndex);
		}

		public string SelectedSecurityHash = "";
		public Action SelectedSecurityHashChanged;
	}
}
