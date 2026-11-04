using LiteInvest.Entity.ServerEntity;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteInvest.Entity.PlazaEntity;

namespace LiteInvest.Entity.ReportEntity
{

    /// <summary>
    /// Раздел отчета Сделки за день
    /// </summary>
    public class ReportTradeDay : ADataBaseClass
    {
        
        /// <summary>
        /// Площадка
        /// </summary>
        public BoardEnum Board { get; set; }

        /// <summary>
        /// Номер торгового счета
        /// </summary>
        [ForeignKey(nameof(AccountTrade))]
        public Guid? AccountTrade { get; set; }

        /// <summary>
        /// Сделка на площадке
        /// </summary>
        [ForeignKey(nameof(MyTrade))]
        public Guid? MyTrade { get; set; }

        /// <summary>
        /// Дата сделки
        /// </summary>
        public DateTime DateTrade {  get; set; }

        /// <summary>
        /// Дата сделки - строка
        /// </summary>
        [NotMapped]
        public string DateTradeString 
        {
            get => DateTrade.ToString("HH:mm:ss dd.MM.yy"); 
        }


        /// <summary>
        /// Краткое наименование инструмента (SBER, GAZP, SiH6)
        /// </summary>
        public string SecIsin { get; set; } = "";

        /// <summary>
        /// Направление сделки
        /// </summary>
        public Side Direction { get; set; }


        [Column(TypeName = "decimal(10, 5)")] // 10 цифр всего, 5 после запятой
        /// <summary>
        /// Цена сделки
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Объем, количество приобретенного инструмента, в штуках, фьючерсы штук лотов.
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// Оборот
        /// </summary>
        public decimal Turnover { get; set; }

        /// <summary>
        /// Единая комиссия
        /// </summary>
        public decimal CommissionSumma { get; set; }

        /// <summary>
        /// Маржа в пунктах
        /// </summary>
        public decimal MargaPoint { get; set; }

        /// <summary>
        /// Маржа (в валюте площадки)
        /// </summary>
        public decimal MargaMoney { get; set; }

        
        /// <summary>
        /// Отчетная Дата
        /// </summary>
        public DateTime DateReport { get; set; }

        public ReportTradeDay() { }
    }

    
}
