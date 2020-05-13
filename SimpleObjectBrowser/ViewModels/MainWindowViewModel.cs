using ReactiveUI;
using SimpleObjectBrowser.Services;
using System.Collections.ObjectModel;

namespace SimpleObjectBrowser.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            var s3 = new AccountViewModel(AccountType.AwsS3);
            s3.Name = "Cool Project";

            s3.Buckets.Add(new BucketViewModel { Name = "images" });
            s3.Buckets.Add(new BucketViewModel { Name = "videos" });
            s3.Buckets.Add(new BucketViewModel { Name = "thumbnails" });

            var blobStorage = new AccountViewModel(AccountType.AzureBlobStorage);
            blobStorage.Name = "Bravo Tango";

            blobStorage.Buckets.Add(new BucketViewModel { Name = "documents" });
            blobStorage.Buckets.Add(new BucketViewModel { Name = "downloads" });
            blobStorage.Buckets.Add(new BucketViewModel { Name = "history" });

            var credential = new AzureBlobStorageConnectionStringCredential
            {
                ConnectionString = "DefaultEndpointsProtocol=https;AccountName=blobs4everyone;AccountKey=0nlVNNloSklMYIDF3ZNX56MlB1s+ZxbSk9FeJ+4/1oU+zUiilpCOebcrcDrmQe6nyljOSy4eCiSEAsBQqdpuPA==;EndpointSuffix=core.windows.net",
            };

            var storageAccount = credential.Connect();

            Accounts.Add(s3);
            Accounts.Add(blobStorage);
            Accounts.Add(new AccountViewModel(storageAccount));
        }

        public string Greeting => "Welcome to Avalonia!";

        private ObservableCollection<AccountViewModel> _accounts = new ObservableCollection<AccountViewModel>();
        public ObservableCollection<AccountViewModel> Accounts
        {
            get { return _accounts; }
            set { this.RaiseAndSetIfChanged(ref _accounts, value); }
        }
    }
}