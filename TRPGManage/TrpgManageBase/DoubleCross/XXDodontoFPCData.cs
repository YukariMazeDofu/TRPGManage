using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ItoKonnyaku.Mvvm;

namespace ItoKonnyaku.TrpgManage
{
    public class XXDodontoFPCData : DodontoFPCData
    {
        #region コンストラクタ
        public XXDodontoFPCData() : base()
        {
            var rn = this.UsingSystem.ResourceNames();
            this._correctedResources = new BindableValue<int>[rn.Length];
            for(var i=0; i<rn.Length; i++)
            {
                this._correctedResources[i] = new BindableValue<int>(0);
            }

            this.IgnoreSyncVar.Add(nameof(this.ErosionEffectLv));
            this.IgnoreSyncVar.Add(nameof(this.ErosionDice));
        }

        #endregion

        #region 定数

        private static readonly int[] EffectErosions = { 0, 100, 160 };
        private static readonly int[] DiceErosions   = { 0,  60,  80, 100, 130, 160, 200, 240, 300 };

        #endregion

        #region プロパティ

        private BindableValue<int>[] _correctedResources;
        public BindableValue<int>[] CorrectedResources
        {
            get { return _correctedResources; }
            private set { this.SetProperty(ref this._correctedResources, value); }
        }

        public override Dictionary<string, string> GetCorrectedSendingData()
        {
            Dictionary<string, string> result;

            var resourceText = String.Join(",", this.UsingSystem.ResourceNames().Zip(this.CorrectedResources, (i, j) => $"{i}:{j.Value}").ToArray());

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

        public int Erosion
        {
            get => this.Resources.Items[(int)XXResource.Erosion].Value;
            private set { }
        }

        public int ErosionEffectLv
        {
            get => XXDodontoFPCData.CalcErosionEffectLv(this.Erosion);
            private set { }
        }

        public int ErosionDice
        {
            get => XXDodontoFPCData.CalcErosionDice(this.Erosion);
            private set { }
        }

        #endregion

        #region コマンド

        public static int CalcErosionEffectLv(int erosion)
        {
            return Array.IndexOf(EffectErosions, EffectErosions.LastOrDefault(i => erosion >= i));
        }

        public static int CalcErosionDice(int erosion)
        {
            return Array.IndexOf(DiceErosions, DiceErosions.LastOrDefault(i => erosion >= i));
        }

        public override void UpdateResource()
        {
            this.IsCanceledSync = true;

            this.Resources.ClearAllEvents();
            //this.Resources.Items.Clear();
            var rn = this.UsingSystem.ResourceNames();
            for (var i = 0; i < rn.Length; i++)
            {
                //this.Resources.AddItem(new BindableValue<int>(this.CorrectedResources.Items[i].Value));
                this.Resources[i].Value = this.CorrectedResources[i].Value;
            }

            //リソース管理の補正値リセットの実装、
            //ここでリソース管理の強制読み直し
            this.RefreshAllEvents();

            this.IsCanceledSync = false;

        }
        
        #endregion

        #region　イベント処理

        public event PropertyChangedEventHandler ErosionChanged;

        public void OnErosionChanged(object sender, PropertyChangedEventArgs e)
        {

            this.RaisePropertyChanged(nameof(this.ErosionEffectLv));
            this.RaisePropertyChanged(nameof(this.ErosionDice));

            this.ErosionChanged?.Invoke(this, new PropertyChangedEventArgs(this.Erosion.ToString()));
        }

        public override void ClearAllEvents()
        {
            base.ClearAllEvents();
            this.Resources.Items[(int)XXResource.Erosion].PropertyChanged -= this.OnErosionChanged;
        }

        protected override void SetAllEvents()
        {
            base.SetAllEvents();
            this.Resources.Items[(int)XXResource.Erosion].PropertyChanged += this.OnErosionChanged;
        }

        #endregion
    }
}
