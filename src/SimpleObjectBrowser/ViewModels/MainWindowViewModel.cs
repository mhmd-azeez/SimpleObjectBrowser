using Microsoft.Win32;

using SimpleObjectBrowser.Mvvm;
using SimpleObjectBrowser.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleObjectBrowser.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public MainWindowViewModel()
        {
            DeleteBlobsCommand = new DelegateCommand(p => DeleteBlobs(SelectedBlobs), p => SelectedBlobs?.Count > 0);
            DownloadBlobsCommand = new DelegateCommand(p => DownloadBlobs(), p => SelectedBlobs?.Count > 0);
            RefreshCommand = new DelegateCommand(p => Refresh(), p => SelectedBucket != null);
            UploadFilesCommand = new DelegateCommand(p => UploadFiles(), p => SelectedBucket != null);
            UpCommand = new DelegateCommand(p => Up(), p => Prefix?.Length > 0);
            CopyLinkCommand = new DelegateCommand(p => CopyLink(), p => SelectedBlobs?.Count > 0);
        }

        private string _prefix;
        public string Prefix
        {
            get { return _prefix; }
            set
            {
                if (Set(ref _prefix, value))
                {
                    UpCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private ObservableCollection<AccountViewModel> _accounts = new ObservableCollection<AccountViewModel>();
        public ObservableCollection<AccountViewModel> Accounts
        {
            get { return _accounts; }
            set { Set(ref _accounts, value); }
        }

        private ObservableCollection<TaskViewModel> _tasks = new ObservableCollection<TaskViewModel>();
        public ObservableCollection<TaskViewModel> Tasks
        {
            get { return _tasks; }
            private set { Set(ref _tasks, value); }
        }

        private IList<BlobViewModel> _selectedBlobs;
        public IList<BlobViewModel> SelectedBlobs
        {
            get { return _selectedBlobs; }
            set
            {
                if (Set(ref _selectedBlobs, value))
                {
                    DeleteBlobsCommand.RaiseCanExecuteChanged();
                    DownloadBlobsCommand.RaiseCanExecuteChanged();
                    CopyLinkCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private BucketViewModel _selectedBucket;
        public BucketViewModel SelectedBucket
        {
            get { return _selectedBucket; }
            set
            {
                if (Set(ref _selectedBucket, value))
                {
                    RefreshCommand.RaiseCanExecuteChanged();
                    UploadFilesCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private int _pageSize = 50;
        public int PageSize
        {
            get { return _pageSize; }
            set
            {
                if (Set(ref _pageSize, value))
                {
                    Load();
                }
            }
        }

        public DelegateCommand DeleteBlobsCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand UploadFilesCommand { get; }
        public DelegateCommand DownloadBlobsCommand { get; }
        public DelegateCommand UpCommand { get; }
        public DelegateCommand CopyLinkCommand { get; }

        public void SaveAccounts()
        {
            ConfigService.SaveAccounts(Accounts);
        }

        public void Up()
        {
            Prefix = PathHelper.GetParent(Prefix);
            Load();
        }

        public void DownloadBlobs()
        {
            if (SelectedBucket is null)
                return;

            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();

                if (result != System.Windows.Forms.DialogResult.OK)
                    return;

                var entries = SelectedBlobs.Select(b => b.NativeBlob).ToList();
                AddTask(new DownloadBlobsTaskViewModel(dialog.SelectedPath, entries));
            }
        }

        private async void CopyLink()
        {
            if (SelectedBlobs?.Count < 0)
                return;

            SelectedBucket.IsBusy = true;

            try
            {
                var blobs = new List<IBlob>();

                blobs.AddRange(SelectedBlobs.Select(b => b.NativeBlob).OfType<IBlob>());

                foreach (var dirVm in SelectedBlobs.Where(b => b.IsDirectory))
                {
                    var children = await dirVm.NativeBlob.ListAllBlobsAsync();
                    blobs.AddRange(children);
                }

                var builder = new StringBuilder();

                foreach (var blob in blobs)
                {
                    var link = blob.GetLink(TimeSpan.FromHours(24));
                    builder.AppendLine(link.AbsoluteUri);
                }

                Clipboard.SetText(builder.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                SelectedBucket.IsBusy = false;
            }
        }

        public void UploadFiles()
        {
            if (SelectedBucket is null)
                return;

            var dialog = new OpenFileDialog();
            dialog.Multiselect = true;
            var result = dialog.ShowDialog();

            if (result != true)
                return;

            var files = dialog.FileNames.Select(p =>
            {
                var name = p.Split('\\').Last();
                var extension = name.Split('.').Last();
                var contentType = MimeTypes.MimeTypeMap.GetMimeType(extension);

                using (var stream = System.IO.File.OpenRead(p))
                {
                    return new FileInfo
                    {
                        OpenStream = () => System.IO.File.OpenRead(p),
                        ContentType = contentType,
                        Length = System.IO.File.OpenRead(p).Length,
                        Name = name
                    };
                }
            });

            var task = new UploadBlobsTaskViewModel(files, Prefix, SelectedBucket.NativeBucket);
            AddTask(task);
        }

        private void AddTask(TaskViewModel task)
        {
            task.Succeeded += Task_Succeeded;
            Tasks.Add(task);

            task.Completed += Task_Completed;
            task.Failed += Task_Failed;

            _ = task.StartAsync();

            void Task_Completed(object sender, EventArgs e)
            {
                task.Completed -= Task_Completed;
                task.Failed -= Task_Failed;
                Tasks.Remove(task);
                Refresh();
            }

            void Task_Failed(object sender, string e)
            {
                MessageBox.Show(e, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Task_Succeeded(object sender, EventArgs e)
        {
            Refresh();
        }

        internal async void Refresh()
        {
            if (SelectedBucket is null) return;

            try
            {
                await SelectedBucket.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        internal async void Load()
        {
            if (SelectedBucket is null) return;

            try
            {
                await SelectedBucket.LoadAsync(Prefix, PageSize);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        internal void DeleteBlobs(IList<BlobViewModel> blobs)
        {
            var response = MessageBox.Show($"Are you sure you want to delete these {blobs.Count} items?", "Confirm", MessageBoxButton.YesNo);
            if (response != MessageBoxResult.Yes)
                return;

            var prefixes = blobs.Select(b => b.FullName).ToArray();

            if (SelectedBucket is null || prefixes.Length == 0) return;

            var task = new DeleteBlobsTaskViewModel(prefixes, SelectedBucket.NativeBucket);
            AddTask(task);
        }
    }
}