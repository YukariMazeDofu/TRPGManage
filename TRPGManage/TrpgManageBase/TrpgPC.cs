#if DEBUG

//#define MAKE_DELAY_SERVER

#endif

using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using ItoKonnyaku.Mvvm;
using System.Windows.Input;
using System.ComponentModel;
using System.Net.Http;

using ItoKonnyaku.Commons.Extensions;
using System.IO;
using System.Security.Cryptography;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace ItoKonnyaku.TrpgManage
{

    public class TrpgPC : BindableBase
    {
        #region コンストラクタ

        public TrpgPC()
        {
        }

        #endregion

        #region プロパティ

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { this.SetProperty(ref this._name, value); }
        }

        private string _sheetUrl = "";
        public string SheetUrl
        {
            get { return _sheetUrl; }
            set { this.SetProperty(ref this._sheetUrl, value); }
        }

        private DodontoFPCData _dodontFPCData = new DodontoFPCData();
        public DodontoFPCData DodontoFPCData
        {
            get { return _dodontFPCData; }
            set { this.SetProperty(ref this._dodontFPCData, value); }
        }

        private ChatPalettes _chatPalettes = new ChatPalettes();
        public ChatPalettes ChatPalettes
        {
            get { return _chatPalettes; }
            set { this.SetProperty(ref this._chatPalettes, value); }
        }

        private DiceCounters _diceCounters = new DiceCounters();
        public DiceCounters DiceCounters
        {
            get { return _diceCounters; }
            set { this.SetProperty(ref this._diceCounters, value); }
        }

        //ダイス結果受信用プロパティ、継承先でオーバーライドして受信したい箇所に代入
        protected int _recievedData = 0;
        public virtual int RecievedData
        {
            get { return _recievedData; }
            set { this.SetProperty(ref this._recievedData, value); }
        }

        private static readonly HttpClient _httpClient = new HttpClient();

        #endregion

        #region コマンド
        
        public async static Task<TrpgPC> MakePCFromWebAsync(string name)
        {
            return await Task.Run(() => new TrpgPC { Name = name });
        }

        public static async Task<HtmlDocument> GetSheetDataAsync(string targetUrl, TrpgSystem trpgSystem)
        {
            var result = new HtmlDocument();

            try
            {
                var str = await _httpClient.GetStringAsync(targetUrl);
#if MAKE_DELAY_SERVER
                await Task.Delay(2000);
#endif
                
                //キャラクター保管所の対象システムシートであることの確認
                if (!Regex.Match(str, trpgSystem.CheckUrl()).Success) return null;

                result.LoadHtml(str);

            }
            catch
            {
                return null;
            }

            return result;
        }

        public UdonaCharacter MakeUdonaPCData(string addChatPalette)
        {
            var result = new UdonaCharacter();


            result.Chatpalette.DiceBot = this.GetType().GetTrpgSystem().ToString();
            result.Chatpalette.Value = MakeUdonaChatPalette(addChatPalette);

            result.Data.Data = new List<UdonaCharacterData>();

            //イメージ
            //イメージを追加しない場合でも空を入れておく必要あり。
            var images = this.DodontoFPCData.Images;
            var imgData = new UdonaCharacterData
            {
                Name = "image",
                Data = new List<UdonaCharacterData>
                    {
                        new UdonaCharacterData{ Name = "imageIdentifier" , Type = "image" }
                    }
            };

            var imagePath = "";
            if (images.Items.CheckIndex(images.SelectedIndex))
            {
                imagePath = this.DodontoFPCData.AbImages[this.DodontoFPCData.Images.SelectedIndex];
            }
            imgData.Data[0].Value = imagePath;
            result.Data.Data.Add(imgData);

            //コモンデータ
            var commonData = 
                new UdonaCharacterData
                {
                    Name = "common",
                    Data = new List<UdonaCharacterData>
                    {
                        new UdonaCharacterData{Name = "name", Value = this.Name},
                        new UdonaCharacterData{Name = "size", Value = this.DodontoFPCData.Size.ToString()},

                        //シートURLぶっこんでるけど、仕様外なのでエラーが出る可能性あり。
                        new UdonaCharacterData{Name = "url",  Value = this.SheetUrl},
                        new UdonaCharacterData{Name="説明", Type="note", Value=""},
                        new UdonaCharacterData{Name="メモ", Type="note", Value=""}

                    }
                };
            result.Data.Data.Add(commonData);

            //詳細データ、各システムごとに変わる
            result.Data.Data.Add(this.MakeUdonaDetailData());
                
            return result;
        }

        protected virtual UdonaCharacterData MakeUdonaDetailData()
        {
            var result = new UdonaCharacterData { Name = "detail" };
            result.Data = new List<UdonaCharacterData>();

            //リソース
            var resource = new UdonaCharacterData
            {
                Name = "リソース",
                Data = new List<UdonaCharacterData>
                {
                    new UdonaCharacterData{Name="HP", Type="numberResource",
                        CurrentValue = this.DodontoFPCData.Resources[(int)DiceBotResource.HP].Value.ToString(),
                        Value        = this.DodontoFPCData.Resources[(int)DiceBotResource.MaxHP].Value.ToString()
                    },
                    new UdonaCharacterData{Name="MP", Type="numberResource",
                        CurrentValue = this.DodontoFPCData.Resources[(int)DiceBotResource.MP].Value.ToString(),
                        Value        = this.DodontoFPCData.Resources[(int)DiceBotResource.MaxMP].Value.ToString()
                    }
                }
            };
            result.Data.Add(resource);

            return result;
        }

        protected virtual string MakeUdonaChatPalette(string addChatPalette)
        {
            var result = "//////////////////////////////チャット//////////////////////////////\r\n";

            result += string.Join("\r\n", this.ChatPalettes.Items.Select(i => i.Value).ToArray());

            return result;
        }

        public virtual void RefreshAllEvents()
        {
            this.ChatPalettes.RefreshAllEvents();
            this.DodontoFPCData.RefreshAllEvents();
        }

        public virtual void ResetAllCorrection()
        {
            this.DiceCounters.ResetAllCorrection();
            this.DodontoFPCData.ResetAllCorrection();
        }

        public override string ToString()
        {
            return this.Name;
        }

        #endregion

    }

    public class IntelliTrpgManageBaseDB : TrpgManageBase<TrpgPC> { }
}
