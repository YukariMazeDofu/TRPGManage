using ItoKonnyaku.Commons.Extensions;
using ItoKonnyaku.Mvvm;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace ItoKonnyaku.TrpgManage
{

    public class CoCCharacterValues : BindableValueList<int>
    {
        #region コンストラクタ

        public CoCCharacterValues()
        {
            this.SelectedIndex = 0;
        }

        #endregion

        #region プロパティ

        private string _sendingMultiText = "";
        public string SendingMultiText
        {
            get { return _sendingMultiText; }
            private set { this.SetProperty(ref this._sendingMultiText, value); }
        }

        private string _sendingResistText = "";
        public string SendingResistText
        {
            get { return _sendingResistText; }
            private set { this.SetProperty(ref this._sendingResistText, value); }
        }

        public RollDice SendRollMultipleDice { get; private set; } = new RollDice(TrpgSystem.Cthulhu);

        private CoCCharacterValueName _selectedValue = CoCCharacterValueName.STR;
        public CoCCharacterValueName SelectedValue
        {
            get { return _selectedValue; }
            set
            {
                this.SetProperty(ref this._selectedValue, value);
                this.SelectedIndex = (int)value;
            }
        }

        private CoCCharacterValueName _enemyValue = CoCCharacterValueName.STR;
        public CoCCharacterValueName EnemyValue
        {
            get { return _enemyValue; }
            set { this.SetProperty(ref this._enemyValue, value); }
        }

        private int _enemyTargetValue = 10;
        public int EnemyTargetValue
        {
            get { return _enemyTargetValue; }
            set { this.SetProperty(ref this._enemyTargetValue, value); }
        }

        private string _resistProbability;
        public string ResistProbability
        {
            get { return _resistProbability; }
            set { this.SetProperty(ref this._resistProbability, value); }
        }

        private bool _isVissibleEnemyValue = true;
        public bool IsVissibleEnemyValue
        {
            get { return _isVissibleEnemyValue; }
            set { this.SetProperty(ref this._isVissibleEnemyValue, value); }
        }

        private CoCCharacterMultiple _multiple = CoCCharacterMultiple.FiveTimes;
        public CoCCharacterMultiple Multiple
        {
            get { return _multiple; }
            set { this.SetProperty(ref this._multiple, value); }
        }


        #endregion

        #region コマンド
        public void SetSendingText()
        {
            this.SendRollMultipleDice = TrpgSystem.Cthulhu.MakeNormalRollDice();

            //倍率処理
            this.SendRollMultipleDice.RollGroup = "能力倍数";
            this.SendRollMultipleDice.RollName  = this.SelectedValue.Name() + this.Multiple.Name();
            this.SendRollMultipleDice.TargetValue = this.SelectedItem.Value * this.Multiple.Value();
            this.SendingMultiText = this.SendRollMultipleDice.Format(true);

            //対抗処理
            var pred = ((this.SelectedItem.Value - this.EnemyTargetValue) * 5 + 50).InRange(0,100);
            this.ResistProbability = $"{pred}%";
            var enemyValue = this.IsVissibleEnemyValue ? $"vs{this.EnemyValue.Name()}":"";
            this.SendingResistText = $"RES({this.SelectedItem.Value}-{this.EnemyTargetValue}) [ 能力対抗 ] {this.SelectedValue.Name()}{enemyValue}";
                
        }

        public override string ToString()
        {
            return $"要素数：{this.Items?.Count()} / 選択中：{this.SelectedValue.Name()}({this.SelectedItem?.ToString()})";
        }
        #endregion

        #region　イベント処理

        protected override void InitializeDelegateCommand()
        {
            base.InitializeDelegateCommand();
        }


        public virtual void ResetAllCorrection()
        {
            this.IsVissibleEnemyValue = true;
            this.EnemyTargetValue = 10;
        }


        public override void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemChanged(sender, e);
            var ignores = new string[]
            {
                nameof(this.SendingResistText),
                nameof(this.SendingMultiText)
            };

            if (ignores.Contains(e.PropertyName)) return; 
            this.SetSendingText();
        }

        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
            var ignores = new string[]
            {
                nameof(this.SendingResistText),
                nameof(this.SendingMultiText)
            };

            if (ignores.Contains(e.PropertyName)) return;
            this.SetSendingText();
        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            this.SetSendingText();
        }

        #endregion
    }

    public enum CoCCharacterValueName
    {//  0    1    2    3    4    5    6    7    8     9
        STR, CON, POW, DEX, APP, SIZ, INT, EDU, POT, OTHER
    }

    public static class CoCharacterValueNameExtention
    {
        public static string Name(this CoCCharacterValueName target)
        {
            string[] names = { "STR", "CON", "POW", "DEX", "APP", "SIZ", "INT", "EDU", "POT", "OTHER" };
            return names[(int)target];
        }
    }

    [ValueConversion(typeof(CoCCharacterValueName), typeof(string))]
    public class CoCCharacterValueNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CoCCharacterValueName target)
            {
                return target.Name();
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string target)
            {
                return (CoCCharacterValueName)Enum.Parse(typeof(CoCCharacterValueName), target, true);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
       
    public enum CoCCharacterMultiple
    {
        One, Twice, Thrice, FourTimes, FiveTimes, SixTimes, SevenTimes, EightTimes, NineTimes
    }

    public static class CoCCharacterMultipleExtention
    {
        public static int Value(this CoCCharacterMultiple target)
        {
            return ((int)target + 1);
        }

        public static string Name(this CoCCharacterMultiple target)
        {
            string[] names = { "×1", "×2", "×3", "×4", "×5", "×6", "×7", "×8", "×9"};
            return names[(int)target];
        }
    }

    [ValueConversion(typeof(CoCCharacterMultiple), typeof(string))]
    public class CoCCharacterMultipleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CoCCharacterMultiple target)
            {
                return target.Name();
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string target)
            {
                return (CoCCharacterMultiple)Enum.Parse(typeof(CoCCharacterMultiple), target, true);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }
}


