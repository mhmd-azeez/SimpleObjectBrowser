using MimeTypes;

using SimpleObjectBrowser.Mvvm;
using SimpleObjectBrowser.Services;

using System;
using System.Collections.Generic;
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
        GoogleCloudStorage,
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

        public async Task LoadAsync(string prefix, int pageSize)
        {
            IsBusy = true;
            try
            {
                var query = new ListQuery
                {
                    Prefix = prefix,
                    Heirarchical = true,
                    PageSize = pageSize
                };

                CurrentPage = await NativeBucket.ListEntriesAsync(query);
                Blobs = ToBlobs(CurrentPage);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task GoForeward()
        {
            if (CanGoForeward == false) return;

            IsBusy = true;
            try
            {
                CurrentPage = await CurrentPage.GetNextPage();
                Blobs = ToBlobs(CurrentPage);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public void GoBackward()
        {
            if (CanGoBackward == false) return;

            CurrentPage = CurrentPage.Previous;
            Blobs = ToBlobs(CurrentPage);
        }

        public BucketViewModel(IStorageBucket bucket)
        {
            NativeBucket = bucket;
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

        private bool _canGoForeward;
        public bool CanGoForeward
        {
            get { return _canGoForeward; }
            private set { Set(ref _canGoForeward, value); }
        }

        private bool _canGoBackward;
        public bool CanGoBackward
        {
            get { return _canGoBackward; }
            private set { Set(ref _canGoBackward, value); }
        }

        private ObservableCollection<BlobViewModel> _blobs;
        private IPagedResult<IEnumerable<IEntry>> _currentPage;

        public ObservableCollection<BlobViewModel> Blobs
        {
            get { return _blobs; }
            set { Set(ref _blobs, value); }
        }

        internal async Task Refresh()
        {
            if (CurrentPage is null)
            {
                return;
            }

            IsBusy = true;
            try
            {
                CurrentPage = await CurrentPage.Refresh();
                Blobs = ToBlobs(CurrentPage);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private ObservableCollection<BlobViewModel> ToBlobs(IPagedResult<IEnumerable<IEntry>> currentPage)
        {
            var pageOffset = (currentPage.PageNumber - 1) * currentPage.PageSize;
            return new ObservableCollection<BlobViewModel>(currentPage.Result.Select((b, i) =>
                    new BlobViewModel(this, b, i + pageOffset + 1)));
        }

        public IStorageBucket NativeBucket { get; }
        public IPagedResult<IEnumerable<IEntry>> CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                if (_currentPage != null)
                {
                    CanGoBackward = _currentPage.HasPreviousPage();
                    CanGoForeward = _currentPage.HasNextPage();
                }
                else
                {
                    CanGoBackward = CanGoForeward = false;
                }
            }
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

        private string _tier;
        public string Tier
        {
            get { return _tier; }
            set { Set(ref _tier, value); }
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

        public BlobViewModel(BucketViewModel parent, IEntry native, int number)
        {
            _parent = parent;
            NativeBlob = native;
            Number = number;
            FullName = native.Key;
            Name = native.Key.TrimEnd('/').Split('/').LastOrDefault();

            if (native is IBlob blob)
            {
                IsDirectory = false;
                LastModified = blob.LastModified;
                Length = blob.Length;
                ContentType = blob.ContentType;
                Tier = blob.Tier;
            }
            else
            {
                IsDirectory = true;
            }

            Icon = GetIcon();
        }

        private ImageSource GetIcon()
        {
            string path;

            if (IsDirectory)
            {
                path = $"Assets/directory.png";
            }
            else
            {
                if (ContentType != null)
                {
                    var extension = MimeTypeMap.GetExtension(ContentType, false);
                    if (string.IsNullOrWhiteSpace(extension) == false)
                    {
                        var icon = IconManager.FindIconForFilename(extension, true);
                        if (icon != null)
                        {
                            return icon;
                        }
                    }
                }

                path = "Assets/file.png";
            }

            return new BitmapImage(new Uri($"pack://application:,,,/{path}"));
        }

        public string ContentType
        {
            get { return _contentType; }
            set { Set(ref _contentType, value); }
        }

        public int Number { get; }
        public IEntry NativeBlob { get; set; }
    }
}
