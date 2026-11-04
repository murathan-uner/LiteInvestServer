using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvest.Entity.ServerEntity
{
    public class Transaction
    {

        /// <summary>
        /// GUID
        /// </summary>
        public  Guid Guid { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Дата время транзакции
        /// </summary>
        public  DateTime Time { get; set; } = DateTime.Now;


        /// <summary>
        /// Счет
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// Трейдер
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Автор транзакции либо System
        /// </summary>
        public  string Author { get; set; } = "System";

        /// <summary>
        /// Раздель, модуль системы
        /// </summary>
        public string Module {get; set; }


        /// <summary>
        /// Меняемое значение
        /// </summary>
        public string PropertyName {get; set; }

        /// <summary>
        /// Значение свойства до транзакции
        /// </summary>
        public decimal ValueBegin {get; set; }

        /// <summary>
        /// Значение свойства после транзакции 
        /// </summary>
        public decimal ValueEnd {get; set; }

        /// <summary>
        /// Значение изменяемое
        /// </summary>
        public decimal ValueChange { get; set; }

        /// <summary>
        /// Описание транзакции
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Источник изменения внутренний, внешний, API, RISK...
        /// </summary>
        public ReasonChangeEnum ReasonChange { get; set; }

        public override string ToString()
        {
            var res = new StringBuilder();
            foreach (var prop in typeof(Transaction).GetProperties())
            {
                res.Append(prop.Name).Append("=").Append(prop.GetValue(this)).Append(";");
            }
            return res.ToString();
        }

    }


    /// <summary>
    /// Причина изменения
    /// </summary>
    public enum ReasonChangeEnum
    {
        RestAPI,
        RiskManager,
        Clearing,
        MarginCall,
        ClosePosition,
        Night


    }

}
