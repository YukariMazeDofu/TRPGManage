using ItoKonnyaku.TrpgManage;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DfWGXX
{
    /// <summary>
    /// DfWGXXWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class DfWGXXWindow : Window
    {
        public DfWGXXWindow()
        {
            InitializeComponent();
            this.canvas.Visibility = Visibility.Visible;    //xaml編集の都合上
            this.DataContext = new TrpgManageBase<XXPC>();
        }
    }
}
