using PropertyChanged;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace LiteInvest.Entity.ServerEntity
{
    [DataContract]
    public class SecurityApi: ADataBaseClass, ICloneable
    {


        [DataMember]
        
        public string id { get; set; }

        [DataMember]
        public string? Isin { get; set; }

        [DataMember]
        public string? ShortName { get; set; }
        
        [DataMember]
        public string? ClassCode { get; set; }
        
        [DataMember]
        public string? FullName { get; set; }
        
        [DataMember]
        public string? Type { get; set; }
        
        [DataMember]
        public decimal Lot { get; set; }
        
        [DataMember]
        [Column(TypeName = "decimal(18, 6)")]
        public decimal PriceStep { get; set; } = 1;
        
        [DataMember]
        [Column(TypeName = "decimal(18, 6)")]
        public decimal PriceStepCost { get; set; } = 1;

        [DataMember]
        public decimal Decimals { get; set; }
        
        [DataMember]
        [Column(TypeName = "decimal(18, 6)")]
        public decimal PriceLimitLow { get; set; }
        
        [DataMember]
        [Column(TypeName = "decimal(18, 6)")]
        public decimal PriceLimitHigh { get; set; }

		[DataMember]
        [JsonIgnore]
		public string? SpecialHash { get; set; }

        [DataMember]
        public DateTime DateUpdateFromMoex { get; set; }

        [JsonIgnore]
        [NotMapped]
        [DoNotNotify]
        public SecurityProfile Profile { get; set; }

		public object Clone()
		{
            return new SecurityApi()
            {
                id = id,
                Isin = Isin,
                ShortName = ShortName,
                ClassCode = ClassCode,
                FullName = FullName,
                Type = Type,
                Lot = Lot,
                PriceStep = PriceStep,
                PriceStepCost = PriceStepCost,
                Decimals = Decimals, 
                PriceLimitLow = PriceLimitLow,
                PriceLimitHigh = PriceLimitHigh,
                Profile = Profile
            };
		}
	}
}
