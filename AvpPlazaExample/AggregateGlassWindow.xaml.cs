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

namespace AvpPlazaTester
{
    /// <summary>
    /// Логика взаимодействия для AggregateGlassWindow.xaml
    /// </summary>
    public partial class AggregateGlassWindow : Window
    {
        TestAggregateGlass testAggregateGlass;
        public AggregateGlassWindow(TestAggregateGlass testAggregateGlass)
        {
            InitializeComponent();
            this.testAggregateGlass = testAggregateGlass;
            testAggregateGlass.SetDispatcher(this);
            DataContext = testAggregateGlass;
        }

        private void ButtonAggregateGlass_Click(object sender, RoutedEventArgs e)
        {
            testAggregateGlass.CommandSubscribeAggregateGlass();
        }
    }
}
