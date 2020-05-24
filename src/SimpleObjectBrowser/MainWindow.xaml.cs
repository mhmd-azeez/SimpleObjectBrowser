
using Microsoft.Win32;

using SimpleObjectBrowser.Services;
using SimpleObjectBrowser.ViewModels;
using SimpleObjectBrowser.Views;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
                    _viewModel.Prefix = string.Empty;
                    _viewModel.SelectedBucket = bucket;
                    _viewModel.Refresh();
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
                    _viewModel.Prefix = entry.FullName;
                    _viewModel.Refresh();
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

        private void listView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
                e.Handled = true;
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _viewModel.SelectedBlobs = listView.SelectedItems.OfType<BlobViewModel>().ToArray();
        }
    }
}
