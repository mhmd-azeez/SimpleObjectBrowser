using SimpleObjectBrowser.Mvvm;
using SimpleObjectBrowser.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SimpleObjectBrowser.ViewModels
{
    public enum AccountType
    {
        AwsS3,
        AzureBlobStorage,
    }

    public class AccountViewModel : BindableBase
    {
        private readonly IStorageAccount _account;

        public AccountViewModel(ICredential credential)
        {
            Credential = credential;
            _account = credential.Connect();

            string path = $"Assets/blob-storage.png";
            if (_account.Type == AccountType.AwsS3)
                path = $"Assets/s3.png";

            Icon = new BitmapImage(new Uri($"pack://application:,,,/{path}"));

            Name = _account.Name;
        }

        public ICredential Credential { get; }

        public async Task ExpandAsync()
        {
            try
            {
                IsBusy = true;

                var buckets = await _account.ListBucketsAsync();
                Buckets = new ObservableCollection<BucketViewModel>(buckets.Select(n => new BucketViewModel(n)));

            }
            finally
            {
                IsBusy = false;
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value); }
        }

        public ImageSource Icon { get; set; }

        private ObservableCollection<BucketViewModel> _buckets = new ObservableCollection<BucketViewModel>();

        public ObservableCollection<BucketViewModel> Buckets
        {
            get { return _buckets; }
            set { Set(ref _buckets, value); }
        }


        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { Set(ref _isBusy, value); }
        }

    }

    public class BucketViewModel : BindableBase
    {
        private string _name;
        private IStorageBucket _nativeBucket;

        public BucketViewModel()
        {
            // LoadCommand = new DelegateCommand(p => Load(), p => _blobs is null);
        }

        public async Task LoadAsync(string prefix)
        {
            IsBusy = true;
            try
            {
                var blobs = await _nativeBucket.ListEntriesAsync(prefix, true);
                Blobs = new ObservableCollection<BlobViewModel>(blobs.Select(b => new BlobViewModel(this, b)));
            }
            finally
            {
                IsBusy = false;
            }
        }

        public BucketViewModel(IStorageBucket bucket)
        {
            _nativeBucket = bucket;
            Name = bucket.Name;
            // LoadCommand = new DelegateCommand(p => Load(), p => _blobs is null);
        }

        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { Set(ref _isBusy, value); }
        }


        private ObservableCollection<BlobViewModel> _blobs;
        public ObservableCollection<BlobViewModel> Blobs
        {
            get { return _blobs; }
            set { Set(ref _blobs, value); }
        }
    }

    public class BlobViewModel : BindableBase
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value); }
        }

        private string _fullName;
        public string FullName
        {
            get { return _fullName; }
            set { Set(ref _fullName, value); }
        }

        private long? _length;
        public long? Length
        {
            get { return _length; }
            set { Set(ref _length, value); }
        }

        private DateTimeOffset? _lastModified;
        public DateTimeOffset? LastModified
        {
            get { return _lastModified; }
            set { Set(ref _lastModified, value); }
        }

        public ImageSource Icon { get; }

        public bool IsDirectory { get; }

        private string _contentType;
        private BucketViewModel _parent;
        private IEntry _native;

        public BlobViewModel(BucketViewModel parent, IEntry native)
        {
            _parent = parent;
            _native = native;

            FullName = native.Name;
            Name = native.Name.TrimEnd('/').Split('/').LastOrDefault();

            if (native is IBlob blob)
            {
                IsDirectory = false;
                LastModified = blob.LastModified;
                Length = blob.Length;
                ContentType = blob.ContentType;
            }
            else
            {
                IsDirectory = true;
            }

            string path = "Assets/file.png";

            if (IsDirectory)
                path = $"Assets/directory.png";

            Icon = new BitmapImage(new Uri($"pack://application:,,,/{path}"));
        }

        public string ContentType
        {
            get { return _contentType; }
            set { Set(ref _contentType, value); }
        }
    }
}
