using ItoKonnyaku.Commons.Extensions;
using ItoKonnyaku.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItoKonnyaku.TrpgManage
{

    public class ArDodontoFPCData : DodontoFPCData
    {
        #region プロパティ

        private ArCosts _costs = new ArCosts();
        public ArCosts Costs
        {
            get { return _costs; }
            set { this.SetProperty(ref this._costs, value); }
        }

        private BindableValueList<int> _correctedResource = new BindableValueList<int>();
        public BindableValueList<int> CorrectedResources
        {
            get { return _correctedResource; }
            set { this.SetProperty(ref this._correctedResource, value); }
        }

        #endregion

        #region コマンド

        public override void CalcCorrectedResources(object sender, PropertyChangedEventArgs e)
        {
            this.CorrectedResources.ClearAllEvents();
            this.CorrectedResources.Items.Clear();

            for(var i = 0; i<this.Resources.Items.Count; i++)
            {
                this.CorrectedResources.AddItem(new BindableValue<int>(this.Resources.Items[i].Value + this.Costs.SendingCost.Items[i].Value));
            }
            var target = this.CorrectedResources.Items;
            target[(int)ArResource.HP].Value = target[(int)ArResource.HP].Value.InRange(-100, target[(int)ArResource.MaxHP].Value);
            target[(int)ArResource.MP].Value = target[(int)ArResource.MP].Value.InRange(-100, target[(int)ArResource.MaxMP].Value);

            this.SendingText = this.Costs.SendingText;
        }

        public override Dictionary<string, string> GetCorrectedSendingData()
        {
            Dictionary<string, string> result;

            var resourceText = String.Join(",", this.UsingSystem.ResourceNames().Zip(this.CorrectedResources.Items, (i, j) => $"{i}:{j.Value}").ToArray());

            result = new Dictionary<string, string>();

            if (this.ToSendData[0]) { result.Add("x", this.X.ToString()); }
            if (this.ToSendData[1]) { result.Add("y", this.Y.ToString()); }
            if (this.ToSendData[2]) { result.Add("size", this.Size.ToString()); }
            if (this.ToSendData[3]) { result.Add("initiative", this.Initiative.ToString()); }
            if (this.ToSendData[4]) { result.Add("rotation", this.Rotation.ToString()); }
            result.Add("counters", resourceText);

            if (this.Images.SelectedIndex != -1)
            {
                result.Add("image", this.Images[this.Images.SelectedIndex].Value);
            }

            return result;
        }

        public override void UpdateResource()
        {
            this.IsCanceledSync = true;

            this.Resources.ClearAllEvents();
            //this.Resources.Items.Clear();

            for (var i = 0; i < this.CorrectedResources.Items.Count; i++)
            {
                //this.Resources.AddItem(new BindableValue<int>(this.CorrectedResources.Items[i].Value));
                this.Resources[i].Value = this.CorrectedResources.Items[i].Value;
            }

            this.Costs.ReCalcSendCost();
            this.Costs.ResetAllCorrection();
            this.RefreshAllEvents();

            this.IsCanceledSync = false;
            //リソース管理の補正値リセットの実装、
            //ここでリソース管理の強制読み直し
        }

        public override void ResetAllCorrection()
        {
            base.ResetAllCorrection();
            this.Costs.ResetAllCorrection();
        }
        #endregion

        #region　イベント処理

        public override void ClearAllEvents()
        {
            base.ClearAllEvents();
            this.Costs.CostChanged -= this.CalcCorrectedResources;
        }

        protected override void SetAllEvents()
        {
            base.SetAllEvents();
            this.Costs.CostChanged += this.CalcCorrectedResources;
        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            this.Costs.RefreshAllEvents();

            //情報欄更新
            this.Costs.ReCalcSendCost();
        }

        #endregion
    }
}
