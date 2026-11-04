using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Text;

using LiteInvest.Entity.PlazaEntity;
using LiteInvest.Entity.ReportEntity;

namespace LiteInvest.Entity.ServerEntity
{
 
 /*
 цена фьючерса = 90 000 р. 
 Лимит = 30 000 р. 

 Плечо получается = цена / лимит 
 плечо = 3 

 проверить плечо выданное к плечу которое есть. 
 свободные финансы < */
    public class AggregationTrade
    {
        public decimal AveragePrice { get; set; }
        public decimal AllVol { get; set; }
    }

    /// <summary>
    /// Класс отвечающий за подсчет среднего (вход/выход)
    /// </summary>
    [DataContract]
    public class TradeCollection
    {
        [DataMember]
        public decimal Volume { get; set; }

        /// <summary>
        /// Возвращает среднее значение входа. 
        /// </summary>
        /// <param name="trade"></param>
        /// <returns>СРЕДНЮЮ ЦЕНУ</returns>
        public AggregationTrade AddTrade(MyTrade trade)
        {
            Trades.TryAdd(trade.NumberTrade, trade);
            var result = CalculateAveragePrice(Trades.Values);
            
            AveragePrice = result.AveragePrice;
            Volume = result.AllVol;

            return result;
        }

        [DataMember]
        public decimal AveragePrice { get; set; }

        /// <summary>
        /// Рассчитывает среднюю цену
        /// КЛЮЧ - весь объём
        /// Value - цена
        /// </summary>
        /// <param name="MyTrades"></param>
        /// <returns></returns>
        private AggregationTrade CalculateAveragePrice(ICollection <MyTrade> MyTrades)
        {
            decimal averageprice = 0;
            decimal summ = 0;

            foreach (var trade in MyTrades)
            {
                summ += trade.Volume;
                averageprice += trade.Price * trade.Volume;
            }
            averageprice = averageprice / summ;


            return new AggregationTrade() { AllVol = summ, AveragePrice=averageprice };
            
        }


        /// <summary>
        /// key = trade.NumberTrade
        /// </summary>
        [DataMember]
        public ConcurrentDictionary<string, MyTrade> Trades { get; set; } = new();
    }


    public class TradeAddResult
    {
        public bool Closed { get; set; }
        //public Pos NewPos { get; set; }

        

        public MyTrade RestNewTrade { get; set; }
    }

    /// <summary>
    /// По сути позиция с открытием, закрытием. 
    /// </summary>
    [DataContract]
    public class Pos
    {
        public Pos()
        {
            Guid = Guid.NewGuid();
        }

