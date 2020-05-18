using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SimpleObjectBrowser.Mvvm
{
    public class BindableBase : INotifyPropertyChanged
    {
        public bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(field, value)) return false;

            field = value;
            RaisePropertyChanged(propertyName);

            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
