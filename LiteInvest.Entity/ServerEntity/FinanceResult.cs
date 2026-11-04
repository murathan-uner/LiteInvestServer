using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvest.Entity.ServerEntity
{
    public class FinanceResult
    {

        /// <summary>
        /// Площадка – наименование торговой площадки;
        /// </summary>
        public BoardEnum Board { get; set; }

        /// <summary>
        /// Финрез – вариационная маржа минус комиссия;
        /// </summary>
        public decimal SummaFinRes { get => Margin - Commission; } 


        /// <summary>
        /// Маржинальная прибыль
        /// </summary>
        public decimal Margin { get; set; }

        /// <summary>
        /// Общая комиссия за совершенные сделки
        /// </summary>
        public decimal Commission { get; set; }

        /// <summary>
        /// Просадка (Margin-Call) – максимальное значение убытка, при котором вы будете обязаны прекратить торговлю (красная зона). 
        /// Желтая зона – 80% от просадки. При достижении одной из зон соответствующая строка станет красной или желтой;
        /// </summary>
        public decimal DrawDown { get; set; }

        /// <summary>
        /// Капитальный счет
        /// </summary>
        public string AccountCapex { get; set; }

        /// <summary>
        /// Торговй  счет
        /// </summary>
        public string AccountTrade { get; set; }


        /// <summary>
        /// Логин пользователя 
        /// </summary>
        public string UserName { get; set; }


        /// <summary>
        /// Депо – количество денежных средств на счете, учитывая маржинальную торговлю.
        /// </summary>
        public decimal DepositMoney { get; set; }

        /// <summary>
        /// Пул – прибыль, отправленная из финреза в пул (сохраняется до конца торговой сессии и зачисляется на клиринге. Отправку можно осуществить, нажав на кнопку «Пул»).
        /// </summary>
        public decimal Pool { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("FinanceResult:");
            foreach (var prop in typeof(FinanceResult).GetProperties())
            {
                sb.AppendFormat("{0}={1};",prop.Name, prop.GetValue(this));
            }
            return sb.ToString();
        }
    }
}
