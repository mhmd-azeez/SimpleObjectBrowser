using SimpleObjectBrowser.ViewModels;

namespace SimpleObjectBrowser.Views
{
    public interface IAddAccountDialog
    {
        AccountViewModel Account { get; }
        bool? ShowDialog();
    }
}