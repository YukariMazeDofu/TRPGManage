using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ItoKonnyaku.Mvvm;
using ItoKonnyaku.Commons.Extensions;
using Prism.Commands;
using Prism.Mvvm;
using System.Data;

namespace ItoKonnyaku.TrpgManage
{
    public class XXEffects : BindableList<XXEffect>
    {
        #region プロパティ

        private int Erosion = 0;

        #endregion

        #region コマンド

        protected override void InitializeDelegateCommand()
        {
            base.InitializeDelegateCommand();
            this.AddEffectItemCommand = new DelegateCommand(() =>
                {
                    var ae = new XXEffect() { Name = "新エフェクト" };
                    ae.SetErosion(this.Erosion);
                    ae.PropertyChanged += this.OnEffectChanged;
                    ae.RefreshAllEvents();
                    this.AddItem(ae);
                }
            );
            this.CopyEffectItemCommand = new DelegateCommand(() =>
                {
                    if (this.SelectedItem == null) return;
                    var ae = new XXEffect(this.SelectedItem);
                    ae.Name = $"{ae.Name} - コピー";
                    ae.SetErosion(this.Erosion);
                    ae.RefreshAllEvents();
                    this.AddItem(ae);

                }
            );
        }

        public ICommand AddEffectItemCommand { get; private set; }
        public ICommand CopyEffectItemCommand { get; private set; }

        #endregion

        #region イベント
        public void OnErosionChanged(object sender, PropertyChangedEventArgs e)
        {
            var erosion = (sender as XXDodontoFPCData);

            this.Erosion = erosion.Erosion;

            foreach (var ef in this.Items)
            {
                ef.SetErosion(this.Erosion);
            }
        }

        public event PropertyChangedEventHandler EffectChanged;

        public void OnEffectChanged(object sender, PropertyChangedEventArgs e)
        {
            var ignores = new string[] {
                nameof(XXEffect.CurrentCostText),
                nameof(XXEffect.CurrentEffectText),
                nameof(XXEffect.IsLimited),
            };
            if (ignores.Contains(e.PropertyName)) return;
            this.EffectChanged?.Invoke(sender, e);
        }

        public void OnEffectListChanged(object sender, EventArgs e)
        {
            //エフェクト増減時に発破、その後ComboのErosionChanged発破。
            //エフェクトを消したときにComboに消したEffectを残さないため。
            this.OnEffectChanged(sender, new PropertyChangedEventArgs(nameof(this.Items)));

            //こちらの方が意図的に正しい。
            //this.EffectChanged?.Invoke(sender, new PropertyChangedEventArgs(nameof(this.Items)));
        }

        public void OnClicked()
        {
            //XAMLのリストクリック時に発破。選択しているアイテムが同じものでも反映させるため。
            this.RaisePropertyChanged(nameof(this.SelectedItem));
        }

        public override void ClearAllEvents()
        {
            base.ClearAllEvents();
            this.Items.CollectionChanged -= this.OnEffectListChanged;
            foreach(var ef in this.Items)
            {
                ef.PropertyChanged -= this.OnEffectChanged;
            }
        }

        protected override void SetAllEvents()
        {
            //あからさまに間違っているのだ！
            base.ClearAllEvents();
            this.Items.CollectionChanged += this.OnEffectListChanged;
            foreach (var ef in this.Items)
            {
                ef.PropertyChanged += this.OnEffectChanged;
            }
        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();

            foreach (var ef in this.Items)
            {
                ef.RefreshAllEvents();
            }
        }

        #endregion
    }

