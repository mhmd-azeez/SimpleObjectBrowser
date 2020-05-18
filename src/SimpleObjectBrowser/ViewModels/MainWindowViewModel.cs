using SimpleObjectBrowser.Mvvm;
using SimpleObjectBrowser.Services;
using System;
using System.Collections.ObjectModel;

namespace SimpleObjectBrowser.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _prefix;
        public string Prefix
        {
            get { return _prefix; }
            set { Set(ref _prefix, value); }
        }

        private ObservableCollection<AccountViewModel> _accounts = new ObservableCollection<AccountViewModel>();
        public ObservableCollection<AccountViewModel> Accounts
        {
            get { return _accounts; }
            set { Set(ref _accounts, value); }
        }

        private BucketViewModel _selectedBucket;
        public BucketViewModel SelectedBucket
        {
            get { return _selectedBucket; }
            set { Set(ref _selectedBucket, value); }
        }

        public void SaveAccounts()
        {
            ConfigService.SaveAccounts(Accounts);
        }
    }
}