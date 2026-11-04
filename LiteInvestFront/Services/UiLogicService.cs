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
using Telerik.Windows.Documents.Spreadsheet.Expressions.Functions;

namespace LiteInvestFront.Services;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Раньше выполнял функцию апи дата сервис, теперь выполняет только часть отвечающую за графическую логику. 
/// </summary>
public class UiLogicService:IDisposable
{
    //TODO: перенести все нормально в настройки

    public int openedInstruments { get; set; } = 0;

	public string userName { get; set; } = string.Empty;

	public Action<List<SecurityApi>>? InstrumentsLoadAction;
	//public Action<SecurityApi>? InstrumentOpenAction;

	public Action<string, int>? ScaleChangeAction;
	public Action<string, int, int>? OrderVolumeUpdateAction; // This will be removed when moving WorkWindowSettings component to JS // Temporary use for this version

    public Action <SecurityApi,ICollection<MarketDepthLevel>, Dictionary<decimal, int>>? BuildNewTable { get; set; }
	//initialquotes
	public void SendInitialQuotes(SecurityApi sec, ICollection<MarketDepthLevel> _initialOrderBook, Dictionary<decimal, int> indexesQuotes)
	{
		BuildNewTable?.Invoke(sec,_initialOrderBook, indexesQuotes);
	}

	public SecurityApi GenerateClonedSecurity(SecurityApi sec)
	{
        var clone = (SecurityApi)sec.Clone();
        clone.SpecialHash = Guid.NewGuid().ToString() + DateTime.Now;
		return clone;
    }

    //public void OpenInstrument(SecurityApi sec)
    //{
    //    InstrumentOpenAction?.Invoke(sec);
    //}

	public void NotifyAllInstrumentOpened(List<SecurityApi> secs)
	{
        InstrumentsLoadAction?.Invoke(secs);
	}

	public void NotifyScaleChanged(string specialHash, int scaleIndex)
	{
		ScaleChangeAction?.Invoke(specialHash, scaleIndex);
    }

    public void NotifyOrderVolumeUpdated(string specialHash, int index, int newVolume)
    {
        OrderVolumeUpdateAction?.Invoke(specialHash, index, newVolume);
    }

    public void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	public string SelectedSecurityHash = "";
}