    public class XXEffect : BindableBase, IXXECRollable, IXXECCostable
    {
        #region　コンストラクタ

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public XXEffect()
        {
            this.ID = Guid.NewGuid();

            var c = Enum.GetValues(typeof(XXEffectValueName)).Length;
            this.EffectValues = new BindableValue<string>[c];
            this.EffectInts   = new BindableValue<int>[c];
            for(var i=0; i<c; i++)
            {
                this.EffectValues[i] = new BindableValue<string>("0");
                this.EffectValues[i].PropertyChanged += this.OnItemChanged;
                this.EffectInts[i]   = new BindableValue<int>(0);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public XXEffect(XXEffect copy) : this()
        {
            this.Name          = copy.Name;
            this.EffectLevel   = copy.EffectLevel;
            this.Usage         = copy.Usage;
            this.CostErosion   = copy.CostErosion;
            this.CostHP        = copy.CostHP;
            this.LimitErosion  = copy.LimitErosion;
            this.LimitCritical = copy.LimitCritical;
            this.IsMajor       = copy.IsMajor;

            int i = 0;
            foreach (var v in this.EffectValues)
            {
                v.Value = copy.EffectValues[i++].Value;
            }
        }

        #endregion

        #region プロパティ

        private Guid _id;
        public Guid ID
        {
            get { return _id; }
            set { this.SetProperty(ref this._id, value); }
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { this.SetProperty(ref this._name, value); }
        }

        private int _effectLevel = 0;
        public int EffectLevel
        {
            get { return _effectLevel; }
            set {
                this.SetProperty(ref this._effectLevel, value);
                this.CalcEffectInts();
            }
        }

        private string _usage = "";
        public string Usage
        {
            get { return _usage; }
            set { this.SetProperty(ref this._usage, value); }
        }

        private int _costErosion = 0;
        public int CostErosion
        {
            get { return _costErosion; }
            set
            {
                this.SetProperty(ref this._costErosion, value);
                this.RaisePropertyChanged(nameof(this.CurrentCostText));
                this.CalcEffectInts();
            }
        }

        private int _costHP = 0;
        public int CostHP
        {
            get { return _costHP; }
            set
            {
                this.SetProperty(ref this._costHP, value);
                this.RaisePropertyChanged(nameof(this.CurrentCostText));
                this.CalcEffectInts();
            }
        }

        private int _limitErosion = 0;
        public int LimitErosion
        {
            get { return _limitErosion; }
            set {
                this.SetProperty(ref this._limitErosion, value);
                this.RaisePropertyChanged(nameof(this.IsLimited));
            }
        }

        private int _limitCritical = 10;
        public int LimitCritical
        {
            get { return _limitCritical; }
            set { this.SetProperty(ref this._limitCritical, value); }
        }

        private bool _isMajor;
        public bool IsMajor
        {
            get { return _isMajor; }
            set {
                this.SetProperty(ref this._isMajor, value);
                this.RaisePropertyChanged(nameof(this.CurrentEffectText));
            }
        }

        public bool IsLimited
        {
            get { return this.LimitErosion > this.Erosion; }
            private set { }
        }

        private BindableValue<string>[] _effectValues;
        public BindableValue<string>[] EffectValues
        {
            get { return _effectValues; }
            set { this.SetProperty(ref this._effectValues, value); }
        }

        private BindableValue<int>[] _effectInts;
        public BindableValue<int>[] EffectInts
        {
            get { return _effectInts; }
            private set { this.SetProperty(ref this._effectInts, value); }
        }

        public string CurrentCostText
        {
            get { return $"[Cost]侵:{this.CostErosion,2}/HP:{this.CostHP,2}"; }
            private set { }
        }

        public string CurrentEffectText
        {
            get
            {
                var isMajor = this.IsMajor ? "■" : "□";
                return $"[行為]{this.EffectInts[0].Value,2}DX{this.EffectInts[1].Value,2}{this.EffectInts[2].Value,3:+0;-0} / [D]{this.EffectInts[3].Value,3:+0;-0;+0} {isMajor}";
            }
            private set { }
        }

        private int Erosion         = 0;
        private int ErosionEffectLv = 0;

        #endregion

        #region　コマンド

        public void SetErosion(int erosion)
        {
            this.Erosion = erosion;
            this.ErosionEffectLv = XXDodontoFPCData.CalcErosionEffectLv(erosion);
            this.RaisePropertyChanged(nameof(this.IsLimited));

            //[NeedCheck]この関数が入っていた理由
            //おそらく、Erosionが変わったときにComboも再計算したかったんだと思う。
            //実際にはXXDodontoFPCData.TodoErosionChangedからComboのTodoErosionChangedも発火するため、不要。
            //this.RaisePropertyChanged("CurrentRoll");

            this.CalcEffectInts();
        }

        public void CalcEffectInts()
        {
            var c = Enum.GetValues(typeof(XXEffectValueName)).Length;
            for (var i = 0; i < c; i++)
            {
                int result;
                try
                {
                    result = this.EffectValues[i].Value
                        .Replace("LV", (this.EffectLevel+this.ErosionEffectLv).ToString())
                        .Replace("HP", this.CostHP.ToString())
                        .Replace("ER", this.CostErosion.ToString())
                        .Calc<int>();
                }
                catch(EvaluateException)
                {
                    result = 0;
                }

                this.EffectInts[i].Value = result;
            }
            this.RaisePropertyChanged(nameof(this.CurrentEffectText));
        }

        public override string ToString()
        {
            return this.Format("名前：{0}, コスト：{2}");
        }

        #endregion

        #region　イベント処理

        public void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            this.CalcEffectInts();
            this.RaisePropertyChanged(nameof(this.EffectValues));
        }

        protected void ClearAllEvents()
        {
            foreach(var rd in this.EffectValues)
            {
                rd.PropertyChanged -= this.OnItemChanged;
            }
        }

        protected void SetAllEvents()
        {
            foreach (var rd in this.EffectValues)
            {
                rd.PropertyChanged += this.OnItemChanged;
            }
        }

        public void RefreshAllEvents()
        {
            this.ClearAllEvents();
            this.SetAllEvents();

            this.CalcEffectInts();
        }

        #endregion
    }

    public static class XXEffectExtention
    {
        public static string Format(this XXEffect target, string format)
        {
            return string.Format(format, target.Name
                                       , target.Usage
                                       , target.CostErosion
                                       , target.CostHP
                                       );
        }
    }

    public enum XXEffectValueName
    {
        DiceNumber, DiceCritical, DiceConst, DamageConst
    }
}
