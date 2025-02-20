using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Serialization;

namespace LiteInvest.Entity.PlazaEntity;

/// <summary>
/// Обезличенная сделка
/// </summary>
public class TradeViewModel
{
    /// <summary>
    /// Имя инструмента
    /// </summary>
    public string SecurityName 
    { 
        get 
        {
            if (!(securityName is null) && securityName.Length > 0)
            {
                return securityName;
            }
            else 
            {
                //if ((securities?.Count ?? 0) > 0 && (securities?.TryGetValue(SecurityId, out Security? _secname)??false))
                //{
                //    securityName = _secname.Name;
                //}
                return securityName??default;
            }
        }
        set
        {
            securityName = value;
        }
    }
    private string? securityName;

    /// Номер сделки
    /// </summary>
    public string TransactionID { get; set; }

    /// <summary>
    /// Биржевой цифровой код инструмента
    /// </summary>
    public string SecurityId { get; set; }

    /// <summary>
    /// Время сделки в часовом поясе MSK (utc+2)
    /// </summary>
    public DateTime Time {get => time; set { time = value; } }
    DateTime time;

    /// <summary>
    /// Сделка получена в онлайне
    /// </summary>
    public bool IsOnline { get => isOnline; set { isOnline = value; } }
    bool isOnline;

    /// <summary>
    /// Направление сделки
    /// </summary>
    public Side Side { get => side; set { side = value; } }
    Side side;

    /// <summary>
    /// Объем по сделке
    /// </summary>
    public decimal Volume { get => volume; set { volume = value; } }
    decimal volume;

    /// <summary>
    /// Цена сделки
    /// </summary>
    public decimal Price { get => price; set { price = value; } }
    decimal price;


	public string Color
	{
        get
        {
            if (color == 0)
                return "transparent";

            if (Side == Side.Sell)
                //return "#fca49c";

                return "red";

            //return "#6CCCAC";
            return "green";
        }
        set { color = 0; }
    }
    int color = 1;

	public int IndexForChart { get; set; }

	private decimal? _size;
	public decimal Size
	{
		get
		{

			if (_size != null)
				return (decimal)_size;

			var vol = (int)Volume;
			var res = vol.ToString().Length;
			return res;
		}
		set
		{
			_size = value;
		}
	}

    /// <summary>
    /// размер пузыря
    /// </summary>
    public decimal BubbleSize { get => bubbleSize; set { bubbleSize = value; } }
    decimal bubbleSize;
}

