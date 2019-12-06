using ItoKonnyaku.Commons.Extensions;
using ItoKonnyaku.Mvvm;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ItoKonnyaku.TrpgManage
{
    public class ArCosts : BindableList<ArCost>
    {
        #region コンストラクタ

        public ArCosts()
        {

        }

        #endregion

        #region プロパティ

        //全体
        private ArCostSets _costSets = ArCostSets.Fate;
        public ArCostSets CostSets
        {
            get { return _costSets; }
            set { this.SetProperty(ref this._costSets, value); }
        }

        private string _sendingText = "";
        public string SendingText
        {
            get { return _sendingText; }
            private set { this.SetProperty(ref this._sendingText, value); }
        }

        private ArCost _sendingCost = new ArCost();
        public ArCost SendingCost
        {
            get { return _sendingCost; }
            private set { this.SetProperty(ref this._sendingCost, value); }
        }

        //Fateコスト
        private ArFateType _fateType = ArFateType.MoreDice;
        public ArFateType FateType
        {
            get { return _fateType; }
            set { this.SetProperty(ref this._fateType, value); }
        }

        private int _fateCost = 1;
        public int FateCost
        {
            get { return _fateCost; }
            set { this.SetProperty(ref this._fateCost, value); }
        }

        //ダメージ、回復判定
        private ArDamageType _damageType = ArDamageType.Physical;
        public ArDamageType DamageType
        {
            get { return _damageType; }
            set { this.SetProperty(ref this._damageType, value); }
        }

        private ArResource _targetValue = ArResource.HP;
        public ArResource TargetValue
        {
            get { return _targetValue; }
            set { this.SetProperty(ref this._targetValue, value); }
        }

        private int _defPointPhysical = 0;
        public int DefPointPhysical
        {
            get { return _defPointPhysical; }
            set { this.SetProperty(ref this._defPointPhysical, value); }
        }

        private int _defPointMagical = 0;
        public int DefPointMagical
        {
            get { return _defPointMagical; }
            set { this.SetProperty(ref this._defPointMagical, value); }
        }

        public int DefPoint
        {
            //damagetypeの選択で返す値を変える
            get
            {
                switch (this.DamageType)
                {
                    case ArDamageType.Physical:
                        return this.DefPointPhysical;
                    case ArDamageType.Magical:
                        return this.DefPointMagical;
                    default:
                        return 0;
                }
            }
            private set { }
        }

        private int _damageBase = 0;
        public int DamageBase
        {
            get { return _damageBase; }
            set { this.SetProperty(ref this._damageBase, value); }
        }

        private int _damageCorrect1 = 0;
        public int DamageCorrect1
        {
            get { return _damageCorrect1; }
            set { this.SetProperty(ref this._damageCorrect1, value); }
        }

        private int _damageCorrect2 = 0;
        public int DamageCorrect2
        {
            get { return _damageCorrect2; }
            set { this.SetProperty(ref this._damageCorrect2, value); }
        }

        private int _damageCorrected = 0;
        public int DamageCorrected
        {
            get { return _damageCorrected; }
            private set { this.SetProperty(ref this._damageCorrected, value); }
        }


        #endregion

        #region コマンド
        protected override void InitializeDelegateCommand()
        {
            base.InitializeDelegateCommand();
            this.AddSkillCostCommand = new DelegateCommand( ()=>
                {
                    ArCost additem;
                    if (this.SelectedItem != null)
                    {
                        additem = new ArCost(this.SelectedItem);
                    }
                    else
                    {
                        additem = new ArCost
                        {
                            CostGroup = "スキル使用"
                        };
                    }
                    this.AddItem(additem);
                }
                );
        }

        public ICommand AddSkillCostCommand { get; private set; }

        public void ReCalcSendCost()
        {
            switch (this.CostSets)
            {
                case ArCostSets.Fate:
                    {
                        var nFCv = new ObservableCollection<BindableValue<int>>();
                        for (var i = 0; i < 5; i++)
                        {
                            nFCv.Add(new BindableValue<int>(0));
                        }
                        nFCv[(int)ArResource.Fate].Value = -this.FateCost;

                        this.SendingCost.ClearAllEvents();
                        this.SendingCost = new ArCost()
                        {
                            CostGroup = "Fate使用",
                            CostName = FateType.Text(),
                            Items = nFCv
                        };
                        break;
                    }
                case ArCostSets.Damage:
                    {
                        var nDCv = new ObservableCollection<BindableValue<int>>();
                        for (var i = 0; i < 5; i++)
                        {
                            nDCv.Add(new BindableValue<int>(0));
                        }
                        nDCv[(int)TargetValue].Value = this.DamageCorrected;

                        var nCostName = this.DamageBase.ToString("0;-0")
                                      + this.DefPoint.ToString("-0;+0;#")
                                      + this.DamageCorrect1.ToString("+0;-0;#")
                                      + this.DamageCorrect2.ToString("+0;-0;#");
                        //+ "="
                        //+ ((this.DamageType == ArDamageType.Heal) ? this.DamageCorrected.ToString("0;-0;0") : this.DamageCorrected.ToString("-0;0;0"));

                        this.SendingCost.ClearAllEvents();
                        this.SendingCost = new ArCost()
                        {
                            CostGroup = (DamageType == ArDamageType.Heal ? TargetValue.ResourceName() : "") + DamageType.Text(),
                            CostName = nCostName,
                            Items = nDCv
                        };
                        break;
                    }
                case ArCostSets.SkillCost:
                    {
                        this.SendingCost = new ArCost(this.SelectedItem);
                        break;
                    }
            }
            this.SendingText = this.SendingCost.ToString();
            this.CostChanged?.Invoke(this, null);
        }

        //ダメージ計算
        private void CalcDamageCorrected()
        {
            var result = this.DamageBase - this.DefPoint + this.DamageCorrect1 + this.DamageCorrect2;
            result = result.InRange(0, result);

            this.DamageCorrected = (this.DamageType == ArDamageType.Heal) ? result : -result;


            return;
        }

        public void ResetAllCorrection()
        {
            this.DamageCorrect1 = 0;
            this.DamageCorrect2 = 0;
            this.DamageBase = 0;
            this.FateCost = 1;
        }
        
        #endregion

        #region　イベント処理

        public event PropertyChangedEventHandler CostChanged;

        public override void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemChanged(sender, e);
            //スキルコストに変更があった場合
            this.CostSets = ArCostSets.SkillCost;
        }

        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

            switch (e.PropertyName)
            {
                case nameof(this.CostSets):
                    {
                        ReCalcSendCost();
                        break;
                    }
                //Fate消費
                case nameof(this.FateType):
                case nameof(this.FateCost):
                    {
                        //リロール時は強制1消費
                        if (this.FateType == ArFateType.ReRoll) this.FateCost = 1;

                        this.CostSets = ArCostSets.Fate;
                        ReCalcSendCost();

                        break;
                    }
                //ダメージ回復判定
                case nameof(this.DamageType):
                case nameof(this.TargetValue):
                case nameof(this.DefPointPhysical):
                case nameof(this.DefPointMagical):
                case nameof(this.DamageBase):
                case nameof(this.DamageCorrect1):
                case nameof(this.DamageCorrect2):
                    {
                        if (this.TargetValue == ArResource.MP && (this.DamageType == ArDamageType.Physical || this.DamageType == ArDamageType.Magical))
                        {
                            this.TargetValue = ArResource.HP;
                        }

                        this.RaisePropertyChanged(nameof(DefPoint));
                        this.CalcDamageCorrected();
                        this.CostSets = ArCostSets.Damage;
                        ReCalcSendCost();
                        break;
                    }
                //スキルコスト
                case nameof(this.SelectedIndex):
                case nameof(this.Items):
                    {
                        this.CostSets = ArCostSets.SkillCost;
                        ReCalcSendCost();

                        break;
                    }
            }

        }

        public override void ClearAllEvents()
        {
            base.ClearAllEvents();

            if (this.Items.Count != 0)
            {
                foreach (var item in this.Items)
                {
                    item.ItemChanged -= this.OnItemChanged;
                }
            }
        }

        protected override void SetAllEvents()
        {
            base.SetAllEvents();

            if (this.Items.Count != 0)
            {
                foreach (var item in this.Items)
                {
                    item.ItemChanged += this.OnItemChanged;
                }
            }
        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            if (this.Items.Count != 0)
            {
                foreach (var item in this.Items)
                {
                    item.RefreshAllEvents();
                }
            }
        }

        #endregion

    }

    public enum ArCostSets
    {
        Fate, Damage, SkillCost
    }

    public enum ArFateType
    {
        MoreDice, ReRoll
    }

    public enum ArDamageType
    {
        Physical, Magical, Penetrate, Heal
    }

    public static class ArTypeExt
    {
        public static string Text(this ArFateType value)
        {
            string[] txts = { "ダイス増加", "ダイス振りなおし" };
            return txts[(int)value];
        }

        public static string Text(this ArDamageType value)
        {
            string[] txts = { "物理ダメージ", "魔法ダメージ", "貫通ダメージ", "回復" };
            return txts[(int)value];
        }
    }

    public class ArCost : BindableValueList<int>
    {
        #region コンストラクタ

        public ArCost()
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ArCost(ArCost copy)
        {
            this.CostGroup = copy.CostGroup;
            this.CostName = copy.CostName;
            this.CostUsage = copy.CostUsage;
            this.Items.Clear();
            foreach(var i in copy.Items)
            {
                this.AddItem(new BindableValue<int>(i.Value));
            }
        }

        #endregion


        #region プロパティ

        private string _costName = "";
        public string CostName
        {
            get { return _costName; }
            set { this.SetProperty(ref this._costName, value); }
        }

        private string _costGroup = "";
        public string CostGroup
        {
            get { return _costGroup; }
            set { this.SetProperty(ref this._costGroup, value); }
        }

        private string _usage = "";
        public string CostUsage
        {
            get { return _usage; }
            set { this.SetProperty(ref this._usage, value); }
        }

        public string ChangeValueText
        {
            get
            {
                return string.Join(",", this.Items.Select((i, cnt) => ((i.Value == 0) ? "" : (((ArResource)cnt).ResourceName() + ":" + i.Value.ToString("+0;-0")))).Where(i => !string.IsNullOrEmpty(i)).ToList());
            }
            private set { }
        }

        #endregion

        public static string Format(ArCost target, string format)
        {
            return string.Format(format, target.CostGroup
                                       , target.CostName
                                       , target.ChangeValueText
                                       , target.Items.Select(n => n.ToString())
                                       );
        }

        public override string ToString()
        {
            var result = ArCost.Format(this, "[ {0} ] {1} ({2})").Replace("()", "");
            return result;
        }

    }




}
