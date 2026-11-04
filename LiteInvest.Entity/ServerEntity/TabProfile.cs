using LiteInvest.Entity.PlazaEntity;

using Newtonsoft.Json;

using System.Collections.Concurrent;
using System.Runtime.Serialization;

/*
 * This class is for saving a table profiles
 * - caption
 * - position
 * - size
 * - ...
 * 
 * Please add every time new profiles are added
 */
namespace LiteInvest.Entity.ServerEntity
{
    [DataContract]
    public class TabProfile
    {
        /// <summary>
        /// Tab ID
        /// </summary>
        [DataMember]
        public string Id { get; set; } = string.Empty;

        [DataMember]
        public string Caption { get; set; } = string.Empty;

        /// <summary>
        /// </summary>
        [DataMember]
        public int Left { get; set; } = 0;

        /// <summary>
        /// </summary>
        [DataMember]
        public int Top { get; set; } = 0;

        /// <summary>
        /// </summary>
        [DataMember]
        public int Width { get; set; } = 0;

        /// <summary>
        /// </summary>
        [DataMember]
        public int Height { get; set; } = 0;
    }

}
