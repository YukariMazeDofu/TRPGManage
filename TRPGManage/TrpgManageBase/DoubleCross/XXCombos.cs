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
using System.Collections.ObjectModel;

namespace ItoKonnyaku.TrpgManage
{
    public class XXCombos : BindableList<XXCombo>
    {
        #region コンストラクタ

        public XXCombos()
        {

        }

        public XXCombos(XXEffects refEffects) : this()
        {
            this.SetRefEffects(refEffects);
        }

        #endregion

        #region プロパティ
        private XXEffects RefEffects;

        #endregion

        #region コマンド

        public void SetRefEffects(XXEffects refEffects)
        {
            this.RefEffects = refEffects;
            foreach (var cb in this.Items)
            {
                cb.SetRefEffects(this.RefEffects);
            }
        }

        protected override void InitializeDelegateCommand()
        {
            base.InitializeDelegateCommand();
            this.AddComboItemCommand = new DelegateCommand(() =>
                {
                    var ac = new XXCombo(this.RefEffects)
                    {
                        Name = $"新コンボ{this.Items.Count+1}",
                        Usage = "コンボ使用法を記入"
                    };
                    ac.PropertyChanged += this.OnComboChanged;
                    ac.RefreshAllEvents();
                    this.AddItem(ac);
                 }
            );

            this.CopyComboItemCommand = new DelegateCommand(() =>
                {
                    if (this.SelectedItem == null) return;
                    var ac = new XXCombo(this.SelectedItem, this.RefEffects);
                    ac.Name = $"{ac.Name} - コピー";
                    ac.PropertyChanged += this.OnComboChanged;
                    ac.RefreshAllEvents();
                    this.AddItem(ac);
                }

            );
            
        }

        public ICommand AddComboItemCommand { get; private set; }
        public ICommand CopyComboItemCommand { get; private set; }
        #endregion

        #region イベント

        public event PropertyChangedEventHandler ComboChanged;

        public void OnErosionChanged(object sender, PropertyChangedEventArgs e)
        {
            foreach(var cb in this.Items)
            {
                cb.RecalcComboValues();
            }
        }

        public void OnEffectChanged(object sender, PropertyChangedEventArgs e)
        {
            //if (e.PropertyName == "IsLimited") return;
            this.OnErosionChanged(sender, e);
        }

        public void OnComboChanged(object sender, PropertyChangedEventArgs e)
        {
            this.ComboChanged?.Invoke(sender, e);
        }

        public void OnClicked()
        {
            this.RaisePropertyChanged(nameof(this.SelectedItem));
        }

        protected override void SetAllEvents()
        {
            base.SetAllEvents();
            foreach (var cb in this.Items)
            {
                cb.PropertyChanged += this.OnComboChanged;
            }
        }

        public override void ClearAllEvents()
        {
            base.ClearAllEvents();
            foreach (var cb in this.Items)
            {
                cb.PropertyChanged -= this.OnComboChanged;
            }
        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            foreach (var cb in this.Items)
            {
                cb.RefreshAllEvents();
            }
        }
        
        #endregion
    }

    public class XXCombo : BindableBase, IXXECRollable, IXXECCostable
    {
        #region コンストラクタ

        public XXCombo()
        {
            this.InitializeDelegateCommand();
            var c = Enum.GetValues(typeof(XXEffectValueName)).Length;
            this._effectInts = new BindableValue<int>[c];
            for (var i = 0; i < c; i++)
            {
                this._effectInts[i] = new BindableValue<int>(0);
            }
        }

        public XXCombo(XXEffects refEffects) : this()
        {
            this.SetRefEffects(refEffects);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public XXCombo(XXCombo copy, XXEffects refEffects) : this(refEffects)
        {
            this._name = copy._name;
            this._usage = copy._usage;
            this.EffectIDs = new BindableValueList<Guid>(copy.EffectIDs.Items);
            this.RefreshAllEvents();
        }
        #endregion

        #region プロパティ

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { this.SetProperty(ref this._name, value); }
        }

        private string _usage = "";
        public string Usage
        {
            get { return _usage; }
            set { this.SetProperty(ref this._usage, value); }
        }

        private XXEffects RefEffects;

        private BindableValueList<Guid> _effectIDs = new BindableValueList<Guid>();
        public BindableValueList<Guid> EffectIDs
        {
            get { return _effectIDs; }
            set { this.SetProperty(ref this._effectIDs, value); }
        }

        public List<XXEffect> Effects
        {
            get
            {
                if (this.RefEffects == null) return null;

                List<XXEffect> result = new List<XXEffect>();

                foreach (var ef in this.EffectIDs.Items)
                {
                    result.Add(RefEffects.Items.FirstOrDefault(j => (j.ID == ef.Value)));
                }

                return result;
            }
            private set { }
        }

        private int _costErosion = 0;
        public int CostErosion
        {
            get { return _costErosion; }
            private set { this.SetProperty(ref this._costErosion, value); }
        }

