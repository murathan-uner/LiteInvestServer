using LiteInvest.Entity.ServerEntity;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvest.Entity.ReportEntity
{
    /// <summary>
    /// История транзакций по единому счету
    /// </summary>
    public class ReportCapexHistoryTransaction : ADataBaseClass
    {
        

        /// <summary>
        /// Площадка
        /// </summary>
        public BoardEnum? Board { get; set; }

        /// <summary>
        /// Номер торгового счета
        /// </summary>
        [ForeignKey(nameof(AccountTrade))]
        public Guid? AccountTrade { get; set; }

        /// <summary>
        /// Номер единого капитального счета
        /// </summary>
        [ForeignKey(nameof(AccountCapex))]
        public Guid? AccountCapex { get; set; }

        /// <summary>
        /// Дата транзакции
        /// </summary>
        public DateTime TransactionDate { get; set; }


        /// <summary>
        /// Дата транзакции строка
        /// </summary>
        [NotMapped]
        public string TransactionDateString
        {
            get => TransactionDate.ToShortDateString();
        }

        /// <summary>
        /// Описание транзакции
        /// </summary>
        public string TransactionDescription { get; set; } = "";

        /// <summary>
        /// Курс по транзакции
        /// </summary>
        public decimal ExchangeRate { get; set; } = 1;

        /// <summary>
        /// Сумма транзакции
        /// </summary>
        public decimal TransactionSumma { get; set; }

        /// <summary>
        /// Создать одну строку транзакцию по единому счету
        /// </summary>
        public ReportCapexHistoryTransaction()
        {

        }


    }
}
