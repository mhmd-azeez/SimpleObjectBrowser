using SimpleObjectBrowser.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SimpleObjectBrowser.Xaml
{
    public class TreeItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate AccountTemplate { get; set; }
        public DataTemplate BucketTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is AccountViewModel)
                return AccountTemplate;

            return BucketTemplate;
        }
    }
}
