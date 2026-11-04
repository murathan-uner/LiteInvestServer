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
    /// <summary>
    /// Финансовй результат по торговому счету в отчет
    /// </summary>
    public class ReportFinRezBoard : ADataBaseClass
    {
        

        /// <summary>
        /// Площадка
        /// </summary>
        public BoardEnum Board { get; set; }

        /// <summary>
        /// Торговый счет
        /// </summary>
        [ForeignKey(nameof(AccountTrade))]
        public Guid? AccountTrade { get; set; }
        

        /// <summary>
        /// Отчетная Дата
        /// </summary>
        public DateTime DateReport { get; set; }

        /// <summary>
        /// Лимит на след.сессию
        /// </summary>
        public decimal LimitNextSession { get; set; }

        /// <summary>
        /// Депозит на след.сессию
        /// </summary>
        public decimal DepositSumma{ get; set; }

        /// <summary>
        /// Маржа
        /// </summary>
        public decimal Marga {  get; set; }

        /// <summary>
        /// Единая комиссия
        /// </summary>
        public decimal CommissionSumma { get; set; }

        /// <summary>
        /// Выставлено количество торговых транзакций
        /// </summary>
        public int TransactionCount {  get; set; }

        /// <summary>
        /// Финансовый результат за отчетный период (день)
        /// </summary>
        public decimal FinResSumma { get; set; }

        /// <summary>
        /// Максимальная просадка на день
        /// </summary>
        public decimal MaxDayLoss { get; set; }

        /// <summary>
        /// Сформировать финансовый результат по торговому счету за день
        /// </summary>
        public ReportFinRezBoard() { }




    }
}
