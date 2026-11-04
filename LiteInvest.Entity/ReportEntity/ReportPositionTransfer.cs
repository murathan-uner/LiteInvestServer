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
    public class ReportPositionTransfer : ADataBaseClass
    {
        
        /// <summary>
        /// Площадка
        /// </summary>
        public BoardEnum Board { get; set; }


        [ForeignKey(nameof(AccountTrade))]
        public Guid? AccountTradeGuid { get; set; }
       

        /// <summary>
        /// Краткое наименование инструмента (SBER, GAZP, SiH6)
        /// </summary>
        public string SecIsin { get; set; } = "";

        /// <summary>
        /// Объем, количество приобретенного инструмента, в штуках, фьючерсы штук лотов.
        /// </summary>
        public decimal Volume { get; set; }

        /// <summary>
        /// Направление сделки
        /// </summary>
        public Side Direction { get; set; }

        /// <summary>
        /// Отчетная Дата
        /// </summary>
        public DateTime DateReport { get; set; }



        public ReportPositionTransfer() { }
    }
}
