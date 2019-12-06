using ItoKonnyaku.Mvvm;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ItoKonnyaku.TrpgManage
{

    public class RollDices : BindableList<RollDice>
    {
        #region コンストラクタ

        public RollDices()
        {
        }

        #endregion
        
        #region プロパティ

        private bool _isVissibleTargetValue = true;
        public bool IsVissibleTargetValue
        {
            get { return _isVissibleTargetValue; }
            set { this.SetProperty(ref this._isVissibleTargetValue, value); }
        }

        private string _sendingText = "";
        public string SendingText
        {
            get { return _sendingText; }
            protected set { this.SetProperty(ref this._sendingText, value); }
        }

        #endregion

        #region コマンド

        protected override void InitializeDelegateCommand()
        {
            base.InitializeDelegateCommand();
            this.ResetAllCorrectionCommand = new DelegateCommand(this.ResetAllCorrection);
        }

        public virtual void SetSendingText()
        {
            throw new NotImplementedException();
        }

        public ICommand ResetAllCorrectionCommand { get; private set; }
        public virtual void ResetAllCorrection()
        {

        }

        public override string ToString()
        {
            return $"ダイスロール数:{this.Items?.Count()} / 選択中：{this.SelectedItem?.ToString()}";
        }

        #endregion

    }

    public class RollDice : BindableBase
    {
        #region コンストラクタ

        public RollDice() { }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public RollDice(RollDice copy)
        {
            this.Copy(copy);
        }

        public RollDice(TrpgSystem sytem)
        {
            this.UsingSystem = sytem;
        }

        #endregion

        #region プロパティ
        public TrpgSystem UsingSystem { get; set; } = TrpgSystem.DiceBot;

        private string _rollGroup = "";
        public string RollGroup
        {
            get { return _rollGroup; }
            set { this.SetProperty(ref this._rollGroup, value); }
        }

        private string _rollName = "";
        public string RollName
        {
            get { return _rollName; }
            set { this.SetProperty(ref this._rollName, value); }
        }

        private int _diceNumber = 0;
        public int DiceNumber
        {
            get { return _diceNumber; }
            set { this.SetProperty(ref this._diceNumber, value); }
        }

        private int _diceBase = 0;
        public int DiceBase
        {
            get { return _diceBase; }
            set { this.SetProperty(ref this._diceBase, value); }
        }

        private int _diceConst = 0;
        public int DiceConst
        {
            get { return _diceConst; }
            set { this.SetProperty(ref this._diceConst, value); }
        }

        private string _inequality = "";
        public string Inequality
        {
            get { return _inequality; }
            set { this.SetProperty(ref this._inequality, value); }
        }

        private int _targetValue = 0;
        public int TargetValue
        {
            get { return _targetValue; }
            set { this.SetProperty(ref this._targetValue, value); }
        }

        #endregion

        #region コマンド

        public bool Copy(RollDice copy)
        {
            this.UsingSystem = copy.UsingSystem;
            this.RollGroup = copy.RollGroup;
            this.RollName = copy.RollName;
            this.DiceNumber = copy.DiceNumber;
            this.DiceBase = copy.DiceBase;
            this.DiceConst = copy.DiceConst;
            this.Inequality = copy.Inequality;
            this.TargetValue = copy.TargetValue;
            return true;
        }

        public override string ToString()
        {
            return this.Format(true);
        }

        public virtual void Clear()
        {
            this.RollGroup = "";
            this.RollName = "";
            this.DiceNumber = 0;
            this.DiceBase = 0;
            this.DiceConst = 0;
            this.Inequality = "";
            this.TargetValue = 0;
        }

        #endregion

    }

    public static class RollDiceExtention
    {
        public static string Format(this RollDice target, string format)
        {
            return string.Format(format, target.DiceNumber
                                       , target.DiceBase
                                       , target.DiceConst
                                       , target.Inequality
                                       , target.TargetValue
                                       , target.RollGroup
                                       , target.RollName);
        }

        public static string Format(this RollDice target, bool isVissibleTargetValue)
        {
            return target.Format(target.UsingSystem.SendingTextFormat(isVissibleTargetValue));
        }
    }


}
