using ConnectorService;

using LiteInvest.Entity.PlazaEntity;

using PlazaEngine.Engine;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Formats.Asn1.AsnWriter;

namespace Aggregate
{

    /// <summary>
    /// The class for creating aggregate clusters is a singleton. To create, call the static Build method.
    /// </summary>
    public class AggregateCluster
    {
        /// <summary>
        /// Instance of the created class
        /// </summary>
        private static AggregateCluster Instance;

        /// <summary>
        /// Connector from which we take ticks
        /// </summary>
        private PlazaConnector connector;

        /// <summary>
        /// list of scales
        /// </summary>
        private List<int> ScaleList;

        /// <summary>
        /// key = isin Security, valus =  scale
        /// </summary>
        private ConcurrentDictionary<string, List<int>> AllSubscribedScaledCluster = new ConcurrentDictionary<string, List<int>>();

        /// <summary>
        /// queue for processing ticks key = Secid
        /// </summary>
        private ConcurrentDictionary<string, ConcurrentQueue<List<Trade>>> dictionaryQueueTiks = new ConcurrentDictionary<string, ConcurrentQueue<List<Trade>>>();

        /// <summary>
        /// key = secID
        /// </summary>
        private ConcurrentDictionary<string, InsideClusters> AllInsideCluster = new ConcurrentDictionary<string, InsideClusters>();

        private AggregateGlass aggregateGlass
        {
            get => AggregateGlass.TryGetInstanceOrNull ?? AggregateGlass.Build(connector, ScaleList);
        }

        private bool ticksLoaded;

        /// <summary>
        /// Number of levels in a cluster in both directions
        /// </summary>
        private int sizeBlankLevel
        {
            get => PlazaOptions.SizeBlankMarketLevelsCountPriceStep;
        }

        public static AggregateCluster? TryGetInstanceOrNull
        {
            get => Instance;
        }

        /// <summary>
        /// Locker for creating an Instance
        /// </summary>
        private static object buildLocker = new object();

        /// <summary>
        /// Create and get an instance of the tick aggregation class into clusters
        /// </summary>
        /// <param name="connector">Коннектор</param>
        /// <param name="scaleList">Масштабы агрегации</param>
        /// <returns></returns>
        public static AggregateCluster Build(ConnectorBase connector, List<int> scaleList)
        {
            lock (buildLocker)
            {
                if (Instance == null)
                {
                    Instance = new AggregateCluster(connector);
                    Instance.ScaleList = scaleList;
                }
                return Instance;
            }
        }

        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connector"></param>
        private AggregateCluster(ConnectorBase connector)
        {
            ticksLoaded = false;

            this.connector = (PlazaConnector)connector;
            this.connector.NewTickCollectionEvent += Connector_NewTickCollectionEvent;
            this.connector.TicksLoadedEvent += Connector_TicksLoadedEvent;
            
            Thread threadMarketDepthAggregate = new Thread(ThreadTicksAggregate);
            threadMarketDepthAggregate.IsBackground = true;
            threadMarketDepthAggregate.Name = "ThreadTicksAggregate";
            threadMarketDepthAggregate.Start();

            
            aggregateGlass.NewInsideQuotesEvent += AggregateGlass_NewInsideQuotesEvent;
            
        }


        /// <summary>
        /// Force create blank cluster in this event from AggregateGlass
        /// </summary>
        /// <param name="insideQuotes"></param>
        private void AggregateGlass_NewInsideQuotesEvent(InsideQuotes insideQuotes)
        {
            var centrePriceIndex = insideQuotes.CentreQuotes[1];
            decimal centrePrice = insideQuotes.ScaledQuotes[1][centrePriceIndex].Price;

            ForceCreateCluster(insideQuotes.SecurityId, centrePrice, aggregateGlass.TimeLastGlass);

        }

        /// <summary>
        /// Event that all historical ticks are loaded and go online
        /// </summary>
        private void Connector_TicksLoadedEvent()
        {
            ticksLoaded = true;
        }

        /// <summary>
        /// Subscribe to receive aggregated clusters
        /// </summary>
        /// <param name="isin"></param>
        /// <param name="emulation"></param>
        public void SubscribAllScaledCluster(string isin, bool simulation)
        {
            foreach (int scale in ScaleList)
            {
                connector.Emulation = simulation;
                connector.Register_Unregister_Ticks(isin, true);
                if (!AllSubscribedScaledCluster.ContainsKey(isin) || AllSubscribedScaledCluster[isin] == null)
                {
                    AllSubscribedScaledCluster[isin] = new List<int>();
                }
                if (!AllSubscribedScaledCluster[isin].Exists(s => s == scale))   // subscription verification
                {
                    AllSubscribedScaledCluster[isin].Add(scale);
                }
            }
        }

