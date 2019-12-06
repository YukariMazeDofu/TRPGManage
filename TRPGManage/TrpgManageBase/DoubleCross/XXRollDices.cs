using ItoKonnyaku.Commons.Extensions;
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

    public class XXRollDices : RollDices
    {
        #region コンストラクタ

        public XXRollDices()
        {
            this.SelectedIndex = 0;
        }

        #endregion
               
        #region プロパティ

        private RollDice _sendRollDice = new RollDice(TrpgSystem.DoubleCross);
        public RollDice SendRollDice
        {
            get { return _sendRollDice; }
            private set { this.SetProperty(ref this._sendRollDice, value); }
        }

        private RollDice _correctRollDice = new RollDice(TrpgSystem.DoubleCross);
        public RollDice CorrectRollDice
        {
            get { return _correctRollDice; }
            private set { this.SetProperty(ref this._correctRollDice, value); }
        }

        private int ErosionDice = 0;

        #endregion

        #region コマンド

        protected override void InitializeDelegateCommand()
        {
            base.InitializeDelegateCommand();
        }

        public override void SetSendingText()
        {
            if (this.SelectedItem == null) return;
            this.SendRollDice = new RollDice(this.SelectedItem);

            //ダイスは最低1
            this.SendRollDice.DiceNumber += (this.CorrectRollDice.DiceNumber + this.ErosionDice);
            this.SendRollDice.DiceNumber  = this.SendRollDice.DiceNumber.InRange(0, this.SendRollDice.DiceNumber);

            this.SendRollDice.DiceBase   += this.CorrectRollDice.DiceBase;
            this.SendRollDice.DiceBase    = this.SendRollDice.DiceBase.InRange(2, 11);
            this.SendRollDice.DiceConst  += this.CorrectRollDice.DiceConst;
            this.SendRollDice.TargetValue = this.CorrectRollDice.TargetValue;

            var addDice = $"{(this.CorrectRollDice.DiceNumber+this.ErosionDice):+0D;-0D;#}";
            var addConst = $"{this.CorrectRollDice.DiceConst:+0;-0;#}";
            var addCritical = $"{(this.CorrectRollDice.DiceBase):/CL+0;/CL-0;#}";

            var addKakko = (!string.IsNullOrEmpty(addDice) || 
                            !string.IsNullOrEmpty(addCritical) ||
                            !string.IsNullOrEmpty(addConst));
            var preKakko = addKakko ? "(" : "";
            var postKakko = addKakko ? ")" : "";

            this.SendRollDice.RollName = $"{this.SendRollDice.RollName}{preKakko}{addDice}{addConst}{addCritical}{postKakko}";

            //確率計算

            this.SendingText = this.SendRollDice.Format(this.IsVissibleTargetValue);

        }

        public override void ResetAllCorrection()
        {
            base.ResetAllCorrection();
            this.CorrectRollDice.Clear();
        }


        #endregion

        #region　イベント処理

        public override void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.SendingText)) return;

            base.OnItemChanged(sender, e);
            this.SetSendingText();
        }

        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var ignores = new string[]
            {
                nameof(this.SendRollDice),
                nameof(this.SendingText)
            };
            if (ignores.Contains(e.PropertyName)) return;

            base.OnPropertyChanged(sender, e);
            this.SetSendingText();
        }

        public void OnErosionChanged(object sender, PropertyChangedEventArgs e)
        {
            var DodontoFPCData = (sender as XXDodontoFPCData);
            this.ErosionDice = DodontoFPCData.ErosionDice;
            this.SetSendingText();
        }

        public override void ClearAllEvents()
        {
            base.ClearAllEvents();
            this.CorrectRollDice.PropertyChanged -= this.OnItemChanged;
        }

        protected override void SetAllEvents()
        {
            base.SetAllEvents();
            this.CorrectRollDice.PropertyChanged += this.OnItemChanged;
        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            this.SetSendingText();
        }

        #endregion
    }

}
