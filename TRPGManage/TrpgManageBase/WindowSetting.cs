using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace ItoKonnyaku.TrpgManage
{
    public class WindowSetting : BindableBase
    {
        #region プロパティ

        //ウインドウ位置
        // 0:Left 1:Top 2:Width 3:Height
        private ObservableCollection<int> _windowPosSize = new ObservableCollection<int>() { 50, 50, 1500, 800};
        public ObservableCollection<int> WindowPosSize
        {
            get { return _windowPosSize; }
            set { this.SetProperty(ref this._windowPosSize, value); }
        }

        private double _consoleWidth = 500;
        public double ConsoleWidth
        {
            get { return _consoleWidth; }
            set { this.SetProperty(ref this._consoleWidth, value); }
        }

        private double _imageSize = 110;
        public double ImageSize
        {
            get { return _imageSize; }
            set { this.SetProperty(ref this._imageSize, value); }
        }

        private CutInController _cutIn = new CutInController();
        public CutInController CutIn
        {
            get { return _cutIn; }
            set { this.SetProperty(ref this._cutIn, value); }
        }

        #endregion

    }

    public class CutInController : BindableBase
    {
        #region コンストラクタ

        public CutInController()
        {
            InitializeDelegateCommand();
        }

        #endregion

        #region プロパティ

        private bool _isCutIn = true;
        public bool IsCutIn
        {
            get { return _isCutIn; }
            set { this.SetProperty(ref this._isCutIn, value); }
        }

        private bool _cutInTrigger = false;
        public bool CutInTrigger
        {
            get { return _cutInTrigger ; }
            set { this.SetProperty(ref this._cutInTrigger , value); }
        }

        private string _cutInText="";
        public string CutInText
        {
            get { return _cutInText; }
            set { this.SetProperty(ref this._cutInText, value); }
        }

        #endregion

        #region コマンド

        public void InitializeDelegateCommand()
        {
            ShowCutInCommand = new DelegateCommand<string>(ShowCutIn);
        }

        public ICommand ShowCutInCommand { get; private set; }
        public void ShowCutIn()
        {
            this.ShowCutIn(this.CutInText);
        }

        public void ShowCutIn(string text)
        {
            this.CutInText = text;
            this.CutInTrigger = false;
            this.CutInTrigger = true;
        }

        #endregion
    }
        
    public class DoubleGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new GridLength((double)value);
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            GridLength gridLength = (GridLength)value;
            return gridLength.Value;
        }
    }
}
