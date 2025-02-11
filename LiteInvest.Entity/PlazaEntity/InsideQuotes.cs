using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvest.Entity.PlazaEntity
{
    public class InsideQuotes
    {

        public InsideQuotes()
        {
            ScaledQuotes = new ConcurrentDictionary<int, List<MarketDepthLevel>>();
            BlankScaledQuotes = new ConcurrentDictionary<int, List<MarketDepthLevel>>();
            IndexPriceLevelQuotes = new ConcurrentDictionary<int, Dictionary<decimal, int>>();
            CentreQuotes = new ConcurrentDictionary<int, int>();
            SecurityId = "";
        }

        /// <summary>
        /// Словарь со стаканами, простыми и агрегированными
        /// key - int масштаб scale, value List<MarketDepthLevel> - стакан по этому масштабу
        /// </summary>
        public ConcurrentDictionary<int, List<MarketDepthLevel>> ScaledQuotes { get; set; }

        public ConcurrentDictionary<int, List<MarketDepthLevel>> BlankScaledQuotes { get; set; }

        /// <summary>
        /// словарь индексов цен и уровней в словарях стаканов ScaledQuotes в  List MarketDepthLevel
        /// key - int масштаб scale, Value - словарь индексов, в нем key - цена, value индекс цены в List ScaledQuotes
        /// </summary>
        public ConcurrentDictionary<int, Dictionary<decimal, int>> IndexPriceLevelQuotes { get; set; }


        /// <summary>
        /// key - int масштаб scale, Value - int номер индекса середины стакана
        /// </summary>
        public ConcurrentDictionary<int, int> CentreQuotes { get; set; }

        /// <summary>
        /// Код инструмента
        /// </summary>
        public string SecurityId { get; set; }

    }

    public static class Extensions
    {
        public static IList<T> Clone<T>(this IList<T> source) where T : ICloneable
            => source.Select(item => (T)item.Clone()).ToList();
    }
}
