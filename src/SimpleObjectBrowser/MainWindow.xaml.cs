using Fluent;
using SimpleObjectBrowser.Services;
using SimpleObjectBrowser.ViewModels;
using SimpleObjectBrowser.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleObjectBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = _viewModel = new MainWindowViewModel();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var accounts = ConfigService.GetSavedAccounts();
            _viewModel.Accounts = new ObservableCollection<AccountViewModel>(accounts);
        }

        private async void TreeView_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = e.OriginalSource as TreeViewItem;
            if (item?.DataContext is AccountViewModel accountViewModel)
            {
                try
                {
                    await accountViewModel.ExpandAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private async void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is BucketViewModel bucket)
            {
                try
                {
                    var prefix = string.Empty;
                    await bucket.LoadAsync(prefix);
                    _viewModel.Prefix = prefix;
                    _viewModel.SelectedBucket = bucket;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private async void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (listView.SelectedItem is BlobViewModel entry && entry.IsDirectory)
            {
                try
                {
                    var prefix = entry.FullName;
                    await _viewModel.SelectedBucket.LoadAsync(prefix);
                    _viewModel.Prefix = prefix;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void addBlobStorageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            addAccountButton.IsDropDownOpen = false;
            var window = new BlobStorageDialog();
            AddAccount(window);
        }

        private void AddAccount(IAddAccountDialog window)
        {
            var result = window.ShowDialog();
            if (result == true)
            {
                _viewModel.Accounts.Add(window.Account);
                _viewModel.SaveAccounts();
            }
        }

        private void addS3MenuItem_Click(object sender, RoutedEventArgs e)
        {
            addAccountButton.IsDropDownOpen = false;
            var window = new S3Dialog();
            AddAccount(window);
        }

        private void forgetAccountMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var account = (AccountViewModel)((FrameworkElement)sender).DataContext;
            _viewModel.Accounts.Remove(account);
            _viewModel.SaveAccounts();
        }
    }
}
