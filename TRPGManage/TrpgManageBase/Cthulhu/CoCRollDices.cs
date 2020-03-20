using ItoKonnyaku.Commons.Extensions;
using ItoKonnyaku.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace ItoKonnyaku.TrpgManage
{

    public class CoCRollDices : RollDices
    {
        #region コンストラクタ

        public CoCRollDices()
        {
        }


        #endregion

        #region プロパティ

        public RollDice SendRollDice { get; private set; } = new RollDice();

        private int _correctTargetValue = 0;
        public int CorrectTargetValue
        {
            get { return _correctTargetValue; }
            set { this.SetProperty(ref this._correctTargetValue, value); }
        }

        private TargetValueMultiple _multiple = TargetValueMultiple.One;
        public TargetValueMultiple Multiple
        {
            get { return _multiple; }
            set { this.SetProperty(ref this._multiple, value); }
        }

        private bool _isRoundUp = true;
        public bool IsRoundUp
        {
            get { return _isRoundUp; }
            set { this.SetProperty(ref this._isRoundUp, value); }
        }

        private bool _isGrowth = false;
        public bool IsGrowth
        {
            get { return _isGrowth; }
            set { this.SetProperty(ref this._isGrowth, value); }
        }

        #endregion

        #region コマンド

        public override void SetSendingText()
        {
            if (this.SelectedItem == null) return;
            this.SendRollDice = new RollDice(this.SelectedItem);

            //倍率処理
            this.SendRollDice.TargetValue = this.IsRoundUp ?
                (int)Math.Ceiling(this.SendRollDice.TargetValue * this.Multiple.Value()) :
                (int)Math.Floor(this.SendRollDice.TargetValue * this.Multiple.Value());
            var multiText = this.Multiple == TargetValueMultiple.One ? "" : this.Multiple.Name().Replace("1", "");

            //補正値処理
            this.SendRollDice.TargetValue += this.CorrectTargetValue;

            //成長の場合不等号逆転
            this.SendRollDice.Inequality = this.IsGrowth ? ">" : "<=";
            var growthText = this.IsGrowth ? " <成長判定>" : "";

            //1-99にまるめ
            this.SendRollDice.TargetValue = this.SendRollDice.TargetValue.InRange(1, 99);

            //送信文出力
            this.SendRollDice.RollName = $"{this.SendRollDice.RollName}{multiText}{this.CorrectTargetValue:+0;-0;#}{growthText}";

            this.SendingText = this.SendRollDice.Format(this.IsVissibleTargetValue);

            //成長時ccbでは動作しないため暫定対応
            if(this.IsGrowth && this.SendingText.Contains("ccb"))
            {
                this.SendingText = this.SendingText.Replace("ccb", "1D100");
            }
        }

        public override void ResetAllCorrection()
        {
            this.CorrectTargetValue = 0;
            this.Multiple = TargetValueMultiple.One;
        }

        #endregion

        #region　イベント処理

        public override void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemChanged(sender,e);
            if(e.PropertyName != nameof(this.SendingText)) this.SetSendingText();
        }

        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
            if(e.PropertyName != nameof(this.SendingText)) this.SetSendingText();
        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            this.SetSendingText();
        }
        #endregion
    }

    public enum TargetValueMultiple
    {
        Thrice, Twice, One, Half, OneThird
    }

    public static class TargetValueMultipleExtention
    {
        public static double Value(this TargetValueMultiple target)
        {
            double[] values = { 3d, 2d, 1d, 0.5d, 0.333333333333333d };
            return values[(int)target];
        }

        public static string Name(this TargetValueMultiple target)
        {
            string[] names = { "×3", "×2", "等倍", "1/2", "1/3" };
            return names[(int)target];
        }
    }

    [ValueConversion(typeof(TargetValueMultiple), typeof(string))]
    public class TargetValueMultipleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TargetValueMultiple target)
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
                return (TargetValueMultiple)Enum.Parse(typeof(TargetValueMultiple), target, true);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }

}
