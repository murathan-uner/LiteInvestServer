using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvest.Entity.PlazaEntity
{
    /// <summary>
    /// Комиссия биржи
    /// </summary>
    public class FeeBoard
    {
        /// <summary>
        /// Номер сделки
        /// </summary>
        public long Id_deal { get; set; }


        /// <summary>
        /// time
        /// время сделки
        /// </summary>
        DateTime Time { get; set; }


    }
}
