using ItoKonnyaku.Commons.Extensions;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ItoKonnyaku.Commons.Windows
{
    /// <summary>
    /// SeacretResultWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SeacretResultWindow : Window
    {
        public SeacretResultWindow()
        {
            InitializeComponent();
            this.DataContext = SeacretResults;
        }

        public ICommand SendResultStringCommand { get; set; }

        public ObservableCollection<string> SeacretResults { get; set; } = new ObservableCollection<string>();

        public bool CloseConfirm { get; set; } = false;

        private bool ShowConfirm(string msg, string caption)
        {
            return MessageBoxResult.OK == MessageBox.Show(msg, caption, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
        }

        private void UCloseWindowBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void UDeleteResultBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.SeacretResults.CheckIndex(uResultList.SelectedIndex))
            {
                if (this.ShowConfirm("<削除確認>\n\n" + uResultList.SelectedItem + "\n\n削除してもよろしいですか？", "削除確認"))
                {
                    this.SeacretResults.TryRemoveAt(uResultList.SelectedIndex);
                    this.SelectLatest();
                }

            }
        }

        private void USendResultBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.SeacretResults.CheckIndex(uResultList.SelectedIndex))
            {
                this.SendResultStringCommand?.Execute(uResultList.SelectedItem.ToString());
                this.SeacretResults.TryRemoveAt(uResultList.SelectedIndex);
                this.SelectLatest();
            }
            return;
        }

        public void SelectLatest()
        {
            this.uResultList.SelectedIndex = this.SeacretResults.Count - 1;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!CloseConfirm)
            {
                e.Cancel = true;
                this.Hide();
            }
        }
    }
}
