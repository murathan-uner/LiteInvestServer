using LiteInvest.Entity.PlazaEntity;

using Newtonsoft.Json;

using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.Serialization;

/*
 * This class is for saving a dialog profile
 */
namespace LiteInvest.Entity.ServerEntity
{
    [DataContract]
    public class DialogProfile
    {
        [DataMember]
        public DialogBound? bound { get; set; }
    }

    public class DialogBound {
        [DataMember]
        public double left { get; set; }

        [DataMember]
        public double top { get; set; }

        [DataMember]
        public double width { get; set; }

        [DataMember]
        public double height { get; set; }
    }

    public enum DialogType
    {
        InstrumentPicker,
        ActiveOrders,
        ActivePositions,
        Finrez
    }
}
