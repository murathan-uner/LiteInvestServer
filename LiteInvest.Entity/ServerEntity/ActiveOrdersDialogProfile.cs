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
    public class ActiveOrdersDialogProfile : DialogProfile
    {
        [DataMember]
        public string? activeTab { get; set; } // null | 'active' | 'canceled' | 'executed'

        [DataMember]
        public string? sortColumn { get; set; } // null | 'symbol' | 'price' | 'volume' | 'timestamp' | 'side'(tab:executed) | 'secId' | 'close'(tab:active)

        [DataMember]
        public string? sortDirection { get; set; } // null | asc | desc

        [DataMember]
        public List<double>? columnWidthsA { get; set; } // null | list of active column widths in pixel

        [DataMember]
        public List<double>? columnWidthsC { get; set; } // null | list of canceled column widths in pixel

        [DataMember]
        public List<double>? columnWidthsE { get; set; } // null | list of executed column widths in pixel
    }

}
