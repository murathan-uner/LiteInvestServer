using LiteInvest.Entity.PlazaEntity;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvest.Entity.ServerEntity
{

    [DataContract]
    public class AccountTrade : ADataBaseClass, ICloneable
    {
        
        /// <summary>
        /// Торгуемая площадка счета
        /// </summary>
        [DataMember]
        public BoardEnum? Board { get; set; }

        /// <summary>
        /// Номер (имя) счета (не более 15 символов)
        /// </summary>
        [DataMember]
        public string AccountNumber { get; set; }


        /// <summary>
        /// Номер Capex счета 
        /// </summary>
        [DataMember]
        public string AccountCapex { get; set; }


        /// <summary>
        /// Максимальная дневная просадка, процент  от Deposit
        /// </summary>
        [DataMember]
        public decimal MaxDayLossPercent { get; set; }


        /// <summary>
        /// Максимальная дневная просадка, процент  от Deposit
        /// </summary>
        [DataMember]
        public decimal MaxDayLossAbsolut { get; set; }

        /// <summary>
        /// Новая установленная максимальная дневная просадка, процент, применение после клиринга
        /// </summary>
        [DataMember]
        public decimal MaxDayLossPercentAfterClearing { get; set; }


        /// <summary>
        /// Новая максимальная дневная просадка, абсолют, применение после клиринга
        /// </summary>
        [DataMember]
        public decimal MaxDayLossAbsolutAfterClearing { get; set; }

        /// <summary>
        /// Подразделение
        /// </summary>
        [DataMember]
        public string Office { get; set; }

        /// <summary>
        /// Пэйаут офиса
        /// </summary>
        [DataMember]
        public decimal OfficePayOut { get; set; }


        /// <summary>
        /// Счет реферала
        /// </summary>
        [DataMember]
        public string? ReferrerAccountCapex { get; set; }

        /// <summary>
        /// Процент реферала
        /// </summary>
        [DataMember]
        public decimal ReferrerPayOut { get; set; }


        /// <summary>
        /// За все время полученный  PayOut компании
        /// </summary>
        [DataMember]
        public decimal TotalOfficePayOut { get; set; }

        /// <summary>
        /// За все время полученный  PayOut компании
        /// </summary>
        [DataMember]
        public decimal TotalReferrerPayOut { get; set; }



        /// <summary>
        /// Плечо к депозиту капитального счета для открытия позиций
        /// </summary>
        [DataMember]
        public int Leverage { get; set; }


        /// <summary>
        /// Лимит средств для открытия позиций 
        /// </summary>
        [DataMember]
        public decimal Limit{ get; set; }

        /// <summary>
        /// Может подключаться, если стоит True,  
        /// трейдер имеет доступ к данной торговой площадке.
        /// </summary>
        [DataMember]
        public bool CanTrade { get; set; }

        /// <summary>
        /// Может подключаться, после клиринга 
        /// если стоит True, трейдер имеет доступ к данной торговой площадке.
        /// </summary>
        [DataMember]
        public bool CanTradeAfterClearing { get; set; }



        /// <summary>
        /// Дата и время, последнего выполненнго PayOut и клиринга
        /// </summary>
        [DataMember]
        public DateTime? DateLastClearing { get; set; }


        /// <summary>
        /// Блокировка торговли на текущую сессию
        /// </summary>
        [DataMember]
        public DateTime DateBlockTradeSession { get; set; }

        /// <summary>
        /// Кем выполнена блокировка торговли на текущую сессию
        /// </summary>
        [DataMember]
        public TypeBlockTradeSessionEnum TypeBlockTradeSession { get; set; }

        /// <summary>
        /// Разрешить перенос через клиринговый период
        /// true  - разрешить
        /// </summary>
        [DataMember]
        public bool AllowOvernightTransfer { get; set; }

        /// <summary>
        /// Отменить штраф за превышение просадки
        /// </summary>
        [DataMember]
        public bool CancelPenaltyDrawdown { get; set; }


        /// <summary>
        /// Робот риск менеджер Вкл-Выкл
        /// true - вкл
        /// </summary>
        [DataMember]
        public bool RobotRiskManagerOn { get; set; }


        public object Clone()
        {
            AccountTrade clone = new AccountTrade();
            foreach (var prop in typeof(AccountTrade).GetProperties())
            {
                prop.SetValue(clone, prop.GetValue(this));
            }
            return clone;
        }
    }



    /// <summary>
    /// Тип торговой площадки
    /// </summary>
    public enum BoardEnum
    {
        /// <summary>
        /// Фондовый рынок Московской биржи; 
        /// </summary>
        StockMoex,

        /// <summary>
        /// Срочный рынок Московской биржи; 
        /// </summary>
        FortsMoex,

        /// <summary>
        /// Валютный рынок Московской биржи; 
        /// </summary>
        CexMoex,

        /// <summary>
        /// CME, EUREX; 
        /// </summary>
        CME,

        /// <summary>
        /// NYSE, NASDAQ. 
        /// </summary>
        Nyse,

        /// <summary>
        /// Крипто
        /// </summary>
        Crypto,
    }


    /// <summary>
    /// Тип депозита
    /// </summary>
    public enum TypeDepositEnum
    {
        /// <summary>
        /// Деньги компании
        /// </summary>
        DepositCompany,

        /// <summary>
        /// Деньги трейдера
        /// </summary>
        DepositTrader
    }

    /// <summary>
    /// Кем выполнена блокировка торговли на текущую сессию
    /// </summary>
    public enum TypeBlockTradeSessionEnum
    {
        /// <summary>
        /// Риск-менеджером
        /// </summary>
        Human,

        /// <summary>
        /// Робото-Автомат
        /// </summary>
        Robot
    }



}
