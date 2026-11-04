using PropertyChanged;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace LiteInvest.Entity.ServerEntity
{
    [DataContract]
    [AddINotifyPropertyChangedInterface]
    public class  ADataBaseClass :INotifyPropertyChanged
    {
        
        /// <summary>
        /// GUID
        /// </summary>
        [DataMember]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Guid { get; set; } = Guid.NewGuid();

        public event PropertyChangedEventHandler? PropertyChanged;

        public void ClearEventPropertyChanged()
        {
            PropertyChanged = null; // Удаляет всех подписчиков
        }
    }
}
