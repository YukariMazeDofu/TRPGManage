using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ItoKonnyaku.Commons.Extensions;
using ItoKonnyaku.Mvvm;

namespace ItoKonnyaku.TrpgManage
{
    public class ISRollDices : RollDices
    {
        #region コンストラクタ

        public ISRollDices() { }

        #endregion

        #region プロパティ

        //表示用
        public IEnumerable<ISRollDice> Items1 { get => this.Items.Select(i => i as ISRollDice).Where(i => i.X == 1); private set { } }
        public IEnumerable<ISRollDice> Items2 { get => this.Items.Select(i => i as ISRollDice).Where(i => i.X == 2); private set { } }
        public IEnumerable<ISRollDice> Items3 { get => this.Items.Select(i => i as ISRollDice).Where(i => i.X == 3); private set { } }
        public IEnumerable<ISRollDice> Items4 { get => this.Items.Select(i => i as ISRollDice).Where(i => i.X == 4); private set { } }
        public IEnumerable<ISRollDice> Items5 { get => this.Items.Select(i => i as ISRollDice).Where(i => i.X == 5); private set { } }
        public IEnumerable<ISRollDice> Items6 { get => this.Items.Select(i => i as ISRollDice).Where(i => i.X == 6); private set { } }

        private ISSpecialty _specialty = ISSpecialty.Violence;
        public ISSpecialty Specialty
        {
            get { return _specialty; }
            set { SetProperty(ref _specialty, value); }
        }

        private int _correctValue = 0;
        public int CorrectValue
        {
            get { return _correctValue; }
            set { SetProperty(ref _correctValue, value); }
        }

        public int HasFearCount { get => this.Items.Select(i => i as ISRollDice).Count(i => i.HasFear); private set { } }
        public int HasSkillCount { get => this.Items.Select(i => i as ISRollDice).Count(i => i.HasSkill); private set { } }

        public ISRollDice SelectRoll(int x, int y) => 
            this.Items.Select(i => i as ISRollDice).FirstOrDefault(i => i.X == x && i.Y == y);



        private BindableValueList<int> _selectedIndexes = new BindableValueList<int>(
                                                                new BindableValue<int>(0),
                                                                new BindableValue<int>(-1),
                                                                new BindableValue<int>(-1),
                                                                new BindableValue<int>(-1),
                                                                new BindableValue<int>(-1),
                                                                new BindableValue<int>(-1)  );
        public BindableValueList<int> SelectedIndexes
        {
            get { return _selectedIndexes; }
            set { SetProperty(ref _selectedIndexes, value); }
        }

        private bool IsChangingIndex = false;

        #endregion

        #region コマンド

        public override void SetSendingText()
        {

            if (!(this.SelectedItem is ISRollDice target)) return;

            var addK = (this.CorrectValue != 0);
            var TargetValue = (addK ? "(" : "") + $"{target.TargetValue}{this.CorrectValue:+0);-0);#}";
            var CorrectText = $"{this.CorrectValue:(+0補正);(-0補正);#}";
            var FearText = (target.HasFear) ? "(恐怖心)" : "";

            this.SendingText = $"2D6>={TargetValue} [ {target.RollGroup} ]{target.RollName}{CorrectText}{FearText}";
        }

        private void SetTargetValue(int x, int y, int setValue)
        {
            //対象決定
            var target = this.SelectRoll(x, y);

            //離脱条件
            if (!x.IsInRange(1,  6) || !y.IsInRange(2, 12)) return;
            if (setValue >= target.TargetValue) return;

            //値設定
            target.TargetValue = setValue;

            //再帰
            this.SetTargetValue(x, y - 1, setValue + 1);
            this.SetTargetValue(x, y + 1, setValue + 1);
            this.SetTargetValue(x-1, y, setValue + Dist(x-1,x));
            this.SetTargetValue(x+1, y, setValue + Dist(x,x+1));

            int Dist(int from, int to)
            {
                var sp = (int)this.Specialty;
                return (from == sp || to == sp)? 1 : 2; // 好奇心の場合は1 その他は2
            }

        }

        private void ClearTargetValue()
        {
            foreach (var i in this.Items) { i.TargetValue = 15; };
        }

        private void ResetTargetValues()
        {
            this.ClearTargetValue();
            var targets = this.Items.Select(i => i as ISRollDice).Where(i => i.HasSkill);
            foreach (var i in targets) { this.SetTargetValue(i.X, i.Y, 5); };
            this.SetSendingText();
        }

        public override void ResetAllCorrection()
        {
            base.ResetAllCorrection();
            this.CorrectValue = 0;
            this.SetSendingText();
        }

        #endregion

