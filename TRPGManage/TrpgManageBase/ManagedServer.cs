#if DEBUG

//#define MAKE_DELAY_SERVER

#endif

using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Codeplex.Data;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net;
using System.Collections.Specialized;
using System.Net.Http.Headers;
using ItoKonnyaku.Commons.Extensions;
using System.Threading;

namespace ItoKonnyaku.TrpgManage
{
    public class DodontoFServer : BindableBase
    {
        #region コンストラクタ

        public DodontoFServer()
        {
        }

        static DodontoFServer()
        {
            _credCache = new CredentialCache();
            _clientHandler = new HttpClientHandler()
            {
                Credentials = _credCache,
                PreAuthenticate = true
            };

            _httpClient = new HttpClient(_clientHandler);

        }

        #endregion

        #region プロパティ

        private DodontoFServerInfo _serverInfo = new DodontoFServerInfo();
        public DodontoFServerInfo ServerInfo
        {
            get { return _serverInfo; }
            set { this.SetProperty(ref this._serverInfo, value); }
        }

        private static readonly CredentialCache _credCache;
        private static readonly HttpClientHandler _clientHandler;
        private static readonly HttpClient _httpClient;

        private static bool _isBusy = false;

        //データ受信時、何秒前までのデータを読み込むか（デフォルト）
        private const string GetDataSec = "300";
        private const double SessionTimeOutSecond = 30d;

        #endregion

        #region コマンド

        //送信部実体、サーバーにPOSTし文字列を取得する。
        private async Task<string> PostHttpRequestAsync(string uri, FormUrlEncodedContent content)
        {

            string strres;
            try
            {
                //送信処理が完了しない間は送信を待つ。
                await WaitIdleAsync();

                _isBusy = true;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                _credCache.Remove(new Uri(uri), this.ServerInfo.AuthType);
                _credCache.Add(new Uri(uri), this.ServerInfo.AuthType, new NetworkCredential(this.ServerInfo.AuthUser, this.ServerInfo.AuthPass));

#if MAKE_DELAY_SERVER
                var response = await _httpClient.PostAsync(uri, content);
                await Task.Delay(1000);
                var strres = await response.Content.ReadAsStringAsync();
                await Task.Delay(1000);
#else
                var response = await _httpClient.PostAsync(uri, content);
                strres = await response.Content.ReadAsStringAsync();
#endif
            }
            finally
            {
                _isBusy = false;
            }
         
            return strres;

            async Task<bool> WaitIdleAsync()
            {
                //_isBusyがtrueの間はループで待つ(0.2秒間隔で確認)
                return await Task.Run(async () => {
                    while (true)
                    {
                        if (_isBusy != true) return true;
                        await Task.Delay(200);
                    }
                }).Timeout(TimeSpan.FromSeconds(SessionTimeOutSecond));
            }
        }
        
        //WebIFを送信し、結果をJsonオブジェクトで返す。
        public async Task<dynamic> SendWebifAsync(Dictionary<string, string> sendData)
        {
            dynamic result;
            var uri = this.ServerInfo.Url + "/DodontoFServer.rb";
            //[NeedCheck]URLの合成方法を正しい形に。サーバURL末尾に/が入っていても対応できるように。

            string strres = await PostHttpRequestAsync(uri, new FormUrlEncodedContent(sendData));

            try
            {
                result = DynamicJson.Parse(strres);
            }
            catch
            {
                throw new ApplicationException("JSON以外の返答がありました。(サーバー認証)");
            }
            return result;
        }

