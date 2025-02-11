using LiteInvest.Entity.PlazaEntity;
using PlazaEngine.Engine;

using System.Diagnostics;

using static System.Formats.Asn1.AsnWriter;

namespace ConnectorService
{
    /// <summary>
    /// Класс создания агрегированных стаканов - Singleton(одиночка). Для создания вызвать статический метод Build и передать коннектор 
    /// </summary>
    public class AggregateGlass
    {
        /// <summary>
        /// Преднастроенные масштабы агрегирования
        /// </summary>
        public static readonly List<int> ScaleList = new List<int>() {1, 5, 10, 15, 100 };

        /// <summary>
        ///Количество уровней в стакане в обе стороны
        /// </summary>
        private int allBlankLevelGlassCount = 3000;

        /// <summary>
        /// key = secID
        /// </summary>
        private Dictionary<string, InsideQuotes> AllInsideQuotes = new Dictionary<string, InsideQuotes>();

        private PlazaConnector connector;

        private static AggregateGlass Instance;

        private static object buildLocker = new object();

        /// <summary>
        /// Создать класс для реализации агрегированного стакана и передать ему коннектор
        /// </summary>
        /// <param name="connector"></param>
        /// <returns></returns>
        public static AggregateGlass Build(ConnectorBase connector)
        {
            lock (buildLocker)
            {
                if (Instance == null)
                {
                    Instance = new AggregateGlass(connector);
                }
                return Instance;
            }
        }

        /// <summary>
        /// key = isin Security, valus = список масштабов scale
        /// </summary>
        private Dictionary<string,List<int>> AllSubscribedScaledGlass = new Dictionary<string, List<int>>();

        /// <summary>
        /// Класс агрегирования стаканов
        /// </summary>
        /// <param name="connector"></param>
        private AggregateGlass(ConnectorBase connector)
        {
            this.connector = (PlazaConnector)connector;
            this.connector.MarketDepthChangeEvent += Connector_MarketDepthChangeEvent;
        }

