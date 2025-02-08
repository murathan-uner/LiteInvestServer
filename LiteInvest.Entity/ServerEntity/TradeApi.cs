using LiteInvest.Entity.PlazaEntity;
using Newtonsoft.Json;
using System.Drawing;

namespace LiteInvest.Entity.ServerEntity;

public class TradeApi
{
	[JsonProperty("sn")]
	public string SecurityName { get; set; }

	[JsonProperty("tid")]
	public string TransactionID { get; set; }

	[JsonProperty("s")]
	public string SecurityId { get; set; }

	[JsonProperty("t")]
	public DateTime Time { get; set; }

	[JsonProperty("d")]
	public Side Side { get; set; }

	[JsonProperty("v")]
	public decimal Volume { get; set; }

	[JsonProperty("p")]
	public decimal Price { get; set; }

	public string Color
	{
		get
		{
			if (Side == Side.Sell)
				//return "#fca49c";

				return "red";

				//return "#6CCCAC";
			return "green";
		}
	}

	public int IndexForChart { get; set; }

	private decimal? _size;
	public decimal Size
	{
		get
		{

			if (_size != null)
				return (decimal) _size;

			var vol = (int)Volume;
			var res= vol.ToString().Length;
			return res;
		}
		set
		{
			_size = value;
		}
	}
}
