using SimpleObjectBrowser.Services;
using SimpleObjectBrowser.ViewModels;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace SimpleObjectBrowser.Views
{
    /// <summary>
    /// Interaction logic for BlobStorageDialog.xaml
    /// </summary>
    public partial class BlobStorageDialog : Window, IAddAccountDialog
    {
        private readonly AzureBlobStorageConnectionStringCredential _model;

        public BlobStorageDialog(AzureBlobStorageConnectionStringCredential model = null)
        {
            InitializeComponent();
            DataContext = _model = model ?? new AzureBlobStorageConnectionStringCredential();
        }

        public AccountViewModel Account { get; private set; }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Account = new AccountViewModel(_model);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Can't connect to account", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