        /// <summary>
        /// ОБрабатываем собития поступления полного стакана из плазы
        /// </summary>
        /// <param name="md">Полный стакан из Плазы</param>
        private void Connector_MarketDepthChangeEvent(MarketDepth md)
        {
            //Task.Factory.StartNew(() =>
            {
                try
                {
                    if (AllSubscribedScaledGlass.ContainsKey(md.SecurityId)) // проверка подписались ли на стакан по этому инструменту
                    {
                        if (!AllInsideQuotes.ContainsKey(md.SecurityId))        // проверка, создан ли чистые бланк стаканов для всех масштабов в 6000 строк
                        {
                            CreateBlankInsideQuotes(md);                    // создаем все балнки стаканов по инструменту, по которому пришел стакан
                        }

                        var security = connector.GetSecurityByIsin(md.SecurityId);
                        decimal priceStep = security.PriceStep;

                        ParallelOptions opt = new ParallelOptions() { MaxDegreeOfParallelism = ScaleList.Count };
                        Parallel.For(0, AllSubscribedScaledGlass[md.SecurityId].Count, opt, (i) =>  // распараллелим сбор по всем заказанным масштабам
                        {
                            int scale = AllSubscribedScaledGlass[md.SecurityId][i];
                            BuildAggregateGlass(md, priceStep, scale);
                        });

                        NewInsideQuotesEvent?.Invoke(AllInsideQuotes[md.SecurityId]);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            //);
        }

        /// <summary>
        /// Построить агрегированный стакан
        /// </summary>
        /// <param name="md">стакан из Плазы</param>
        /// <param name="priceStep">Шаг цены инструмента</param>
        /// <param name="scale">Масштаб</param>
        private void BuildAggregateGlass(MarketDepth md, decimal priceStep, int scale)
        {
            decimal currentPrice = 0;
            decimal currentVolume = 0;
            var d =  AllInsideQuotes[md.SecurityId].ScaledQuotes[scale];
            d.Clear(); d = null;
            AllInsideQuotes[md.SecurityId].ScaledQuotes[scale] = (List<MarketDepthLevel>)AllInsideQuotes[md.SecurityId].BlankScaledQuotes[scale].Clone();
            CheckNeedExpansionBlankGlass(md);
            
            for (int y = 0; y < md.Asks.Count; y++)              // продажи
            {
                decimal scalePrice = md.Asks[y].Price - md.Asks[y].Price % (scale * priceStep);
                if (y == 0)
                {
                    currentPrice = scalePrice;
                }
                if (currentPrice == scalePrice)
                {
                    currentVolume += md.Asks[y].Ask;
                }
                if (currentPrice != scalePrice)
                {
                    int indexPrice = AllInsideQuotes[md.SecurityId].IndexPriceLevelQuotes[scale][currentPrice];
                    var s = AllInsideQuotes[md.SecurityId].ScaledQuotes[scale][indexPrice];
                    s.Type = Side.Sell;
                    s.Ask = currentVolume;
                    currentPrice = scalePrice;
                    currentVolume = md.Asks[y].Ask;
                }
                if (y == md.Asks.Count - 1)                         // в том числе конец цикла
                {
                    int indexPrice = AllInsideQuotes[md.SecurityId].IndexPriceLevelQuotes[scale][currentPrice];
                    var s = AllInsideQuotes[md.SecurityId].ScaledQuotes[scale][indexPrice];
                    s.Type = Side.Sell;
                    s.Ask = currentVolume;
                }
            }

            currentPrice = 0;
            currentVolume = 0;
            
            for (int y = 0; y < md.Bids.Count; y++)                 // покупки
            {
                decimal scalePrice = md.Bids[y].Price - md.Bids[y].Price % (scale * priceStep);

                if (y == 0)
                {
                    currentPrice = scalePrice;
                    int indexCentrePrice = AllInsideQuotes[md.SecurityId].IndexPriceLevelQuotes[scale][currentPrice];
                    AllInsideQuotes[md.SecurityId].CentreQuotes[scale] = indexCentrePrice;
                }
                if (currentPrice == scalePrice)
                {
                    currentVolume += md.Bids[y].Bid;
                }
                if (currentPrice != scalePrice)
                {
                    int indexPrice = AllInsideQuotes[md.SecurityId].IndexPriceLevelQuotes[scale][currentPrice];
                    var s = AllInsideQuotes[md.SecurityId].ScaledQuotes[scale][indexPrice];
                    s.Type = Side.Buy;
                    s.Bid = currentVolume;
                    currentPrice = scalePrice;
                    currentVolume = md.Bids[y].Bid;
                }
                if (y == md.Bids.Count - 1)                         // в том числе конец цикла
                {
                    int indexPrice = AllInsideQuotes[md.SecurityId].IndexPriceLevelQuotes[scale][currentPrice];
                    var s = AllInsideQuotes[md.SecurityId].ScaledQuotes[scale][indexPrice];
                    s.Type = Side.Buy;
                    s.Bid = currentVolume;
                }
            }
        }

        private void CheckNeedExpansionBlankGlass(MarketDepth md)
        {
            decimal maxPrice = 0;
            decimal minPrice = 0;
            if (md.Asks.Count > 0)
            {
                maxPrice = md.Asks[0].Price;
            }
            else if (md.Bids.Count > 0)
            {
                maxPrice = md.Bids[0].Price * 2 - md.Bids.Last().Price;
            }
            if (md.Bids.Count > 0)
            {
                minPrice = md.Bids.Last().Price;
            }
            else if (md.Asks.Count > 0)
            {
                minPrice = md.Asks.Last().Price * 2 - md.Asks[0].Price;
            }
            if (maxPrice == 0 || minPrice == 0)
            {
                return; // пустой стакан
            }
            var s = AllInsideQuotes[md.SecurityId].IndexPriceLevelQuotes[1]; // берем полный не масштабированный индекс
            if (s.Count > 1 && (!s.ContainsKey(maxPrice) || s[maxPrice] > s.Count * 0.8 
                || !s.ContainsKey(minPrice) || s[minPrice] < s.Count * 0.2) )
            {
                CreateBlankInsideQuotes(md);
            }
        }


        /// <summary>
        /// Подписаться на все пренастроенные агрегированные стаканы предустановленных масштабов scaleList
        /// </summary>
        /// <param name="isin"></param>
        public void SubscribAllScaledGlass(string isin)
        {
            foreach (var scale in ScaleList)
            {
                //connector.Emulation = true;
                connector.Register_Unregister_MarketDepth(isin, true);
                if (!AllSubscribedScaledGlass.ContainsKey(isin) || AllSubscribedScaledGlass[isin] == null)
                {
                    AllSubscribedScaledGlass[isin] = new List<int>();
                }
                if (!AllSubscribedScaledGlass[isin].Exists(s => s == scale))   // проверим, не подписались ли ранее 
                {
                    AllSubscribedScaledGlass[isin].Add(scale);
                }
            }
        }
       
        /// <summary>
        /// Создаем пустые бланки стаканов всех масштабов большой глубины, а так же индексированные словари по ним
        /// </summary>
        /// <param name="md">стакан из Плазы</param>
        private void CreateBlankInsideQuotes(MarketDepth md)
        {
            InsideQuotes insideQuotes = new InsideQuotes();
            insideQuotes.SecurityId = md.SecurityId;

            var security = connector.GetSecurityByIsin(md.SecurityId);
            
            for (int i = 0; i < ScaleList.Count; i++)   // под все масштабы сразу делаем стаканы
            {
                insideQuotes.ScaledQuotes[ScaleList[i]] = new List<MarketDepthLevel>();
                insideQuotes.IndexPriceLevelQuotes[ScaleList[i]] = new Dictionary<decimal, int>();
                var sc = insideQuotes.ScaledQuotes[ScaleList[i]];
                var indexSc = insideQuotes.IndexPriceLevelQuotes[ScaleList[i]];

                decimal currentPriceLevel = md.Bids[0].Price + allBlankLevelGlassCount * ScaleList[i] * security.PriceStep;
                currentPriceLevel = currentPriceLevel - currentPriceLevel % (ScaleList[i] * security.PriceStep);
               
                for (int y = allBlankLevelGlassCount; y > allBlankLevelGlassCount * -1; y--)
                {
                    MarketDepthLevel level = new MarketDepthLevel();
                    currentPriceLevel = currentPriceLevel - ScaleList[i] * security.PriceStep;
                    level.Price = currentPriceLevel;
                    level.Ask = 0;
                    level.Bid = 0;
                    level.Type = 0;
                    sc.Add(level);
                    indexSc[currentPriceLevel] = sc.Count - 1;
                }
                insideQuotes.BlankScaledQuotes[ScaleList[i]] = (List<MarketDepthLevel>)sc.Clone();
            }
            

            AllInsideQuotes[md.SecurityId] = insideQuotes;
        }

        public void NeedUpDateGlass(string? securityId)
        {
            if (!string.IsNullOrWhiteSpace(securityId) && AllInsideQuotes.ContainsKey(securityId))
            {
                NewInsideQuotesEvent?.Invoke(AllInsideQuotes[securityId]);
            }
        }

        //ToDo Цент стакана посчитать и отдать.


        /// <summary>
        /// Получить все аггрегированные  стаканы по одному инструменту 
        /// </summary>
        public event Action<InsideQuotes> NewInsideQuotesEvent;

    }
}
