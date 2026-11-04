using LiteInvest.Entity.PlazaEntity;

using Newtonsoft.Json;

using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.Serialization;

/*
 * This class is for saving a active orders dialog profile
 */
namespace LiteInvest.Entity.ServerEntity
{
    [DataContract]
    public class ActivePositionsDialogProfile : DialogProfile
    {
        [DataMember]
        public string? activeTab { get; set; } // null | 'opened' | 'closed'

        [DataMember]
        public string? sortColumn { get; set; } // null | 'symbol' | 'enterPrice' | 'volume' | 'side' | 'secId' | 'close'
                                                // null | 'symbol' | 'PnL' | 'volume(posValue)' | 'side' | 'enterPrice' | 'exitPrice' | 'timestamp' | 'secId'

        [DataMember]
        public string? sortDirection { get; set; } // null | asc | desc

        [DataMember]
        public List<double>? columnWidthsO { get; set; } // null | list of opened column widths in pixel

        [DataMember]
        public List<double>? columnWidthsC { get; set; } // null | list of closed column widths in pixel

        [DataMember]
        public int? pnlVisibleType { get; set; } // null | 0: P | 1: R | 2: Percent
    }

}
