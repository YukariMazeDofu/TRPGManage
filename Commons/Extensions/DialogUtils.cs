using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ItoKonnyaku.Commons.Extensions
{
    public static class DialogUtils
    {
        public static bool ShowDialogWindow(string message, string caption)
        {
            return MessageBoxResult.OK == MessageBox.Show(message, caption, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
        }
    }
}