        private int _costHP = 0;
        public int CostHP
        {
            get { return _costHP; }
            private set { this.SetProperty(ref this._costHP, value); }
        }

        private int _limitErosion = 0;
        public int LimitErosion
        {
            get { return _limitErosion; }
            private set { this.SetProperty(ref this._limitErosion, value); }
        }

        private int _limitCritical = 0;
        public int LimitCritical
        {
            get { return _limitCritical; }
            private set { this.SetProperty(ref this._limitCritical, value.InRange(2,11)); }
        }

        private bool _isLimited = false;
        public bool IsLimited
        {
            get { return _isLimited; }
            private set { this.SetProperty(ref this._isLimited, value); }
        }

        private bool _isMajor;
        public bool IsMajor
        {
            get { return _isMajor; }
            set {
                this.SetProperty(ref this._isMajor, value);
                this.RecalcComboValues();
            }
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

        public string EffectNames
        {
            get { return string.Join("/", this.Effects.Select(i => $"{i.Name}{i.EffectLevel}").ToArray()); }
            private set { }
        }

        public string EffectTimingNames
        {
            get { return string.Join("/", this.Effects.Where(i => i.IsMajor == this.IsMajor).Select(i => $"{i.Name}{i.EffectLevel}").ToArray()); }
            private set { }
        }

        #endregion

        #region コマンド

        protected void InitializeDelegateCommand()
        {
            this.AddEffectItemCommand = new DelegateCommand<XXEffect>((ae) =>
           {
               if (ae != null)
               {
                   var ids = this.EffectIDs.Items.Select(j => j.Value);
                   if (ids.Contains(ae.ID)) return; //重複登録を許可しない
                   this.EffectIDs.AddItem(new BindableValue<Guid>(ae.ID));
               }
           });
        }

        public ICommand AddEffectItemCommand { get; private set; }

        public void SetRefEffects(XXEffects refEffects)
        {
            this.RefEffects = refEffects;
        }

        public void RecalcComboValues()
        {
            this.RemoveErrorID();
            foreach (var i in this._effectInts) { i.Value = 0; };

            if (this.Effects == null) return;
            if (this.Effects.Count == 0)
            {
                this.CostErosion = 0;
                this.CostHP = 0;
                this.LimitErosion = 0;
                this.LimitCritical = 10;
                this.IsLimited = false;
            }
            else
            {
                this.CostErosion = this.Effects.Where(i => i.IsMajor == this.IsMajor).Sum(i => i.CostErosion);
                this.CostHP = this.Effects.Where(i => i.IsMajor == this.IsMajor).Sum(i => i.CostHP);
                this.LimitErosion = this.Effects.Max(i => i.LimitErosion);
                this.LimitCritical = this.Effects.Min(i => i.LimitCritical);
                this.IsLimited = this.Effects.Any(i => i.IsLimited);

                //EffectInts計算
                foreach (var i in this._effectInts) { i.Value = 0; };
                foreach (var ef in this.Effects)
                {
                    for (var i = 0; i < this._effectInts.Length; i++)
                    {
                        this._effectInts[i].Value += ef.EffectInts[i].Value;
                    }
                }
                this._effectInts[(int)XXEffectValueName.DiceCritical].Value =
                    this._effectInts[(int)XXEffectValueName.DiceCritical].Value.InRange(this.LimitCritical, 11);
            }
            this.RaisePropertyChanged(nameof(this.CurrentEffectText));
            this.RaisePropertyChanged(nameof(this.CurrentCostText));
        }

        protected void RemoveErrorID()
        {
            //コンボ登録されていたエフェクトが削除された際、表示上は消えるが
            //実体は残ったままなので、時々チェックして実体のないIDを消す。
            if (RefEffects == null) return;

            var ids = RefEffects.Items.Select(i => i.ID);
            var target = this.EffectIDs.Items.Where(i => !ids.Contains(i.Value)).ToArray();

            foreach (var ri in target)
            {
                this.EffectIDs.RemoveItem(this.EffectIDs.Items.IndexOf(ri));
            }
        }

        #endregion

        #region イベント

        public void OnEffectIDsChanged(object sender, EventArgs e)
        {
            this.RaisePropertyChanged(nameof(this.Effects));
            this.RaisePropertyChanged(nameof(this.EffectNames));
            this.RaisePropertyChanged(nameof(this.EffectTimingNames));
            this.RecalcComboValues();
        }

        protected void ClearAllEvents()
        {
            this.EffectIDs.Items.CollectionChanged -= this.OnEffectIDsChanged;
        }

        protected void SetAllEvents()
        {
            this.EffectIDs.Items.CollectionChanged += this.OnEffectIDsChanged;
        }

        public void RefreshAllEvents()
        {
            this.ClearAllEvents();
            this.SetAllEvents();
            this.EffectIDs.RefreshAllEvents();
            this.RecalcComboValues();
        }

        #endregion
    }


}
