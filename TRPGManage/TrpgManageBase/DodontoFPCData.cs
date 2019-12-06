using ItoKonnyaku.Mvvm;
using Microsoft.CSharp.RuntimeBinder;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Input;

namespace ItoKonnyaku.TrpgManage
{

    public class DodontoFPCData : BindableBase
    {
        #region コンストラクタ

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DodontoFPCData(TrpgSystem trpgSystem)
        {
            InitializeDelegateCommand();

            this.UsingSystem = trpgSystem;
            var rn = this.UsingSystem.ResourceNames();
            this._resources = new BindableValueList<int>();
            //Enumerable.Repeat(0, rn.Count()).Select(i => this._resources.AddItem(new BindableValue<int>(i)));
            for(var i = 0; i < rn.Count(); i++)
            {
                this._resources.AddItem(new BindableValue<int>(0));
            }

            this.IgnoreSyncVar = new List<string>{
                nameof(this.Images.SelectedItem),
                nameof(this.AbImages)
            };
        }

        public DodontoFPCData() : this(TrpgSystem.DiceBot)
        { }

        #endregion
        
        #region プロパティ

        private TrpgSystem _usingSystem = TrpgSystem.DiceBot;
        public TrpgSystem UsingSystem
        {
            get { return _usingSystem; }
            set { this.SetProperty(ref this._usingSystem, value); }
        }

        private double _x = 0d;
        public double X
        {
            get { return _x; }
            set { this.SetProperty(ref this._x, Math.Floor(value)); }
        }

        private double _y = 0d;
        public double Y
        {
            get { return _y; }
            set { this.SetProperty(ref this._y, Math.Floor(value)); }
        }

        private int _size = 1;
        public int Size
        {
            get { return _size; }
            set { this.SetProperty(ref this._size, value); }
        }

        private double _initiative = 0d;
        public double Initiative
        {
            get { return _initiative; }
            set { this.SetProperty(ref this._initiative, Math.Floor(value)); }
        }

        private int _rotation = 0;
        public int Rotation
        {
            get { return _rotation; }
            set { this.SetProperty(ref this._rotation, value % 360); }
        }

        private ObservableCollection<bool> _toSendData = new ObservableCollection<bool>() { false, false, true, true, true };
        public ObservableCollection<bool> ToSendData
        {
            get { return _toSendData; }
            set { this.SetProperty(ref this._toSendData, value); }
        }

        private string _baseUrl = "";
        public string BaseUrl
        {
            get { return _baseUrl; }
            set
            {
                this.SetProperty(ref this._baseUrl, value);
                this.RaisePropertyChanged(nameof(this.AbImages));
            }
        }

        private BindableValueList<string> _images = new BindableValueList<string>();
        public BindableValueList<string> Images
        {
            get { return _images; }
            set { this.SetProperty(ref this._images, value); }
        }

        public ObservableCollection<string> AbImages
        {
            get
            {
                //Sync自動受信時にここでこける場合がある。
                ObservableCollection<string> result = null;
                try
                {
                    var res = this._images.Items.Select(i => (new Uri(new Uri(this._baseUrl), i.Value)).ToString());
                    result = new ObservableCollection<string>(res);
                }
                catch { }
                return result;
            }
            private set
            {
                //シリアライズされないようにするおまじない。
            }
        }

        private BindableValueList<int> _resources;
        public BindableValueList<int> Resources
        {
            get { return _resources; }
            set { this.SetProperty(ref this._resources, value); }
        }

        public string SendingText { get; set; } = "";

        //ServerSyncUpoload(データ更新にデータをアップロード)
        //--Sync時実行するコマンド。親(Base)のUploadを使う。親で代入する。
        public static ICommand SyncServerUpload { get; set; }
        
        //--一斉更新時（受信したとき）にSyncを実行しない。true中はSyncしない。
        public bool IsCanceledSync { get; set; } = false;

        //--Syncを無視する変数名、(Imagesに対するAbImages等）
        protected List<string> IgnoreSyncVar { get; set; }

        #endregion

        #region コマンド

        protected virtual void InitializeDelegateCommand()
        {
            this.ResetAllCorrectionCommand = new DelegateCommand(this.ResetAllCorrection);
        }

        public Dictionary<string, string> GetSendingData()
        {
            return this.GetSendingData(this.Images.SelectedIndex);
        }

