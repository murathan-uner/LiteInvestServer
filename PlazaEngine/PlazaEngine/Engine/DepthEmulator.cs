
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LiteInvest.Entity.PlazaEntity;

using PlazaEngine.Depth;

namespace PlazaEngine.Engine
{
    internal class DepthEmulator
    {
        private ConcurrentDictionary<string, Security> _depthEmulators;
        private ConcurrentDictionary<string, Security> _ticksEmulators;
        private Dictionary<string, MarketDepth> _marketDepths;
        private Thread threadEmulating;
        

        public DepthEmulator()
        {
            _depthEmulators = new ConcurrentDictionary<string, Security>();
            _marketDepths = new Dictionary<string, MarketDepth>();
            _ticksEmulators = new ConcurrentDictionary<string, Security>();

            threadEmulating = new Thread(ThreadEmulating);
            threadEmulating.Name = "ThreadEmulating";
            threadEmulating.IsBackground = true;
            //threadEmulating.Start();
        }

        public event Action<MarketDepth>? MarketDepthChanged;
        public event Action<Dictionary<string, List<Trade>>>? NewTickCollectionEvent;


        private void ThreadEmulating()
        {
            int depth = 50;
            try
            {
                Random rnd = new Random(); 
                int counterDeleteLevel = 0;
                while (true)
                {
                    try
                    {
                        Thread.Sleep(200);
                        var security = _depthEmulators.Values.ToList();
                        foreach (var sec in security)
                        {
                            if (!_marketDepths.TryGetValue(sec.Id, out MarketDepth? value))
                            {
                                value = new MarketDepth();
                                _marketDepths[sec.Id] = value;
                            }
                            var md = value;
                            md.SecurityId = sec.Id;
                            md.Time = DateTime.UtcNow.AddHours(3);

                            decimal HiPrice = sec.PriceLimitHigh != 0 ? sec.PriceLimitHigh - 4 * (sec.PriceLimitHigh - sec.PriceLimitLow) / 10 : 1000;
                            decimal LoPrice = sec.PriceLimitLow != 0 ? sec.PriceLimitLow + 4 * (sec.PriceLimitHigh - sec.PriceLimitLow) / 10 : 100;
                            if (HiPrice < LoPrice)  // в режиме эмуляции на эмулированные инструменты бывает так прилетает
                            {
                                (HiPrice, LoPrice) = (LoPrice , HiPrice);   // меняем местами
                            }
                            if (HiPrice == LoPrice)     // и так бывает
                            {
                                if (sec.PriceStep != 0)
                                {
                                    HiPrice = HiPrice + sec.PriceStep * 1000;
                                    LoPrice = LoPrice - sec.PriceStep * 1000;
                                    
                                }
                                else
                                {
                                    HiPrice = HiPrice * 1.1m;
                                    LoPrice = LoPrice * 0.9m;
                                    sec.PriceStep = 0.1m;
                                }
                            }
                            if ((HiPrice - LoPrice) > sec.PriceStep * 4000)     // и так бывает
                            {
                                var Hi = (HiPrice + LoPrice) / 2 + sec.PriceStep * 2000;
                                var Lo = (HiPrice + LoPrice) / 2 - sec.PriceStep * 2000;
                                (HiPrice, LoPrice) = (Hi, Lo);
                            }
                            if (md.Asks.Count < 10 || md.Bids.Count < 10)
                            {
                                md.Asks.Clear();
                                md.Bids.Clear();
                                for (int i = 0; i < depth; i++)
                                {
                                    decimal p = Math.Round((HiPrice + LoPrice) / 2 + (decimal)rnd.NextDouble() * (HiPrice - LoPrice) / 4, sec.Decimals);
                                    int v = rnd.Next(1, 1000);
                                    if (!md.Asks.Exists(a => a.Price == p))
                                    {
                                        md.Asks.Add(new MarketDepthLevel() { Id = i, Price = p, Ask = v });
                                    }

                                    p = Math.Round((HiPrice + LoPrice) /2  - (decimal)rnd.NextDouble() * (HiPrice - LoPrice) / 4, sec.Decimals);
                                    v = rnd.Next(1, 1000);
                                    if (!md.Bids.Exists(a => a.Price == p))
                                    {
                                        md.Bids.Add(new MarketDepthLevel() { Id = i, Price = p, Bid = v });
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i <= 5; i++)
                                {
                                    int l = rnd.Next(0, md.Asks.Count - 1);
                                    int v = rnd.Next(1, 1000);
                                    md.Asks[l].Ask = v;
                                    l = rnd.Next(0, md.Bids.Count - 1);
                                    v = rnd.Next(1, 1000);
                                    md.Bids[l].Bid = v;
                                }
                                if (counterDeleteLevel++ > 10)
                                {
                                    counterDeleteLevel = 0;
                                    int ll = rnd.Next(0, md.Asks.Count - 1);
                                    md.Asks.RemoveAt(ll);
                                    ll = rnd.Next(0, md.Bids.Count - 1);
                                    md.Bids.RemoveAt(ll);
                                }
                            }

                           

                            md.Asks.Sort((x, y) => x.Price > y.Price ? -1 : x.Price == y.Price ? 0 : 1);
                            md.Bids.Sort((x, y) => x.Price > y.Price ? -1 : x.Price == y.Price ? 0 : 1);

                            ChangeCenterMarketDepthEmulating(md, 30, 30);
                            ChangeCenterMarketDepthEmulating(md, 3, 3);

                            MarketDepthChanged?.Invoke(md.GetCopy());

                            TickCollectionEmulating(md);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                
            }
        }


        Dictionary<long, DateTime> nextTimeChangeCenterMarketDepth = new Dictionary<long, DateTime>();
        private void ChangeCenterMarketDepthEmulating(MarketDepth md, int maxFreguency, int maxCountLevels)
        {
            long hash = md.SecurityId.GetHashCode() + maxFreguency.GetHashCode() + maxCountLevels.GetHashCode();

            if (nextTimeChangeCenterMarketDepth.ContainsKey(hash) && nextTimeChangeCenterMarketDepth[hash] > DateTime.Now)
            {
                return;
            }
            Random rnd = new Random();
            int levelsChangeCount = rnd.Next(1, maxCountLevels);
            Direction directionChange = rnd.NextDouble() > 0.5 ? Direction.Buy : Direction.Sell;

            for (int i = 0; i < levelsChangeCount && i < md.Asks.Count && i < md.Bids.Count; i++)
            {
                if (directionChange == Direction.Buy)
                {
                    md.Asks.Add(new MarketDepthLevel() { Id = md.Bids[0].Id, Price = md.Bids[0].Price, Ask = md.Bids[0].Bid });
                    md.Bids.RemoveAt(0);
                }
                else
                {
                    md.Bids.Add(new MarketDepthLevel() { Id = md.Asks.Last().Id, Price = md.Asks.Last().Price, Bid = md.Asks.Last().Ask});
                    md.Asks.RemoveAt(md.Asks.Count-1);
                }
            }

            md.Asks.Sort((x, y) => x.Price > y.Price ? -1 : x.Price == y.Price ? 0 : 1);
            md.Bids.Sort((x, y) => x.Price > y.Price ? -1 : x.Price == y.Price ? 0 : 1);

            int addSecond = rnd.Next(2, maxFreguency);
            nextTimeChangeCenterMarketDepth[hash] = DateTime.Now.AddSeconds(addSecond);
        }

        DateTime lastTickCollection = DateTime.Now;

        int MaxTicksRandomPause = 10;   // секунд, чем меньше, тем чаще
        int MaxTicksRandomCountInPack = 10; // максимальное кол-во тиков в одной пачке

        internal void TickCollectionEmulating(MarketDepth md)
        {
            

            try
            {
                if (!_ticksEmulators.ContainsKey(md.SecurityId))
                {
                    return;
                }
                Dictionary<string, List<Trade>> ticks = new();
                Random rnd = new Random();
                int _ticksPause = rnd.Next(MaxTicksRandomPause);
                if (lastTickCollection.AddSeconds(_ticksPause) > DateTime.Now)
                {
                    return;
                }
                var ask = md.Asks.Last();
                var bid = md.Bids.First();
                List<Trade> trades = new List<Trade>();

                int _maxTickCountInPack = rnd.Next(MaxTicksRandomCountInPack / 2) + 1;

                for (int i = 0; i < _maxTickCountInPack; i++)
                {
                    trades.Add(new Trade()
                    {
                        Price = ask.Price,
                        SecurityId = md.SecurityId,
                        Side = Side.Buy,
                        TransactionID = DateTime.Now.Ticks.ToString(),
                        Volume = (decimal)Math.Round(rnd.NextDouble() * (double)ask.Volume),
                        Time = md.Time.AddMilliseconds(10 * i),
                        IsOnline = true,
                        SecurityName = _ticksEmulators[md.SecurityId].Name
                    });

                    trades.Add(new Trade()
                    {
                        Price = bid.Price,
                        SecurityId = md.SecurityId,
                        Side = Side.Sell,
                        TransactionID = DateTime.Now.Ticks.ToString(),
                        Volume = (decimal)Math.Round(rnd.NextDouble() * (double)bid.Volume),
                        Time = md.Time.AddMilliseconds(30 * i),
                        IsOnline = true,
                        SecurityName = _ticksEmulators[md.SecurityId].Name
                    });
                }
                ticks.Add(md.SecurityId, trades);
                NewTickCollectionEvent?.Invoke(ticks);
                lastTickCollection = DateTime.Now;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            
        }


        object subscribeLocker = new object();
        internal void Subscribe(Security security)
        {
            lock (subscribeLocker)
            {
                if (_depthEmulators.ContainsKey(security.Id))
                {
                    return;
                }
                _depthEmulators.TryAdd(security.Id, security);
                if (!threadEmulating.IsAlive)
                {
                    threadEmulating.Start();
                }
            }
        }

        public void UnSubscribe(Security security)
        {
            _depthEmulators.Remove(security.Id, out var _);
        }

        public void SubscribeTick(Security security)
        {
            _ticksEmulators.TryAdd(security.Id, security);

            if (!threadEmulating.IsAlive)
            {
                threadEmulating.Start();
            }
        }

        public void UnSubscribeTick(Security security)
        {
            _ticksEmulators.Remove(security.Id, out var _);
        }

    }
}