        #region イベント処理
        protected override void SetAllEvents()
        {
            base.SetAllEvents();
            this.SelectedIndexes.ItemChanged += this.OnIndexChanged;
        }

        public override void ClearAllEvents()
        {
            base.ClearAllEvents();
            this.SelectedIndexes.ItemChanged -= this.OnIndexChanged;
        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            this.SelectedIndexes.RefreshAllEvents();
        }

        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
            string[] CalcTarget = { nameof(this.Specialty) };
            if (CalcTarget.Contains(e.PropertyName)) this.ResetTargetValues();

            string[] CalcCorrect = { nameof(this.CorrectValue) };
            if (CalcCorrect.Contains(e.PropertyName)) this.SetSendingText();
        }

        public override void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemChanged(sender, e);

            string[] CalcTarget = { nameof(ISRollDice.HasFear), nameof(ISRollDice.HasSkill) };
            if (CalcTarget.Contains(e.PropertyName))
            {
                this.RaisePropertyChanged(nameof(this.HasFearCount));
                this.RaisePropertyChanged(nameof(this.HasSkillCount));
                this.ResetTargetValues();
            }
            

        }


        void OnIndexChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.IsChangingIndex) return;
            this.IsChangingIndex = true;
            var xi = int.Parse(e.PropertyName);
            var yi = this.SelectedIndexes[xi].Value;

            var target = this.SelectedIndexes.Items.Where((_, i) => i != xi);
            foreach (var t in target) { t.Value = -1; }

            this.SelectedIndex = xi * 11 + yi;

            IsChangingIndex = false;

            this.SetSendingText();
        }

        #endregion
    }

    public class ISRollDice : RollDice
    {
        #region コンストラクタ
        public ISRollDice() : base()
        {

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ISRollDice(ISRollDice copy)
        {
            this.Copy(copy);
        }



        #endregion

        #region プロパティ

        private bool _hasFear = false;
        public bool HasFear
        {
            get { return _hasFear; }
            set { SetProperty(ref _hasFear, value); }
        }

        private bool _hasSkill = false;
        public bool HasSkill
        {
            get { return _hasSkill; }
            set { SetProperty(ref _hasSkill, value); }
        }

        private int _x = 1;
        public int X
        {
            get { return _x; }
            set { SetProperty(ref _x, value); }
        }

        private int _y = 1;
        public int Y
        {
            get { return _y; }
            set { SetProperty(ref _y, value); }
        }

        #endregion

        #region コマンド

        public bool Copy(ISRollDice copy)
        {
            base.Copy(copy);
            this.HasFear = copy.HasFear;
            this.HasSkill = copy.HasSkill;
            this.X = copy.X;
            this.Y = copy.Y;
            return true;
        }

        #endregion
    }

    public enum ISSpecialty
    {
        Null       = 0,
        Violence   = 1,
        Emotion    = 2,
        Perception = 3,
        Technology = 4,
        Knowledge  = 5,
        Mysterious = 6
    }

    public static class ISSpecialityExtention
    {
        public static string Name(this ISSpecialty target)
        {
            string[] names = { "未設定", "暴力", "情動", "知覚", "技術", "知識", "怪異" };
            return names[(int)target];
        }
    }

    [ValueConversion(typeof(ISSpecialty), typeof(string))]
    public class ISSpecialtyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ISSpecialty target)
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
                return (ISSpecialty)Enum.Parse(typeof(ISSpecialty), target, true);
            }
            else
            {
                return DependencyProperty.UnsetValue;
            }
        }
    }

    [ValueConversion(typeof(ISSpecialty), typeof(Brush))]
    public class ISSpecialty2BrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is ISSpecialty val) || parameter == null || !(parameter is string par) || !int.TryParse(par, out var i)) return DependencyProperty.UnsetValue;
            var result = ((int)val == i)? new SolidColorBrush(Colors.Violet) : SystemColors.ActiveBorderBrush;
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    [ValueConversion(typeof(bool), typeof(Brush))]
    public class Bool2BrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(value is bool val) || parameter == null || !(parameter is string par)) return DependencyProperty.UnsetValue;

            //http://leetmikeal.hatenablog.com/entry/20141204/1417658909
            var cols = par.Split('-');
            var tcolor = (Color)ColorConverter.ConvertFromString(cols[0]);
            var tbrush = new SolidColorBrush(tcolor);

            SolidColorBrush fbrush;
            switch (cols[1])
            {
                case "Window": 
                    fbrush = SystemColors.WindowBrush;
                    break;

                case "Text":
                default:
                    fbrush = SystemColors.WindowTextBrush; 
                    break;
            }

            return val ? tbrush : fbrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


}
