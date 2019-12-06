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

/* 2019移行メモ
    https://qiita.com/nqdior/items/c9634f5c753cf4e53d4d

    System.Windows.Interactivity及びMicrosoft.Expression.Interactivity.Coreがなくなったので、
    代わりにNugetでMicrosoft.Xaml.Behaviors.Wpfを導入し、
    xmlns: i, c, ei を
    xmlns:ic="http://schemas.microsoft.com/xaml/behaviors"
    に変更した。

*/

namespace DfWGIS
{
    /// <summary>
    /// DfWGISWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class DfWGISWindow : Window
    {
        public DfWGISWindow()
        {
            InitializeComponent();
            this.canvas.Visibility = Visibility.Visible;    //xaml編集の都合上
            this.DataContext = new TrpgManageBase<ISPC>();
        }
    }
}
