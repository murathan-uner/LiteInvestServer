using LiteInvest.Entity.PlazaEntity;

using Newtonsoft.Json;

using PropertyChanged;

using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;


namespace LiteInvest.Entity.ServerEntity
{
    public enum Roles
    {
        Usual,
        Admin
    }


    [DataContract]
    public class User : ADataBaseClass
    {
        public User(string _login, string _pass)
        {
            Login = _login;
            Password = _pass; 
        }



        public User()
        {
            
        }


        /// <summary>
        /// Имя например ФИО
        /// </summary>
        [DataMember]
        public string? Name { get; set; }

		[DataMember]
        public string Login { get; set; }

        //TODO: pass HASH
        [JsonIgnore]
        [DataMember]
        public string Password { get; set; }

        
        [DataMember]
        /// <summary>
        /// Может ли данный юзер торговать
        /// Будет отключено в админке.
        /// </summary>
        public bool CanInput { get; set; }

        
        [DataMember]
        public bool Admin { get; set; }


        /// <summary>
        /// Страна трейдера
        /// </summary>
        [DataMember]
        public string? Country { get; set; }

        /// <summary>
        /// Город трейдера
        /// </summary>
        [DataMember]
        public string? City { get; set; }


        /// <summary>
        /// Адрес места жительства трейдера
        /// </summary>
        [DataMember]
        public string? AddressResidential { get; set; }

        /// <summary>
        /// Телефон трейдера
        /// </summary>
        [DataMember]
        public string? PhoneNumber { get; set; }


        /// <summary>
        /// E-mail трейдера
        /// </summary>
        [DataMember]
        public string? Email { get; set; }

        /// <summary>
        /// Примечание
        /// </summary>
        [DataMember]
        public string? Description { get; set; }

        /// <summary>
        /// Примечание
        /// </summary>
        [DataMember]
        [NotMapped]
        public TabProfile MasterTab { get; set; }

        /// <summary>
        /// key = tabId, value = tab profile
        /// </summary>
        [DataMember]
        [JsonIgnore]
        [NotMapped]
        //public ConcurrentDictionary<string, TabProfile> Tabs { get; set; } = [];
        public List<TabProfile> Tabs { get; set; } = [];

        /// <summary>
        /// key = tabId, value = (key = SpecialHash, value = security profile)
        /// </summary>
        [DataMember]
        [JsonIgnore]
        [NotMapped]
        public ConcurrentDictionary<string, ConcurrentDictionary<string, SecurityProfile>> Instruments { get; set; } = [];

        /// <summary>
        /// События на подписку на мои ордера 
        /// </summary>
        [JsonIgnore]
        [NotMapped]
        public ConcurrentDictionary<string, Action<List<Order>>> PrivateOrderEventsForUsers { get;  set; } = [];

        /// <summary>
        /// События на подписку на мои ордера 
        /// </summary>
        [JsonIgnore]
        [NotMapped]
        public ConcurrentDictionary<string, Action<MarketDepth>> MarketDepthsSubscriptions { get; set; } = [];

        /// <summary>
        /// Acces Token
        /// </summary>
        [JsonIgnore]
        public string? AccessToken { get; set; }


        /// <summary>
        /// Refresh Token
        /// </summary>
        [DataMember]
        [JsonIgnore]
        public string? RefreshToken { get; set; }

        /// <summary>
        /// IP Adress User
        /// </summary>
        [DataMember]

        public string? IPAddress { get; set; }

        /// <summary>
        /// Bind IP Adress 
        /// </summary>
        [DataMember]
        [JsonIgnore]
        public bool BindIP { get; set; }

        /// <summary>
        /// User Agent Browser 
        /// </summary>
        [DataMember]
        public string? UserAgentBrowser { get; set; }

        /// <summary>
        ///Time create Refresh Token 
        /// </summary>
        [DataMember]
        public DateTime TimeRefreshToken { get; set; }

        /// <summary>
        ///Time last login  
        /// </summary>
        [DataMember]
        public DateTime TimeLastLogin{ get; set; }

        /// <summary>
        /// key = tab id, value = (key = SpecialHash, value = order index starts from 0)
        /// </summary>
        [DataMember]
        [NotMapped]
        public ConcurrentDictionary<string, ConcurrentDictionary<string, int>> InstrumentOrders { get; set; } = [];

        /// <summary>
        /// key = security id, value = SecurityProfile
        /// </summary>
        [DataMember]
        [JsonIgnore]
        [NotMapped]
        public ConcurrentDictionary<string, SecurityProfile> FavoriteInstruments { get; set; } = [];

        /// <summary>
        /// key = security id, value = (order index, SecurityProfile)
        /// </summary>
        [DataMember]
        [JsonIgnore]
        [NotMapped]
        public ConcurrentDictionary<string, (int, SecurityProfile)> RecentInstruments { get; set; } = [];

        [NotMapped]
        public int? RecentInstrumentsLastOrderIndex { get; set; } = -1;

        [DataMember]
        [NotMapped]
        public InstrumentPickerProfile? InstrumentPickerProfile { get; set; }

        [DataMember]
        [NotMapped]
        public ActiveOrdersDialogProfile? ActiveOrdersDialogProfile { get; set; }

        [DataMember]
        [NotMapped]
        public ActivePositionsDialogProfile? ActivePositionsDialogProfile { get; set; }

        [JsonIgnore]
        [DoNotNotify]
        public string? XMLserializer
        {
            get
            {
                StringBuilder stringOutput = new();
                using (var s = XmlWriter.Create(stringOutput))
                {
                    var r = new DataContractSerializer(typeof(User));
                    r.WriteObject(s, this);
                }
                var res = stringOutput.ToString();
                return res;

            }
            set
            {
                xMLserializer = value;
            }
        }


        [JsonIgnore]
        [DoNotNotify]
        private string? xMLserializer
        {
            get;
            set;
        }


        public void SelfXMLDeSerializer()
        {
            try
            {
                if (string.IsNullOrEmpty(xMLserializer))
                    return;

                byte[]? b = Encoding.Unicode.GetBytes(xMLserializer);

                using (var reader = XmlDictionaryReader.CreateTextReader(b, new XmlDictionaryReaderQuotas()))
                {
                    var serializer = new DataContractSerializer(typeof(User));
                    User? res = (User)serializer.ReadObject(reader, true);

                    if (res != null)
                    {
                        this.MasterTab = res.MasterTab;
                        this.Tabs = res.Tabs;
                        this.Instruments = res.Instruments ?? new();
                        this.InstrumentOrders = res.InstrumentOrders ?? new();
                        this.FavoriteInstruments = res.FavoriteInstruments ?? new();
                        this.RecentInstruments = res.RecentInstruments ?? new();
                    }
                    res = null;
                }
            }
            catch (Exception ex)
            {
                ServerLogger.Log(ex.Message);
                Console.WriteLine(ex);
            }
        }



    }

}
