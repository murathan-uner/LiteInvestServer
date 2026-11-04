using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvest.Entity.Helpers
{
    public class NotifyPropertyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            OnPropertyChangedAsync(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task OnPropertyChangedAsync(object o, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            await Task.Run(() => PropertyChanged?.Invoke(o, propertyChangedEventArgs));
            return;
        }

    }
}
