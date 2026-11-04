using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvest.Entity.ServerEntity
{
    public class ClearingResult
    {



        // <summary>
        /// GUID
        /// </summary>
        [DataMember]
        public Guid Guid { get; set; } = Guid.NewGuid();


        /// <summary>
        /// Время клиринга
        /// </summary>
        [DataMember]
        public DateTime DateTimeClearing { get; set; }

        /// <summary>
        /// Площадка
        /// </summary>
        [DataMember]
        public BoardEnum Board {  get; set; }

        /// <summary>
        /// Пользователь
        /// </summary>
        [DataMember]
        public string UserLogin { get; set; } = "";

        /// <summary>
        /// Капитальный счет
        /// </summary>
        [DataMember]
        public string AccountCapex{ get; set; } = "";

        /// <summary>
        /// Торговый счет
        /// </summary>
        [DataMember]
        public string AccountTrade { get; set; } = "";

        /// <summary>
        /// Guid позиции клиринга
        /// </summary>
        [DataMember]
        public Guid PositionId { get; set; } = Guid.Empty;

        /// <summary>
        /// Deposit Company Before Clearing;
        /// </summary>
        [DataMember]
        public decimal DepositCompanyBeforeClearing { get; set; }

        /// <summary>
        /// Deposit Company Before Clearing;
        /// </summary>
        [DataMember]
        public decimal DepositCompanyAfterClearing { get; set; }

        /// <summary>
        /// Deposit Trader Before Clearing;
        /// </summary>
        [DataMember]
        public decimal DepositTraderBeforeClearing { get; set; }

        /// <summary>
        /// Deposit Trader Before Clearing;
        /// </summary>
        [DataMember]
        public decimal DepositTraderAfterClearing { get; set; }

        /// <summary>
        /// Комиссия по клирингу
        /// </summary>
        [DataMember]
        public decimal CommissionSumma { get; set; }

        /// <summary>
        /// PnL по клирингу до вычета комиссий и штрафов и пэйаута
        /// </summary>
        [DataMember]
        public decimal  PnL {  get; set; }

        /// <summary>
        /// Штраф по клирингу за перенос и за превышение просадки
        /// </summary>
        [DataMember]
        public decimal Penalty {  get; set; }

        /// <summary>
        /// PayOut 
        /// </summary>
        [DataMember]
        public decimal PayOut {  get; set; }

        /// <summary>
        /// Описание
        /// </summary>
        [DataMember]
        public string Description { get; set; } = "";


        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(this.GetType().Name);
            sb.Append("=>");
            foreach (var prop in typeof(ClearingResult).GetProperties())
            {
                sb.AppendFormat("{0}={1}; ", prop.Name, prop.GetValue(this));
            }
            return sb.ToString();
        }

        

    }
}