        public Dictionary<string, string> GetSendingData(int imageIndex)
        {
            this.CalcCorrectedResources(this, null);

            Dictionary<string, string> result;

            var resourceText = String.Join(",", this.UsingSystem.ResourceNames().Zip(this.Resources.Items, (i, j) => $"{i}:{j.Value}").ToArray());

            result = new Dictionary<string, string>();

            if (this.ToSendData[0]) { result.Add("x", this.X.ToString()); }
            if (this.ToSendData[1]) { result.Add("y", this.Y.ToString()); }
            if (this.ToSendData[2]) { result.Add("size", this.Size.ToString()); }
            if (this.ToSendData[3]) { result.Add("initiative", this.Initiative.ToString()); }
            if (this.ToSendData[4]) { result.Add("rotation", this.Rotation.ToString()); }
            result.Add("counters", resourceText);

            if (imageIndex != -1)
            {
                result.Add("image", this.Images[imageIndex].Value);
            }

            return result;
        }

        public virtual Dictionary<string, string> GetCorrectedSendingData()
        {
            throw new NotImplementedException();
        }

        public virtual void UpdateResource()
        {
            throw new NotImplementedException();
        }

        public ICommand ResetAllCorrectionCommand { get; private set; }
        public virtual void ResetAllCorrection()
        {

        }

        public string SetRecievedData(dynamic Data, string BaseUrl)
        {
            //受信直後の更新時はServerSyncしない
            this.IsCanceledSync = true;

            this.CalcCorrectedResources(this, null);

            var result = "";

            try
            {
                this.BaseUrl = BaseUrl;
                this.X = Data.x;
                this.Y = Data.y;
                this.Size = (int)Data.size;
                this.Initiative = Data.initiative;
                this.Rotation = (int)Data.rotation;

                this.AbImages.Clear();
                this.Images.Items.Clear();

                var imgs = ((List<string>)Data.images).Where(i => !(i.Contains("defaultImageSet"))).Select(i => new BindableValue<string>(i)).ToList();
                this.Images = new BindableValueList<string>(imgs);
                this.Images.SelectedIndex = this.Images.Items.IndexOf(new BindableValue<string>(Data.imageName));

                //暫定対応。記号"/"を含むkeyをうまく認識できないので、さらに別の方法でデシリアライズするという暴挙に出る。

                var counterraw = (string)Data.counters.ToString();

                var ResourcesName = this.UsingSystem.ResourceNames();
                var js = new JavaScriptSerializer();
                dynamic json = js.DeserializeObject(counterraw);
                for (int i = 0; i < ResourcesName.Length; i++)
                    this.Resources[i].Value = int.TryParse(json[ResourcesName[i]].ToString(), out int value) ? value : 0;
                    
                this.RaisePropertyChanged(nameof(this.AbImages));


                //暫定対応ここまで。

                this.RefreshAllEvents();

                result = $"完了：どどんとふキャラデータ受信 ({Data.name})";
            }
            catch(RuntimeBinderException)
            {
                result = "エラー:どどんとふで該当キャラのステータス値を更新してみてください。";
            }
            finally
            {
                this.IsCanceledSync = false;
            }
            return result;

        }

        public virtual void CalcCorrectedResources(object sender, PropertyChangedEventArgs e)
        {

        }

        #endregion

        #region　イベント処理

        public virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //内部変数(下位変数も含む)の変更時、無視変数でなければSyncUpload
            if (!this.IsCanceledSync && !IgnoreSyncVar.Contains(e.PropertyName))
            {
                SyncServerUpload?.Execute(null);
            };
        }

        public virtual void ClearAllEvents()
        {
            this.PropertyChanged -= this.OnPropertyChanged;
            this.Resources.ItemChanged -= this.OnPropertyChanged;
            this.Images.PropertyChanged -= this.OnPropertyChanged;
        }

        protected virtual void SetAllEvents()
        {
            this.PropertyChanged += this.OnPropertyChanged;
            this.Resources.ItemChanged += this.OnPropertyChanged;
            this.Images.PropertyChanged += this.OnPropertyChanged;
        }

        public virtual void RefreshAllEvents()
        {
            this.Resources.RefreshAllEvents();
            this.Images.RefreshAllEvents();
            this.ClearAllEvents();
            this.SetAllEvents();
        }

        #endregion

    }

}
