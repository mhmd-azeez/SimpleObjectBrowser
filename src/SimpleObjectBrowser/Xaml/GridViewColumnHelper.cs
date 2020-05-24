using FirstFloor.ModernUI.Windows.Media;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SimpleObjectBrowser.Xaml
{
    public class GridViewColumnHelper
    {


        public static ListView GetUsePercentagesFor(DependencyObject obj)
        {
            return (ListView)obj.GetValue(UsePercentagesForProperty);
        }

        public static void SetUsePercentagesFor(DependencyObject obj, ListView value)
        {
            obj.SetValue(UsePercentagesForProperty, value);
        }

        // Using a DependencyProperty as the backing store for UsePercentagesFor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UsePercentagesForProperty =
            DependencyProperty.RegisterAttached("UsePercentagesFor", typeof(ListView), typeof(GridViewColumnHelper), new PropertyMetadata(null, UsePercentages_Changed));


        private static void UsePercentages_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gridView = d as GridView;
            if (gridView is null) return;

            var listView = e.NewValue as ListView;
            if (listView is null) return;

            listView.SizeChanged += SizeChanged;
            listView.Unloaded += Unloaded;
            RecalculateColumnWidths(listView, gridView);

            void SizeChanged(object sender, SizeChangedEventArgs args)
            {
                RecalculateColumnWidths(listView, gridView);
            }

            void Unloaded(object sender, object args)
            {
                listView.SizeChanged -= SizeChanged;
                listView.Unloaded -= Unloaded;
            }
        }

        // from: https://stackoverflow.com/a/10526024/7003797
        private static void RecalculateColumnWidths(ListView listView, GridView gridView)
        {
            var workingWidth = listView.ActualWidth - SystemParameters.VerticalScrollBarWidth;

            var dynamicColumns = gridView.Columns.Where(c => (double)c.GetValue(WidthPercentProperty) > 0).ToArray();
            var fixedColumns = gridView.Columns.Except(dynamicColumns).ToArray();

            workingWidth = workingWidth - fixedColumns.Sum(c => c.Width);

            foreach (var column in dynamicColumns)
            {
                var percent = (double)column.GetValue(WidthPercentProperty);
                column.Width = Math.Max(1, workingWidth * percent);
            }
        }

        public static double GetWidthPercent(DependencyObject obj)
        {
            return (double)obj.GetValue(WidthPercentProperty);
        }

        public static void SetWidthPercent(DependencyObject obj, double value)
        {
            obj.SetValue(WidthPercentProperty, value);
        }

        // Using a DependencyProperty as the backing store for WidthPercent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WidthPercentProperty =
            DependencyProperty.RegisterAttached("WidthPercent", typeof(double), typeof(GridViewColumnHelper), new PropertyMetadata(0d));


    }
}
