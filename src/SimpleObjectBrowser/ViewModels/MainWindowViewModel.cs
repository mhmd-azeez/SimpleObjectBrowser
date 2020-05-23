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

        public void UploadFile(IEnumerable<string> paths)
        {
            if (SelectedBucket is null)
                return;

            var files = paths.Select(p =>
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
            _ = task.StartAsync();
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
                await SelectedBucket.LoadAsync(Prefix);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        internal void DeleteBlobs(IEnumerable<BlobViewModel> blobs)
        {
            var prefixes = blobs.Select(b => b.FullName).ToArray();

            if (SelectedBucket is null || prefixes.Length == 0) return;

            var task = new DeleteBlobsTaskViewModel(prefixes, SelectedBucket.NativeBucket);
            AddTask(task);
        }
    }
}