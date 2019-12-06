using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItoKonnyaku.TrpgManage
{
    public class ISAvoidance : BindableBase
    {
        #region コンストラクタ

        public ISAvoidance() { }

        #endregion

        #region プロパティ

        private int _velocity = 1;
        public int Velocity
        {
            get { return _velocity; }
            set { SetProperty(ref _velocity, value); this.SetSendingText(); }
        }

        public int TargetValue
        {
            get => this.Velocity + 4;
            private set { }
        }

        private string _sendingText = "";
        public string SendingText
        {
            get { return _sendingText; }
            set { SetProperty(ref _sendingText, value); }
        }

        #endregion

        #region コマンド

        private void SetSendingText()
        {
            this.SendingText = $"2D6>={this.TargetValue} [ 戦闘 ]回避(速度{this.Velocity})";
        }

        #endregion

        #region イベント

        public void RefreshAllEvents()
        {
            this.SetSendingText();
        }

        #endregion


    }
}
