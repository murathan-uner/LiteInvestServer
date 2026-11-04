using LiteInvest.Entity.ServerEntity;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvest.Entity.ReportEntity
{
    public class ReportFinRezInstrument : ADataBaseClass
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
        /// Номер инструмента 
        /// </summary>
        public string SecId { get; set; } = "";


        /// <summary>
        /// Маржа по инструменту
        /// </summary>
        public decimal Marga { get; set; }

        /// <summary>
        /// Единая комиссия
        /// </summary>
        public decimal CommissionSumma { get; set; }

        /// <summary>
        /// Финансовый результат по инструменту
        /// </summary>
        public decimal FinResSumma { get; set; }

        /// <summary>
        /// Отчетная Дата
        /// </summary>
        public DateTime DateReport { get; set; }

        public ReportFinRezInstrument() { }

    }
}
