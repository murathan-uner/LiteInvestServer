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
    public class InstrumentPickerProfile : DialogProfile
    {
        [DataMember]
        public string? activeTab { get; set; } // null | 'all' | 'favorite' | 'recent'

        [DataMember]
        public string? sortColumn { get; set; } // null | 'favorite' | 'symbol' | 'longName' | 'updatedAt'

        [DataMember]
        public string? sortDirection { get; set; } // null | asc | desc

        [DataMember]
        public List<double>? columnWidths { get; set; } // null | list of widths in pixel
    }

}