        /// <summary>
        /// The position has been cleared, the financial result, commissions and payout are recorded on the deposit.
        /// </summary>
        [DataMember]
        public bool Cleared { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public DateTime DateCleared { get; set; } = DateTime.MinValue;


        [DataMember]
        public BoardEnum Board { get; set; }

        [DataMember]
        public Guid Guid { get; set; }

        //[DataMember]
        //public decimal comissionInside { get; set; } = 0.02m / 100;    

        [DataMember]
        public TradeCollection OpenTrades { get; set; } = new();

        [DataMember]
        public TradeCollection CloseTrades { get; set; } = new();
		
        [DataMember]
		public string SecId { get; set; }

        [DataMember]
        public decimal PriceStep { get; set; }

        [DataMember]
        public decimal PriceStepCost { get; set; }

        [DataMember]
		public string SecName { get; set; }

        [DataMember]
        public decimal CurrentPos { get; set; } = 0;

        public decimal PosValue { get => Side == Side.Buy? CurrentPos : - CurrentPos; }

        [DataMember]
        public Side Side { get;  set; }

        
        public string StringSide { get => Side.ToString(); }

        [DataMember]
        public decimal AverageEntry{ get; set; }

        [DataMember]
        public DateTime OpenTime { get; set; }

        [DataMember]
        public DateTime CloseTime { get;  set; }

        [DataMember]
        public decimal AverageExit { get;  set; }


        private bool open;

        [DataMember]
        public bool Open
        {
            get => open;  
            set
            {
                if (open != value)
                {
                    open = value;
                }
            }
        }

        [DataMember]
        public decimal MaxOpened { get; set; }


        [DataMember]
        public decimal Commission { get; set; }

        /// <summary>
        /// сразу считает 
        /// </summary>
        [DataMember]
        public decimal RealizedPnl { get;  set; }

        /// <summary>
        /// Прибыль в пунтках
        /// </summary>
        [DataMember]
        public decimal RealizedPnlPoints { get;  set; }


        [DataMember]
        private decimal unRealizedPnl { get; set; }

        public decimal UnRealizedPnl
        {
            get
            { 
                if (CurrentPos == 0)
                            return 0;
                return unRealizedPnl;
            }

            set => unRealizedPnl = value;
        }

        [DataMember]
        private decimal unRealizedPnlPoints { get; set; }

        public decimal UnRealizedPnlPoints 
        {
            get
            {
                if (CurrentPos == 0)
                    return 0;
                return unRealizedPnlPoints;
            }
            set => unRealizedPnlPoints = value;
        
        }

        object tradelock = new object();

        public int multDirection => Side == Side.Buy ? 1 : -1;

        public decimal SimulationExit { get; set; }

       
        private void UpdateOpen(AggregationTrade aggregationTrade)
        {
            MaxOpened = aggregationTrade.AllVol;
            
            // Cделай округление цены на сервере к ближайшему шагу цену. В открытых позициях, потому что потом эта цена берётся и вставляется в линию прибыли
            if (PriceStep == 0) // не получили с сервера шаг цены, попробуем вычислить его сами
            {
                int decimalCount = 0;
                foreach (var item in OpenTrades.Trades)
                {
                    var t = item.Value;
                    string p = t.Price.ToString();
                    if (p.Split(['.', ',']).Count() == 2 )
                    {
                        int dc = p.Split([ '.' , ','])[1].Length;
                        if (decimalCount < dc)
                        {
                            decimalCount = dc;
                        }
                    }
                }
                
                PriceStep = 1/ (decimal)Math.Pow(10, decimalCount);
                
            }
            AverageEntry = Math.Round(aggregationTrade.AveragePrice / PriceStep) * PriceStep;
            
        }

        public TradeAddResult? AddTrade(MyTrade myTrade)
        {
            try
            {
               
                if (OpenTrades == null)
		            OpenTrades = new();

	            if (CloseTrades == null)
		            CloseTrades = new ();

                if (OpenTrades.Trades.TryGetValue(myTrade.NumberTrade, out var myTrade1) && myTrade1.NumberOrderMarket == myTrade.NumberOrderMarket)
                {   // сделка уже обработана
                    return null;
                }

                if (CloseTrades.Trades.TryGetValue(myTrade.NumberTrade, out var myTrade2) && myTrade2.NumberOrderMarket == myTrade.NumberOrderMarket)
                {   // сделка уже обработана
                    return null;
                }

                if (Board == null)
                {
                    Board = myTrade.Board;
                }

                //первая сделка открывает направление
                if (CurrentPos == 0)
                {
                    Side = myTrade.Side;
                    CurrentPos += myTrade.Volume;
                    OpenTime = myTrade.Time;
                    Open = true;

                    var res = OpenTrades.AddTrade(myTrade);
                    UpdateOpen(res);

                    return null;
                }
                // добавили позиции, усреднились
                if (CurrentPos != 0 && Side == myTrade.Side)
                {

                    CurrentPos += myTrade.Volume;
                    var res = OpenTrades.AddTrade(myTrade);
                    UpdateOpen(res);

                    return null;
                }



                //пришло обратное направление (то есть чел, перезакрывается)
                if (CurrentPos != 0 && Side != myTrade.Side)
                {
                    myTrade.MarginPoint = (myTrade.Price - AverageEntry) * multDirection / PriceStep;

                    // частичное закрытие
                    if (myTrade.Volume < CurrentPos)
                    {
                        CurrentPos -= myTrade.Volume;
                        CloseTrades.AddTrade(myTrade);

                        //CalculateRealizedPnl();
                        return null;

                    }

                    
                    if (myTrade.Volume == CurrentPos)   // позиция закрыта полностью
                    {
                        Open = false;
                        var rest = Math.Abs(CurrentPos) - Math.Abs(myTrade.Volume);

                        CurrentPos = 0;
                        CloseTime = myTrade.Time;

                        AverageExit = Math.Round(CloseTrades.AddTrade(myTrade).AveragePrice / PriceStep) * PriceStep;
                        //CalculateRealizedPnl();

                        //полное закрытие и остатка нет
                        if (rest == 0)
                        {
                            return new TradeAddResult()
                            {
                                RestNewTrade = null,
                                Closed = true,
                            };
                        }
                    }   
                    else if (myTrade.Volume > CurrentPos) //поза закрыта и есть остаток в сделке для перехода в новую позицию (условно перевернулись из лонг в шорт)
                    {
                        var rest = myTrade.Volume - CurrentPos ;
                        Open = false;
                        myTrade.Volume = CurrentPos;

                        CurrentPos = 0;
                        CloseTime = myTrade.Time;
                        
                        AverageExit = Math.Round(CloseTrades.AddTrade(myTrade).AveragePrice / PriceStep) * PriceStep;

                        return new TradeAddResult()
                        {
                            RestNewTrade = new MyTrade()
                            {
                                Board = myTrade.Board,
                                Side = myTrade.Side,
                                Price = myTrade.Price,
                                Volume = Math.Abs(rest),
                                NumberTrade = myTrade.NumberTrade + ".",
                                Comment = myTrade.Comment,
                                SecurityId = myTrade.SecurityId,
                                Portfolio = myTrade.Portfolio,
                                NumberOrderMarket = myTrade.NumberOrderMarket,
                                Time = myTrade.Time,
                                NumberOrderUser = myTrade.NumberOrderUser
                            },
                            Closed = true,
                        };
                    }
                    
                    return null;
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public override string ToString()
        {
            return new StringBuilder()
                .Append($"guid={Guid}").Append("; ")
                .Append($"Sec = {SecName}").Append("; ")
                .Append($"Side={Side}").Append("; ")
                .Append($"board={Board}").Append("; ")
                .Append($"MaxOpened={MaxOpened}").Append("; ")
                .Append($"RealizedPnl={RealizedPnl}").Append("; ")
                .Append($"UnRealizedPnl={UnRealizedPnl}").Append("; ")
                .ToString();
        }

    }
}