        //WebIF(Talk)を送信する。
        public async Task<dynamic> SendTalkAsync(string Name, string Message, bool Seacret = false)
        {
            dynamic result;

            var senddata = new Dictionary<string, string>()
            {
                {"webif"   , "talk" },
                {"room"    , this.ServerInfo.RoomNo },
                {"password", this.ServerInfo.Password },
                {"name"    , Name },
                {"message" , (Seacret? "s":"") + Message },
                {"bot"     , this.ServerInfo.Bot }
            };

            result = await this.SendWebifAsync(senddata);

            if (Seacret)
            {
                //実ダイス取得
                senddata["room"] = this.ServerInfo.SeacretRoomNo;
                senddata["password"] = this.ServerInfo.SeacretPassword;
                senddata["message"] = Message;

                result = await this.SendWebifAsync(senddata);
            }

            return result;
        }

        //WebIF(Refresh)を送信する。
        public async Task<dynamic> GetInfoAsync(Dictionary<string, string> getInfo)
        {
            dynamic result = new { result = "ERROR" };

            var senddata = new Dictionary<string, string>()
            {
                {"webif"   , "refresh" },
                {"room"    , this.ServerInfo.RoomNo },
                {"password", this.ServerInfo.Password },
                {"callback", ""}
            };
            getInfo.ToList().ForEach(x => senddata.Add(x.Key, x.Value));

            result = await this.SendWebifAsync(senddata);
            return result;
        }

        //WebIF(Refresh)を送信し、設定した名前のキャラ情報を抽出する。
        public async Task<dynamic> GetCharacterAsync(string Name)
        {
            dynamic result = null;

            var getInfo = new Dictionary<string, string>()
            {
                {"characters", "0"}
            };
            var resraw = await this.GetInfoAsync(getInfo);

            foreach (dynamic ch in resraw.characters)
            {
                if (ch.IsDefined("name") && ch.name == Name) { result = ch; break; }
            }

            return result;
        }

        //WebIF(addCharacter)を送信する。
        public async Task<dynamic> AddCharacterAsync(string Name, Dictionary<string, string> setInfo)
        {
            dynamic result;

            var senddata = new Dictionary<string, string>()
            {
                {"webif"   , "addCharacter" },
                {"room"    , this.ServerInfo.RoomNo },
                {"password", this.ServerInfo.Password },
                {"name"    , Name },
            };
            setInfo.ToList().ForEach(x => senddata.Add(x.Key, x.Value));

            result = await this.SendWebifAsync(senddata);
            return result;
        }

        //WebIF(changeCharacter)を送信する。
        public async Task<dynamic> ChangeCharacterAsync(string Name, Dictionary<string, string> setInfo)
        {
            dynamic result = new { result = "ERROR" };

            var senddata = new Dictionary<string, string>()
            {
                {"webif"     , "changeCharacter" },
                {"room"      , this.ServerInfo.RoomNo },
                {"password"  , this.ServerInfo.Password },
                {"targetName", Name }
            };
            setInfo.ToList().ForEach(x => senddata.Add(x.Key, x.Value));

            result = await this.SendWebifAsync(senddata);
            return result;
        }

        //WebIF(Chat)を送信する。
        public async Task<dynamic> GetTalkRawAsync(string Sec = GetDataSec, bool Seacret = false)
        {
            var senddata = new Dictionary<string, string>()
            {
                {"webif"   , "chat" },
                {"room"    , Seacret? this.ServerInfo.SeacretRoomNo : this.ServerInfo.RoomNo },
                {"password", Seacret? this.ServerInfo.SeacretPassword : this.ServerInfo.Password },
                {"sec"    , Sec }
            };

            dynamic result = await this.SendWebifAsync(senddata);
            return result;
        }

        //WebIF(Chat)を送信し、設定した名前のキャラ発言を抽出する。
        public async Task<dynamic> GetTalkAsync(string Name, string Sec = GetDataSec, bool Seacret = false)
        {
            var resraw = await this.GetTalkRawAsync(Sec, Seacret);

            //WebIfで送っていないものはsenderNameの末尾に/tが入るらしい。
            var result = new List<dynamic>();
            foreach (dynamic ch in resraw.chatMessageDataLog)
            {
                if (ch[1].senderName == Name) { result.Add(ch); }
            }

            return (result as dynamic);
        }

