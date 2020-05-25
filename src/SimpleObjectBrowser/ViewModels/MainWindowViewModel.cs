using Microsoft.Win32;

using SimpleObjectBrowser.Mvvm;
using SimpleObjectBrowser.Services;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleObjectBrowser.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        public MainWindowViewModel()
        {
            DeleteBlobsCommand = new DelegateCommand(p => DeleteBlobs(SelectedBlobs), p => SelectedBlobs?.Count > 0);
            RefreshCommand = new DelegateCommand(p => Refresh(), p => SelectedBucket != null);
            UploadFilesCommand = new DelegateCommand(p => UploadFiles(), p => SelectedBucket != null);
        }

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

        private int _pageSize = 10;
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

        public void SaveAccounts()
        {
            ConfigService.SaveAccounts(Accounts);
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