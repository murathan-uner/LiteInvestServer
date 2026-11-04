using LiteInvest.Entity.ReportEntity;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace LiteInvest.Entity.ServerEntity
{
    [DataContract]
    public class AccountCapex :ADataBaseClass, ICloneable
    {

        /// <summary>
        /// Номер (имя) счета (не более 15 символов)
        /// </summary>
        [DataMember]
        public string AccountNumber { get; set; }

        /// <summary>
        /// Пользователь, которому принадлежит счет
        /// </summary>
        [DataMember]
        public string UserName { get; set; }

        /// <summary>
        ///  Средства компании, выделенные на торговлю
        /// </summary>
        [DataMember]
        public decimal DepositCompany { get; set; }

        /// <summary>
        /// Собственные Средства 
        /// </summary>
        [DataMember]
        public decimal DepositTrader { get; set; }


        /// <summary>
        ///  Средства компании, выделенные на торговлю
        /// </summary>
        [DataMember]    // сохранять в XML
        [JsonIgnore]    // в  API не отдавать
        public decimal FullDepositCompany { get; set; }

        /// <summary>
        /// Собственные Средства 
        /// </summary>
        [DataMember]
        [JsonIgnore]    // в  API не отдавать
        public decimal FullDepositTrader { get; set; }


        /// <summary>
        ///  Средства компании для зачислления в клиринг и ночью
        /// </summary>
        [DataMember]
        public decimal AddDepositCompanyAfterClearing { get; set; }

        /// <summary>
        /// Средства трейдера для зачисления в клиринг и ночью
        /// </summary>
        [DataMember]
        public decimal AddDepositTraderAfterClearing { get; set; }

        
        public object Clone()
        {
            AccountCapex clone = new AccountCapex();
            foreach (var prop in typeof(AccountCapex).GetProperties())
            {
                prop.SetValue(clone, prop.GetValue(this));
            }
            return clone;
        }
    }
}
