using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItoKonnyaku.TrpgManage
{
    public class CoCDodontoFPCData : DodontoFPCData
    {
        #region プロパティ

        private string _sendingSANText = "";
        public string SendingSANText
        {
            get { return _sendingSANText; }
            private set { this.SetProperty(ref this._sendingSANText, value); }
        }

        public void SetSANText()
        {
            this.SendingSANText = $"ccb<={this.Resources[(int)CoCResource.SAN]} [ 能力値 ]SANチェック";
        }

        #endregion

        #region イベント
        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);
            this.SetSANText();
        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
        }

        public override void ResetAllCorrection()
        {
            base.ResetAllCorrection();
            this.SetSANText();
        }
        #endregion

    }
}
