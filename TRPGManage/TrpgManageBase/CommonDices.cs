using ItoKonnyaku.Commons.Extensions;
using ItoKonnyaku.Mvvm;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ItoKonnyaku.TrpgManage
{
    public class ChatPalettes : BindableValueList<string>
    {
        #region プロパティ

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
            this.AddItemCommand = new DelegateCommand<string>
                (i =>
                    {
                        if (!string.IsNullOrEmpty(i)) this.AddItem(new BindableValue<string>(i));
                    }
            );
            //this.ForcedSetSendText = new DelegateCommand(this.SetSendText);
        }

        public ICommand AddItemCommand { get; private set; }

        //public ICommand ForcedSetSendText { get; private set; }

        public void SetSendingText()
        {
            this.SendingText = this.SelectedItem?.Value ?? "";
        }

        public override string ToString()
        {
            return $"チャットパレット数:{this.Items?.Count()} / 選択中:{this.SelectedItem?.Value?.ToString()}";
        }
        #endregion

        #region　イベント処理

        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.SetSendingText();
        }

        public override void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            this.SetSendingText();
        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            this.SetSendingText();
        }

        #endregion

    }

    public class DiceCounters : BindableBase
    {
        #region　コンストラクタ

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DiceCounters()
        {
            this.InitializeDelegateCommand();
        }

        #endregion

        #region　プロパティ

        private Dictionary<string, int> Counters { get; set; } = new Dictionary<string, int>();

        public int DiceConst { get; private set; } = 0;

        private string _sendingText = string.Empty;
        public string SendingText
        {
            get { return _sendingText; }
            protected set { this.SetProperty(ref this._sendingText, value); }
        }

        #endregion

        #region　コマンド

        protected virtual void InitializeDelegateCommand()
        {
            this.AddDicesCommand = new DelegateCommand<string>(i => AddDices(i));
            this.ResetDicesCommand = new DelegateCommand(this.ResetAllCorrection);
        }

        public ICommand AddDicesCommand { get; private set; }
        public void AddDices(string dice)
        {
            var res = Regex.Match(dice, @"(?<Sign>[\+-])(?<DiceConst>[DC])(?<DiceBase>[0-9]+)");

            if (!res.Success) { return; }

            if(res.Groups["DiceConst"].Value == "D")
            {
                var key = $"D{res.Groups["DiceBase"].Value}";

                //Dictionary使いにくい…
                if (this.Counters.ContainsKey(key))
                {
                    this.Counters[key] += int.Parse($"{res.Groups["Sign"]}1");
                }
                else
                {
                    this.Counters.Add(key, int.Parse($"{res.Groups["Sign"]}1"));
                }
            }
            else
            {
                this.DiceConst += int.Parse($"{res.Groups["Sign"]}{res.Groups["DiceBase"].Value}");
            }

            this.SetSendingText();

        }

        public void SetSendingText()
        {
            //ダイスの順番に意味を持つ場合もあるので、ダイス登録順で表示する。
            this.SendingText = string.Join("+", this.Counters.Where(i => i.Value != 0).Select(i => $"{i.Value}{i.Key}"))
                                     .Replace("+-","-") + $"{this.DiceConst:+0;-0;#}";
            
        }

        public ICommand ResetDicesCommand { get; private set; }
        public void ResetAllCorrection()
        {
            this.Counters.Clear();
            this.DiceConst = 0;
            this.SendingText = "";
        }

        #endregion

    }
}
