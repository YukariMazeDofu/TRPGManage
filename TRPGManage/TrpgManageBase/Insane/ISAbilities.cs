using ItoKonnyaku.Mvvm;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ItoKonnyaku.TrpgManage
{
    public class ISAbilities : BindableList<ISAbility>
    {
        #region コンストラクタ

        public ISAbilities() : base() { }

        #endregion

        #region プロパティ

        private ISRollDices _rollDices;

        private int _correctValue = 0;
        public int CorrectValue
        {
            get { return _correctValue; }
            set { SetProperty(ref _correctValue, value); }
        }

        private string _sendingText = "";
        public string SendingText
        {
            get { return _sendingText; }
            set { SetProperty(ref _sendingText, value); }
        }

        #endregion

        #region コマンド

        protected override void InitializeDelegateCommand()
        {
            base.InitializeDelegateCommand();
            this.AddNewAbilityCommand = new DelegateCommand(this.AddNewAbility);
            this.SetRollDiceCommand = new DelegateCommand(() => this.SetRollDice(this.SelectedItem));
            this.RemoveRollDiceCommand = new DelegateCommand(() => this.RemoveRollDice(this.SelectedItem));
            this.ResetAllCorrectionCommand = new DelegateCommand(this.ResetAllCorrection);
        }

        public void SetRollDicesInstance(ISRollDices var) => this._rollDices = var;

        public ICommand AddNewAbilityCommand { get; private set; }
        private void AddNewAbility()
        {
            this.AddItem(new ISAbility());
        }

        public ICommand SetRollDiceCommand { get; private set; }
        private void SetRollDice(ISAbility item)
        {
            if (item == null) return;
            if (!(this._rollDices?.SelectedItem is ISRollDice target))
            {
                this.RemoveRollDice(item);
                return;
            }
            item.X = target.X;
            item.Y = target.Y;
            item.RollName = target.RollName;
            item.UseRoll = true;
        }

        public ICommand RemoveRollDiceCommand { get; private set; }
        private void RemoveRollDice(ISAbility item)
        {
            if (item == null) return;
            item.X = 0;
            item.Y = 0;
            item.RollName = "なし";
            item.UseRoll = false;
        }

        private void SetSendingText()
        {
            var item = this.SelectedItem;
            if (item == null) return;
            var target = this._rollDices?.SelectRoll(item.X, item.Y);

            string result;
            if (target != null)
            {
                if (item.UseRoll)
                {
                    var addK = (this.CorrectValue != 0);
                    var TargetValue = (addK ? "(" : "") + $"{target.TargetValue}{this.CorrectValue:+0);-0);#}";
                    var CorrectText = $"{this.CorrectValue:(+0補正);(-0補正);#}";
                    var FearText = (target.HasFear) ? "(恐怖心)" : "";

                    result = $"2D6>={TargetValue} [ アビリティ ]{item.Name}({target.RollName}){CorrectText}{FearText}";
                }
                else
                {
                    result = $"[ アビリティ ]{item.Name}({target.RollName})";
                }
            }
            else
            {
                result = $"[ アビリティ ]{item.Name}";
            }


            this.SendingText = result;
        }

        public ICommand ResetAllCorrectionCommand { get; private set; }
        public void ResetAllCorrection()
        {
            this.CorrectValue = 0;
        }

        #endregion

        #region イベント

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            this.SetSendingText();
        }

        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

            string[] SetSendingText = { nameof(this.SelectedIndex), nameof(this.CorrectValue) };
            if (SetSendingText.Contains(e.PropertyName)) this.SetSendingText();
        }

        public override void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemChanged(sender, e);
            this.SetSendingText();
        }

        #endregion
    }

    public class ISAbility : BindableBase
    {
        #region プロパティ

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private string _usage = "";
        public string Usage
        {
            get { return _usage; }
            set { SetProperty(ref _usage, value); }
        }

        private int _x = 0;
        public int X
        {
            get { return _x; }
            set { SetProperty(ref _x, value); }
        }

        private int _y = 0;
        public int Y
        {
            get { return _y; }
            set { SetProperty(ref _y, value); }
        }

        private string _rollName = "なし";
        public string RollName
        {
            get { return _rollName; }
            set { SetProperty(ref _rollName, value); }
        }

        private bool _useRoll = false;
        public bool UseRoll
        {
            get { return _useRoll; }
            set { SetProperty(ref _useRoll, value); }
        }

        #endregion

        #region コマンド

        public override string ToString()
        {
            return $"[ 技能 ]{this.Name}({this.X},{this.Y})";
        }

        #endregion
    }

}
