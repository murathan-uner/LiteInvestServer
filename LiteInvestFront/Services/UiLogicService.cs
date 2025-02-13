using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Web;
using Binance.Net.Clients;
using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ServerEntity;

using RestSharp;
using LiteInvestFront.Entity;
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
using CryptoExchange.Net.Interfaces;

namespace LiteInvestFront.Services;




/// <summary>
/// Раньше выполнял функцию апи дата сервис, теперь выполняет только часть отвечающую за графическую логику. 
/// </summary>
public class UiLogicService:IDisposable
{
	//TODO: перенести все нормально в настройки

	public int openedInstruments { get; set; } = 0;

	public string userName { get; set; }
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

	public Action<SecurityApi> NewSecOpened { get; set; }


	
	public Task OpenInstrument(SecurityApi sec)
	{

		var clone = (SecurityApi)sec.Clone();
		clone.SpecialHash = (Guid.NewGuid().ToString()) + DateTime.Now;
		NewSecOpened?.Invoke(clone);

		return Task.CompletedTask;
	}

	public void Dispose()
	{
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