        /// <summary>
        /// Building aggregated clusters from ticks
        /// </summary>
        /// <param name="trades">Tikcs</param>
        private void BuildAggregateCluster(List<Trade> trades)
        {
            for (int i = 0; i < trades.Count; i++)
            {
                Trade trade = trades[i];
                if (trade == null || string.IsNullOrWhiteSpace(trade.SecurityId))   // error trade
                {
                    continue;
                }
                var security = connector.GetSecurityByIsin(trade.SecurityId);
                if (security == null)   // error security 
                {
                    continue;
                }
                
                if (!AllInsideCluster.ContainsKey(trade.SecurityId))
                {
                    CreateBlankInsideCluster(trade);
                }
                if (!AllInsideCluster.ContainsKey(trade.SecurityId))
                {
                    Console.WriteLine($"{trade.SecurityId} не создали");
                    continue;
                }
                var cluster =  AllInsideCluster[trade.SecurityId];
                DateTime timeCluster = GetTimeClusterForTrade(trade, cluster.TimeFrame);

                
                for (int j = 0; j < ScaleList.Count; j++)
                {
                    int scale = ScaleList[j];
                    
                    CreateBlankTimeCluster(trade, cluster, timeCluster, scale);
                    DeleteOldTimeCluster(cluster, timeCluster, scale);
                    try
                    {
                        decimal scaledPriceTrade = trade.Price - trade.Price % (scale * security.PriceStep);
                        var sc = cluster.ScaledCluster[scale];

                        if (!cluster.IndexPriceLevelCluster[scale][timeCluster].ContainsKey(scaledPriceTrade))
                        {
                            continue;
                        }

                        var indexPrice = cluster.IndexPriceLevelCluster[scale][timeCluster][scaledPriceTrade];
                        if (trade.Side == Side.Buy)
                        {
                            sc[timeCluster][indexPrice].Bid += trade.Volume;
                        }
                        else
                        {
                            sc[timeCluster][indexPrice].Ask += trade.Volume;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private void DeleteOldTimeCluster(InsideClusters cluster, DateTime timeCluster, int scale)
        {

            
            var keys = cluster.ScaledCluster[scale].Keys.ToList();
            keys.Sort();
            while (keys.Count >3)
            {
                cluster.ScaledCluster[scale].Remove(keys[0], out var list);
                list?.Clear();
                list = null;
                
                cluster.IndexPriceLevelCluster[scale].Remove(keys[0], out var list1);
                list1?.Clear();
                list1 = null;

                keys.RemoveAt(0);
            }
        }

        bool _testThreadFrozzen = false;    // only for developer

        /// <summary>
        /// We get a list of ticks List<Trade> for different instruments 
        /// </summary>
        /// <param name="trades"> key = secId</param>
        private void Connector_NewTickCollectionEvent(Dictionary<string, List<Trade>> trades)
        {
            List<string> keysSecId = trades.Keys.ToList();
            TimeSpan timeFrame = PlazaOptions.TimeFrameClusters;
            for (int i = 0; i < keysSecId.Count; i++)
            {
                string secId = keysSecId[i];
                if (!dictionaryQueueTiks.ContainsKey(secId))
                {
                    dictionaryQueueTiks[secId] = new ConcurrentQueue<List<Trade>>();
                }
                if (trades[secId].Count > 0 && !_testThreadFrozzen)
                {
                    if (trades[secId][0].Time < DateTime.UtcNow.AddHours(3).Add(timeFrame * - 5)) // skip old tick 
                    {
                        continue;
                    }
                    //_testThreadFrozzen = true;        // only for developer
                    dictionaryQueueTiks[secId].Enqueue(trades[secId]);
                }
            }
        }

        /// <summary>
        ///  Thread that aggregates ticks into clusters
        /// </summary>
        private void ThreadTicksAggregate()
        {
            
            while (true)
            {
                try
                {
                    Thread.Sleep(1);
              
                    foreach (var pair in dictionaryQueueTiks)
                    {
                        bool needUpdate = false;
                        
                        while (!pair.Value.IsEmpty)
                        {
                            if (pair.Value.TryDequeue(out List<Trade> trades))
                            {
                                if ((trades?.Count??0) == 0)    // check nullable or empty
                                {
                                    continue;
                                }
                                BuildAggregateCluster(trades);
                                needUpdate = true;
                            }
                        }
                        if (needUpdate && AllSubscribedScaledCluster.ContainsKey(pair.Key) && AllInsideCluster.ContainsKey(pair.Key))  // Give away the clusters that are subscribed to.
                        {
                            NewInsideClusterEvent?.Invoke(AllInsideCluster[pair.Key]);
                        }
                    }
            
                    
                    
                    _testThreadFrozzen = false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

        }


        /// <summary>
        /// Build a new blank cluster
        /// </summary>
        /// <param name="trade"></param>
        private void CreateBlankInsideCluster(Trade trade)
        {
            var security = connector.GetSecurityByIsin(trade.SecurityId);
            if (security == null)
            {
                return;
            }

            AllInsideCluster[trade.SecurityId] = new InsideClusters();
            InsideClusters cluster = AllInsideCluster[trade.SecurityId];
            cluster.SecurityId = trade.SecurityId;
            cluster.TimeFrame = PlazaOptions.TimeFrameClusters;

            DateTime timeCluster = GetTimeClusterForTrade(trade, cluster.TimeFrame);


            for (int i = 0; i < ScaleList.Count; i++)
            {
                var scale = ScaleList[i];
                cluster.ScaledCluster[scale] = new ConcurrentDictionary<DateTime, List<MarketDepthLevel>>();
                cluster.IndexPriceLevelCluster[scale] = new ConcurrentDictionary<DateTime, ConcurrentDictionary<decimal, int>>();

                CreateBlankTimeCluster(trade, cluster, timeCluster, scale);
            }
        }



        private void CreateBlankTimeCluster(Trade trade, InsideClusters cluster, DateTime timeCluster, int scale)
        {
            var sc = cluster.ScaledCluster[scale];
            for (int i = 0; i < 3; i++)
            {
                DateTime timeClusterCheck = timeCluster.Subtract(i * cluster.TimeFrame);
                if (!sc.ContainsKey(timeClusterCheck))
                {
                    var indexSc = cluster.IndexPriceLevelCluster[scale];
                    sc[timeClusterCheck] = aggregateGlass.GetBlankScaledPriceLevel(trade.SecurityId, scale, trade.Price);
                    int indexer = 0;
                    indexSc[timeClusterCheck] = new ConcurrentDictionary<decimal, int>();
                    sc[timeClusterCheck].Select(p => indexSc[timeClusterCheck].TryAdd(key: p.Price, value: indexer++)).ToList();
                    if (indexSc[timeClusterCheck][sc[timeClusterCheck][0].Price] != 0)
                    {
                        Console.WriteLine("Error CreateBlankInsideCluster index 0");
                    }
                    if (indexSc[timeClusterCheck][sc[timeClusterCheck].Last().Price] != sc[timeClusterCheck].Count - 1)
                    {
                        Console.WriteLine("Error CreateBlankInsideCluster Last index ");
                    }

                    //cluster.BlankScaledCluster[scale] = (List<MarketDepthLevel>)sc[timeClusterCheck].Clone();
                }
            }
        }


        /// <summary>
        /// Calculate TimeCluster for incoming Trade-Tikc
        /// </summary>
        /// <param name="trade"></param>
        /// <param name="cluster"></param>
        /// <returns></returns>
        private static DateTime GetTimeClusterForTrade(Trade trade, TimeSpan TimeFrame)
        {
            int timeTradeTotalSecond = (int)trade.Time.TimeOfDay.TotalSeconds;
            int timeFrameClusterSeconds = (int)TimeFrame.TotalSeconds;
            int timeStartClusterSecond = timeTradeTotalSecond - timeTradeTotalSecond % timeFrameClusterSeconds;    // round
            DateTime timeCluster = trade.Time.Date.AddSeconds(timeStartClusterSecond);
            return timeCluster;
        }


        /// <summary>
        /// Force create Aggregate blank Cluster for securityId and centrePrice
        /// </summary>
        /// <param name="securityId"></param>
        /// <param name="centrePrice"></param>
        /// <param name="timeNow"></param>
        public void ForceCreateCluster(string? securityId, decimal centrePrice, DateTime timeNow)
        {
            if (string.IsNullOrWhiteSpace(securityId)) 
            {
                return;
            }
            if (!AllInsideCluster.ContainsKey(securityId))
            {
                Trade trade = new Trade();
                trade.SecurityId = securityId;
                trade.Price = centrePrice;
                trade.Time = timeNow;
                trade.Volume = 0;
                CreateBlankInsideCluster(trade);

                Console.WriteLine($"ForceCreateCluster -> {timeNow.ToString("HH.mm:ss.fff")} {securityId}, centrePrice={centrePrice}");

                
            }

            NewInsideClusterEvent?.Invoke(AllInsideCluster[securityId]);

        }


        /// <summary>
        /// Subscription to aggregated clusters
        /// </summary>
        public event Action<InsideClusters> NewInsideClusterEvent;
    }
}
