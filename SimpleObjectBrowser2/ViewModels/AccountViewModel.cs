using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ReactiveUI;
using SimpleObjectBrowser.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace SimpleObjectBrowser.ViewModels
{
    public enum AccountType
    {
        AwsS3,
        AzureBlobStorage,
    }

    public class AccountViewModel : ViewModelBase
    {
        private readonly IStorageAccount _account;

        public AccountViewModel(AccountType type)
        {
            string path;
            if (type == AccountType.AwsS3)
                path = $"Assets/s3.png";
            else
                path = $"Assets/blob-storage.png";

            Icon = new Bitmap(path);
        }

        public AccountViewModel(IStorageAccount account) : this(account.Type)
        {
            _account = account;
            Name = account.Name;

            IsBusy = true;
            account.ListBucketsAsync().ToObservable().Select(nativeBuckets => nativeBuckets.Select(n => new BucketViewModel(n)))
                                    .Subscribe(buckets =>
                                    {
                                        Buckets = new ObservableCollection<BucketViewModel>(buckets);
                                        IsBusy = false;
                                    });
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value); }
        }

        public Bitmap Icon { get; set; }

        private ObservableCollection<BucketViewModel> _buckets = new ObservableCollection<BucketViewModel>();

        public ObservableCollection<BucketViewModel> Buckets
        {
            get { return _buckets; }
            set { this.RaiseAndSetIfChanged(ref _buckets, value); }
        }


        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { this.RaiseAndSetIfChanged(ref _isBusy, value); }
        }

    }

    public class BucketViewModel : ViewModelBase
    {
        private string _name;
        private IStorageBucket _nativeBucket;

        public BucketViewModel()
        {
            LoadCommand = ReactiveCommand.Create(() => Load());
        }

        private async void Load()
        {
            IsBusy = true;
            try
            {
                var blobs = await _nativeBucket.ListBlobsAsync();
                Blobs = new ObservableCollection<BlobViewModel>(blobs.Select(b => new BlobViewModel(this, b)));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
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
            LoadCommand = ReactiveCommand.Create(() => Load());
        }

        public string Name
        {
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { this.RaiseAndSetIfChanged(ref _isBusy, value); }
        }


        private ObservableCollection<BlobViewModel> _blobs;
        public ObservableCollection<BlobViewModel> Blobs
        {
            get { return _blobs; }
            set { this.RaiseAndSetIfChanged(ref _blobs, value); }
        }

        public ReactiveCommand<Unit, Unit> LoadCommand { get; }
    }

    public class BlobViewModel : ViewModelBase
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value); }
        }

        private long _length;
        public long Length
        {
            get { return _length; }
            set { this.RaiseAndSetIfChanged(ref _length, value); }
        }

        private DateTimeOffset? _lastModified;
        public DateTimeOffset? LastModified
        {
            get { return _lastModified; }
            set { this.RaiseAndSetIfChanged(ref _lastModified, value); }
        }

        private string _contentType;
        private BucketViewModel _parent;
        private IBlob _native;

        public BlobViewModel(BucketViewModel parent, IBlob native)
        {
            _parent = parent;
            _native = native;

            Name = native.Name;
            LastModified = native.LastModified;
            Length = native.Length;
            ContentType = native.ContentType;
        }

        public string ContentType
        {
            get { return _contentType; }
            set { this.RaiseAndSetIfChanged(ref _contentType, value); }
        }

    }
}
