using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvest.Entity.PlazaEntity
{
    /// <summary>
    /// Агрегированные кластеры
    /// </summary>
    public class InsideClusters
    {
        /// <summary>
        /// Создать 
        /// </summary>
        public InsideClusters()
        {
            ScaledCluster = new ConcurrentDictionary<int, ConcurrentDictionary<DateTime, List<MarketDepthLevel>>>();
            //BlankScaledCluster = new ConcurrentDictionary<int, List<MarketDepthLevel>>();
            IndexPriceLevelCluster = new ConcurrentDictionary<int, ConcurrentDictionary<DateTime, ConcurrentDictionary<decimal, int>>>();
            TimeFrame = PlazaOptions.TimeFrameClusters;
            SecurityId = "";
        }


        /// <summary>
        /// Словарь агрегированных кластеров
        /// key int - масштаб, key Datetime - время старта кластера, List - кластер по ценовым уровням
        /// </summary>
        public ConcurrentDictionary<int, ConcurrentDictionary<DateTime, List<MarketDepthLevel>>> ScaledCluster;


        /// <summary>
        /// Чистый кластер по определенному масштабу
        /// key int - масштаб, List кластер по ценовым уровням
        /// </summary>
        //public ConcurrentDictionary<int, List<MarketDepthLevel>> BlankScaledCluster;


        /// <summary>
        /// Словарь индексов агрегированных цен
        /// key int - масштаб, key Datetime - время старта кластера, List - кластер по ценовым уровням
        /// </summary>
        public ConcurrentDictionary<int, ConcurrentDictionary<DateTime, ConcurrentDictionary<decimal,int>>> IndexPriceLevelCluster;


        /// <summary>
        /// Код инструмента
        /// </summary>
        public string SecurityId { get; set; }


        /// <summary>
        /// Таймфрейм для построения кластера, (5 минут в PlazaOptions.TimeFrameClusters)
        /// </summary>
        public TimeSpan TimeFrame;

    }
}
