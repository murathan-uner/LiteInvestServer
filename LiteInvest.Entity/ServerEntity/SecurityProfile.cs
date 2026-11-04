using LiteInvest.Entity.PlazaEntity;

using Newtonsoft.Json;

using System.Collections.Concurrent;
using System.Runtime.Serialization;

/*
 * This class is for saving a security profiles
 * - scale
 * - like trades volume
 * - like trades volume index
 * - automatic scroll flag
 * - ...
 * 
 * Please add every time new profiles are added
 */
namespace LiteInvest.Entity.ServerEntity
{
    [DataContract]
    public class SecurityProfile :ADataBaseClass
    {
        /// <summary>
        /// security ID
        /// </summary>
        [DataMember]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// scale index 0-4
        /// </summary>
        [DataMember]
        public int Scale { get; set; } = 0;

        /// <summary>
        /// </summary>
        [DataMember]
        public List<int> LikeTradesVolume { get; set; } = [];

        /// <summary>
        /// Selected like trades volume index for each instrument
        /// </summary>
        [DataMember]
        public int LikeTradesVolumeIndex { get; set; } = 0;

        /// <summary>
        /// Automatic scrolling ON/OFF flag
        /// </summary>
        [DataMember]
        public bool AutomaticScrollFlag { get; set; } = false;

        public int GetLikeTradeVolume()
        {
            if (LikeTradesVolumeIndex >= 0 && LikeTradesVolumeIndex < LikeTradesVolume.Count)
                return LikeTradesVolume[LikeTradesVolumeIndex];
            return 1;
        }

        public void SetLikeTradeVolume(int index, int newVolume)
        {
            if (newVolume < 1)
                newVolume = 1;
            if (index >= 0 && index < LikeTradesVolume.Count)
                LikeTradesVolume[index] = newVolume;
        }

        public SecurityProfile Clone()
        {
            var cloned = new SecurityProfile
            {
                Id = this.Id,
                Scale = this.Scale,
                LikeTradesVolume = this.LikeTradesVolume,
                LikeTradesVolumeIndex = this.LikeTradesVolumeIndex,
                AutomaticScrollFlag = this.AutomaticScrollFlag
            };

            return cloned;
        }
    }

}
