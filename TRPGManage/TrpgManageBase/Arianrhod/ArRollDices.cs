using ItoKonnyaku.Commons.Extensions;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ItoKonnyaku.TrpgManage
{

    public class ArRollDices : RollDices
    {
        #region コンストラクタ

        public ArRollDices()
        {
        }

        #endregion

        #region プロパティ

        private RollDice _sendRollDice = new RollDice(TrpgSystem.Arianrhod);
        public RollDice SendRollDice
        {
            get { return _sendRollDice; }
            private set { this.SetProperty(ref this._sendRollDice, value); }
        }

        private RollDice _correctRollDice = new RollDice(TrpgSystem.Arianrhod);
        public RollDice CorrectRollDice
        {
            get { return _correctRollDice; }
            private set { this.SetProperty(ref this._correctRollDice, value); }
        }

        private bool _isVissibleProbability = true;
        public bool IsVissibleProbability
        {
            get { return _isVissibleProbability; }
            set { this.SetProperty(ref this._isVissibleProbability, value); }
        }

        private ArDiceProbability _probability = new ArDiceProbability();
        public ArDiceProbability Probability
        {
            get { return _probability; }
            set { this.SetProperty(ref this._probability, value); }
        }

        #endregion

        #region コマンド

        protected override void InitializeDelegateCommand()
        {
            base.InitializeDelegateCommand();

            this.AddSendRollItemCommand = new DelegateCommand( () =>
            {
                if (string.IsNullOrEmpty(this.SendRollDice.RollName)) return;
                this.AddItem(new RollDice(this.SendRollDice));
            });

        }

        public ICommand AddSendRollItemCommand { get; private set; }
        public override int AddItem(RollDice item)
        {
            item.TargetValue = 0;
            this.Items.Add(item);
            item.PropertyChanged += this.OnItemChanged;
            return this.Items.Count - 1;
        }

        public override void SetSendingText()
        {
            if (this.SelectedItem == null) return;
            this.SendRollDice = new RollDice(this.SelectedItem);

            //ダイスは最低1
            this.SendRollDice.DiceNumber += this.CorrectRollDice.DiceNumber;
            this.SendRollDice.DiceNumber  = this.SendRollDice.DiceNumber.InRange(1, this.SendRollDice.DiceNumber);

            this.SendRollDice.DiceConst  += this.CorrectRollDice.DiceConst;
            this.SendRollDice.TargetValue = this.CorrectRollDice.TargetValue;


            var addDice  = $"{this.CorrectRollDice.DiceNumber:+0D;-0D;#}";
            var addConst = $"{this.CorrectRollDice.DiceConst:+0;-0;#}" ;
            var addKakko = !string.IsNullOrEmpty(addDice) || !string.IsNullOrEmpty(addConst);
            var preKakko  = addKakko ? "(" : "";
            var postKakko = addKakko ? ")" : "";
            this.SendRollDice.RollName    = $"{this.SendRollDice.RollName}{preKakko}{addDice}{addConst}{postKakko}";

            //確率計算
            this.Probability.Calc(this.SendRollDice);

            this.SendingText = this.SendRollDice.Format(this.IsVissibleTargetValue);
        }

        public override void ResetAllCorrection()
        {
            this.CorrectRollDice.Clear();
        }


        #endregion

        #region　イベント処理

        public override void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            var ignores = new string[]
            {
                nameof(this.SendingText)
            };

            if (ignores.Contains(e.PropertyName)) return;

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


    public class ArDiceProbability : BindableBase
    {
        #region プロパティ

        private static readonly string DataPath =
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Data\\DiceProbability.csv";

        private static double[][] DiceProbabilityTable = null;

        private readonly double[] CriticalTable = { 0d, 0d, 0.02778d, 0.07407d, 0.13194d, 0.19624d, 0.26322d, 0.33020d, 0.39532d, 0.45734d, 0.51548d };

        public RollDice TargetRoll { get; private set; } = new RollDice();

        private double _rollProbability = 0d;
        public double  RollProbability
        {
            get { return _rollProbability; }
            private set { this.SetProperty(ref this._rollProbability, value); }
        }

        private double _rerollProbability = 0d;
        public double ReRollProbability
        {
            get { return _rerollProbability; }
            private set { this.SetProperty(ref this._rerollProbability, value); }
        }

        private double _addDiceRollProbability = 0d;
        public double AddDiceRollProbability
        {
            get { return _addDiceRollProbability; }
            private set { this.SetProperty(ref this._addDiceRollProbability, value); }
        }

        private double _criticalProbability = 0d;
        public double CriticalProbability
        {
            get { return _criticalProbability; }
            private set { this.SetProperty(ref this._criticalProbability, value); }
        }

        private double _average = 0d;
        public double Average
        {
            get { return _average; }
            private set { this.SetProperty(ref this._average, value); }
        }

        #endregion


        #region コマンド

        public static void ReadProbability()
        {
            try
            {
                var csv = System.IO.File.ReadAllText(DataPath);
                DiceProbabilityTable = csv.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Split(',')).Select(a => a.Select(double.Parse).ToArray()).ToArray();
            }
            catch(IOException)
            {
                return;
            }
        }

        //入力されたスキルから各種確率を計算
        public void Calc(RollDice target)
        {
            if (DiceProbabilityTable == null) ReadProbability();

            this.TargetRoll = target;

            var DiceNumber = target.DiceNumber.InRange(0, 10);

            var NormalFailRollProbabilitiy = this.CalcFailProbability(target.DiceNumber);

            this.RollProbability        = 1.0d - NormalFailRollProbabilitiy;
            this.AddDiceRollProbability = 1.0d - this.CalcFailProbability(target.DiceNumber + 1);
            this.ReRollProbability      = 1.0d - NormalFailRollProbabilitiy * NormalFailRollProbabilitiy;
            this.CriticalProbability    = CriticalTable[DiceNumber];
            this.Average                = 3.5d * target.DiceNumber + target.DiceConst;
        }


        //失敗する確率
        public double CalcFailProbability(int Dice)
        {

            var result = -8.99d;

            if (this.TargetRoll       == null
              || DiceProbabilityTable == null
              || Dice < 1
              || Dice > 10
              || this.TargetRoll.TargetValue > 60
              ) { return result; }

            var target = this.TargetRoll.TargetValue - this.TargetRoll.DiceConst - 1;
            target = target.InRange(0, 60);
            result = DiceProbabilityTable[Dice][target];

            return result;

        }

        #endregion

    }
}