        //WebIF(Chat)を送信し、設定した名前のキャラの最新のダイス結果を抽出する。（名前指定なしで全員対象）
        public async Task<int> GetDiceRollResultAsync(string Name = "", string Sec = GetDataSec, bool Seacret = false)
        {
            var resraw = await this.GetTalkRawAsync(Sec, Seacret);
            var result = 0;
            var isSelectingName = (Name != string.Empty);
            try
            {
                var res = new List<dynamic>();
                foreach (dynamic ch in resraw.chatMessageDataLog)
                {
                    if (!isSelectingName || ch[1].senderName == Name)
                        res.Add(Regex.Match(ch[1].message, this.ServerInfo.Bot + @"[^""]+").Value);
                }
                result = res.Where(i => i != "").Select(i => ConvertDiceResult(i)).LastOrDefault();
            }
            catch
            {
                result = -1;
            }
            return result;
        }

        //WebIFより得たデータを成形する。Info.Botを使うため、動的。
        public string ConvertResult(dynamic result)
        {
            var res = (List<dynamic>)result;
            return Regex.Match(res[res.Count() - 1][1].message, this.ServerInfo.Bot + @"[^""]+").Value;
        }

        //WebIFより得たデータを成形し、ダイス結果のみを抽出する。
        //他のダイス結果の場合、エラーを返す
        public static int ConvertDiceResult(string result)
        {
            int res;
            try
            {
                res = int.Parse(Regex.Match(result, "(?<=→ )[0-9]+", RegexOptions.RightToLeft).Value);
            }
            catch
            {
                res = -1;
            }
            return res;
        }

#endregion
    }

    public class DodontoFServerInfo : BindableBase
    {
        #region プロパティ

        private string _url = "";
        public string Url
        {
            get { return _url; }
            set { this.SetProperty(ref this._url, value); }
        }

        private string _roomNo = "";
        public string RoomNo
        {
            get { return _roomNo; }
            set { this.SetProperty(ref this._roomNo, value); }
        }

        private string _password = "";
        public string Password
        {
            get { return _password; }
            set { this.SetProperty(ref this._password, value); }
        }

        private string _bot = "";
        public string Bot
        {
            get { return _bot; }
            set { this.SetProperty(ref this._bot, value); }
        }

        private string _status = "";
        public string Status
        {
            get { return _status; }
            set { this.SetProperty(ref this._status, value); }
        }

        private string _seacretRoomNo = "";
        public string SeacretRoomNo
        {
            get { return _seacretRoomNo; }
            set { this.SetProperty(ref this._seacretRoomNo, value); }
        }

        private string _seacretPassword = "";
        public string SeacretPassword
        {
            get { return _seacretPassword; }
            set { this.SetProperty(ref this._seacretPassword, value); }
        }

        private string _authUser = "";
        public string AuthUser
        {
            get { return _authUser; }
            set { this.SetProperty(ref this._authUser, value); }
        }

        private string _authPass = "";
        public string AuthPass
        {
            get { return _authPass; }
            set { this.SetProperty(ref this._authPass, value); }
        }

        private string _authType = "Digest";
        public string AuthType
        {
            get { return _authType; }
            set { this.SetProperty(ref this._authType, value); }
        }

        private bool _isSync = false;
        public bool IsSync
        {
            get => _isSync;
            set { this.SetProperty(ref this._isSync, value); }
        }

        private int _syncIntervalSec = 10;
        public int SyncIntervalSec
        {
            get => _syncIntervalSec;
            set { this.SetProperty(ref this._syncIntervalSec, value); }
        }

        #endregion

        #region コマンド

        public void ForceRaiseUrlChanged()
        {
            var reserve = this.Url;
            this.Url = string.Empty;
            this.Url = reserve;
            //this.RaisePropertyChanged(nameof(this.Url)); //これでは実際の値が変更されていないため、稼働しない。
        }

        #endregion
    }


}
