using ItoKonnyaku.Commons.Extensions;
using ItoKonnyaku.Mvvm;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace ItoKonnyaku.TrpgManage
{

    public class XXCharacterValues : BindableValueList<int>
    {
        #region コンストラクタ

        public XXCharacterValues()
        {
            this.SelectedIndex = 0;
        }

        #endregion

        #region コマンド
        public void SetSendingText()
        {
 
        }

        public override string ToString()
        {
            return $"要素数：{this.Items?.Count()}/選択中：{((XXCharacterValueName)this.SelectedIndex).Name()}（{this.SelectedItem}）";
        }
        #endregion

        #region　イベント処理

        protected override void InitializeDelegateCommand()
        {
            base.InitializeDelegateCommand();
            this.ResetAllCorrectionCommand = new DelegateCommand(this.ResetAllCorrection);
        }


        public ICommand ResetAllCorrectionCommand { get; private set; }
        public virtual void ResetAllCorrection()
        {

        }


        public override void OnItemChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnItemChanged(sender, e);
        }

        public override void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(sender, e);

        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            //this.SetSendingText();
        }

        #endregion
    }

    public enum XXCharacterValueName
    {//    0            1         2       3
        Physical, Perceptional, Mental, Social
    }

    public static class XXCharacterValueNameExtention
    {
        public static string Name(this XXCharacterValueName target)
        {
            string[] names = { "肉体", "感覚", "精神", "社会" };
            return names[(int)target];
        }
    }

}


