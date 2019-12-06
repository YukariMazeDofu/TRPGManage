using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

using ItoKonnyaku.Commons.Extensions;
using ItoKonnyaku.Commons.Windows;
using ItoKonnyaku.Mvvm;

using Prism.Commands;
using Prism.Mvvm;

using GongSolutions.Wpf.DragDrop;

namespace ItoKonnyaku.TrpgManage
{
    public class TrpgManageBase<P> : BindableBase, IDropTarget where P : TrpgPC
    {
        #region コンストラクタ

        public TrpgManageBase()
        {
            InitializeDelegateCommand();

            //バージョン表示関係
            System.Diagnostics.FileVersionInfo ver =
                System.Diagnostics.FileVersionInfo.GetVersionInfo(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            this.Version = string.Format("{0} {1}\n{2}\n{3}"
                                        , ver.FileDescription
                                        , ver.ProductVersion
                                        , ver.Comments
                                        , ver.LegalCopyright);

            //シリアライズファイル読み込み
            this.LoadData();

            //起動時Shift押下でWindowSettingを初期化する。
            if (IsShiftKeyPressed)
            {
                this.InitializeWindowSettings();
            }


            this.RefreshAllEvents();

            //SyncServer用タイマ設定(起動時は設定なし)
            this.Data.Server.ServerInfo.IsSync = false;

            this.Data.ResetAllCorrection();
        }

        #endregion

        #region 定数

        //プログラムバージョン
        public string Version { get; private set; }

        //プログラム位置
        private static readonly string ProgramPath =
            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        //シリアライズ用タイプ
        private static readonly Type[] types = new Type[]{
                typeof(CoCDodontoFPCData),
                typeof(ArDodontoFPCData),
                typeof(XXDodontoFPCData),
                typeof(ISRollDice)
            };

        //使用TRPGシステム（Pから判断)
        public static readonly TrpgSystem UsingSystem = typeof(P).GetTrpgSystem();

        //セッションタイムアウト時間
        private static readonly double SessionTimeOutSecond = 30d;

        //バックアップ個数
        private static readonly int BackupCount = 9;

        #endregion
        
        #region プロパティ

        public TrpgManageData<P> Data { get; set; } = new TrpgManageData<P>();

        private readonly SeacretResultWindow SeacretResultWindow = new SeacretResultWindow();

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        //WebSheetから受信中か。受信中は再受信不能にする。
        private bool _isGettingWebSheet = false;
        private bool IsGettingWebSheet
        {
            get => this._isGettingWebSheet;
            set
            {
                this._isGettingWebSheet = value;
                (this.AddPCFromWebCommand as DelegateCommand<string>)?.RaiseCanExecuteChanged();
            }
        }

        //SyncServer関係
        //--Upload
        private static readonly int _syncUploadInterval = 1000;
        private static bool _isCoolingUpload = false;
        private static Timer _coolingUploadTimer;
        private static bool _queueInCooling = false;

        //--Download
        private static Timer _syncDownloadTimer;

        private static readonly HttpClient _httpClient = new HttpClient();

        #endregion
        
        #region コマンド

        //コマンド初期化
        protected void InitializeDelegateCommand()
        {
            this.DoDebugCommand = new DelegateCommand<string>(async i => await this.DoDebug(i));

            this.SaveCommand = new DelegateCommand(() => this.SaveData());

            this.ClosingCommand = new DelegateCommand(() => this.Closing(true));

            this.ForceCloseCommand = new DelegateCommand(() => { this.Closing(false); Environment.Exit(0); });

            this.AllResetCommand = new DelegateCommand<string>(i => this.AllReset(i));

            this.PasteClipboardCommand = new DelegateCommand<TextBox>(this.PasteClipboard);

            this.ReloadWebBrowserCommand = new DelegateCommand(this.ReloadWebBrowser);

            this.JumpToUrlCommand = new DelegateCommand<string>(this.JumpToUrl);

            this.ShowSeacretResultWindowCommand = new DelegateCommand(() => { this.SeacretResultWindow.Show(); this.SeacretResultWindow.Activate(); });

            this.GetDodontoFPCDataCommand = new DelegateCommand(async () => await this.GetDodontoFPCDataAsync(this.Data.PCs.SelectedItem));

            this.SetDodontoFPCDataCommand = new DelegateCommand(async () => await this.SetDodontoFPCDataAsync(this.Data.PCs.SelectedItem));

            this.SetDodontoFPCDataCorrectedCommand = new DelegateCommand(async () => await this.SetDodontoFPCDataCorrectedAsync(this.Data.PCs.SelectedItem));

            this.MakePConDodontoFCommand = new DelegateCommand(async () => await this.MakePConDodontoFAsync(this.Data.PCs.SelectedItem));

            this.GetResentDiceRollResultCommand = new DelegateCommand(async () => await this.GetResentDiceRollResultAsync() );

            this.SendDiceRollCommand = new DelegateCommand<string>(async i =>
                {
                    await this.SendDodontoFTalkAsync(i, this.Data.IsSeacretDice);
                    this.Data.PCs.SelectedItem.ResetAllCorrection();
                }
            );

            this.SendTalkCommand = new DelegateCommand<string>(async i => await this.SendDodontoFTalkAsync(i, false));

            this.SeacretResultWindow.SendResultStringCommand = new DelegateCommand<string>(async i => await this.SendDodontoFTalkAsync(i, false));

            this.AddPCFromWebCommand = new DelegateCommand<string>(async i => await AddPCFromWebAsync(i), _ => !this.IsGettingWebSheet);

            this.ExportPCCommand = new DelegateCommand(this.ExportPC);

            this.RemovePCCommand = new DelegateCommand(this.RemovePC);

            this.ExportUdonaPCCommand = new DelegateCommand(async () => {
                var addChatPalette = "";
                try
                {
                    addChatPalette = System.IO.File.ReadAllText($@"{ProgramPath}\Data\UdonariumChatPalette.txt");
                }
                catch (FileNotFoundException)
                {
                }
                await this.ExportUdonaPC(this.Data.PCs.SelectedItem.MakeUdonaPCData(addChatPalette));
            });

            DodontoFPCData.SyncServerUpload = new DelegateCommand(async () => await this.SyncServerUpload());

        }

        //デバッグ用コマンド
        public ICommand DoDebugCommand { get; private set; }
        public async Task DoDebug(string debugStr)
        {
#if DEBUG
            await Task.Run(() => true); //エラー除け

            //#define MAKE_DELAY_SERVER
            //　意図的にサーバー遅延状況(2000ms)を作り出す。(Debug時限定)
            //　必要時に下記ファイルの先頭のdefineを定義すること。
            //
            //　TrpgPC.cs　　　　　キャラシート保管所サーバー
            //　ManagedServer.cs　 どどんとふサーバー

#if false   //連続投稿テスト（同期）

            await CheckSendManyMessageContinuous();
            async Task CheckSendManyMessageContinuous() 
            {
                await (this.Data.Server.SendTalkAsync("ユカリ", $"連続投稿時(同期)に非同期でUIが動くかの検証ですよ！"));
                for (var i = 1; i <= 10; i++)
                    await (this.Data.Server.SendTalkAsync("ユカリ", $"連続投稿はめっ！ですよ！{i.ToString()}回も投稿してます！"));
            }
#endif
#if false   //連続投稿テスト（非同期）
            
            await CheckSendManyMessageAsync();
            async Task CheckSendManyMessageAsync() 
            {
                await (this.Data.Server.SendTalkAsync("ユカリ", $"連続投稿時(非同期)に非同期でUIが動くかの検証ですよ！"));
                var test = new List<Task<dynamic>>();
                for (var i = 1; i <= 10; i++)
                    test.Add(this.Data.Server.SendTalkAsync("ユカリ", $"連続投稿はめっ！ですよ！{i.ToString()}回も投稿してます！"));
                await Task.WhenAll(test.ToArray());
            }
#endif
#if false   //IsInRangeテスト
            await TestIsInRange();
            async Task TestIsInRange()
            {
                var test1 = 2;
                var testres = test1.IsInRange(3, 4).ToString();
                await Task.Run(() => this.UpdateStatus(testres)).ConfigureAwait(false);
            }
#endif

#if false    //InSANeテスト
            this.UpdateStatus((this.Data.PCs.SelectedItem as ISPC).RollDices.SelectRoll(2,8).ToString());

#endif

#if true    //サーバ情報出力
            SerializeToXml($@"{ProgramPath}\Data\InitialData.xml", this.Data);
            await Task.Run(() => this.UpdateStatus(@"現在のサーバ設定を\Data\InitialServer.xmlとして出力しました。")).ConfigureAwait(false);
#endif

#else
            SerializeToXml($@"{ProgramPath}\Data\InitialServer.xml", this.Data.Server.ServerInfo);
            await Task.Run(() => this.UpdateStatus(@"現在のサーバ設定を\Data\InitialServer.xmlとして出力しました。")).ConfigureAwait(false);
#endif
        }

        //各種オブジェクトのシリアライズ/デシリアライズ
        public static bool SerializeToXml<T>(string xmlPath, T data)
        {
            DirectoryUtils.SafeCreateDirectory(xmlPath);
            var Serializer = new DataContractSerializer(typeof(T), types);

            var Settings = new XmlWriterSettings
            {
                Encoding = System.Text.Encoding.UTF8,
                Indent = true
            };

            using (var fs = XmlWriter.Create(xmlPath, Settings))
            {
                Serializer.WriteObject(fs, data);
                fs.Flush();
            }
            return true;
        }

        public static T DeserializeFromXml<T>(string xmlPath)
        {
            T result = default;
            var Serializer = new DataContractSerializer(typeof(T), types);
            var xmlSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
            using (var fs = XmlReader.Create(xmlPath, xmlSettings))
            {
                result = (T)Serializer.ReadObject(fs);
            }
            return result;
        }

        //全データセーブ
        public ICommand SaveCommand { get; private set; }
        public void SaveData()
        {
            BackupData();

            SerializeToXml($@"{ProgramPath}\Data\SaveData.xml", Data);
            this.UpdateStatus("完了：セーブ ");

            //データセーブのバックアップを取っておく。
            void BackupData()
            {
                for(var i = BackupCount; i>0 ; i--)
                {
                    var BeforePath = $@"{ProgramPath}\Data\SaveData.xml.b{i-1}".Replace(".b0","");
                    var AfterPath  = $@"{ProgramPath}\Data\SaveData.xml.b{i}";

                    if (File.Exists(AfterPath))  File.Delete(AfterPath);
                    if (File.Exists(BeforePath)) File.Copy(BeforePath, AfterPath);

                }

                return;
            }
        }

        //全データロード
        public void LoadData()
        {
            string result;
            try
            {
                this.Data = DeserializeFromXml<TrpgManageData<P>>($@"{ProgramPath}\Data\SaveData.xml");
                result = "完了：ロード";
            }
            catch
            {
                this.Data = new TrpgManageData<P>();
                result = "完了：新規起動(SaveData.xml無し)";
                
            }
            this.Data.WindowSetting.CutIn.CutInTrigger = false;
            this.Data.RefreshAllEvents();
            this.UpdateStatus(result);
        }

        //終了時
        public ICommand ClosingCommand { get; private set; }
        public ICommand ForceCloseCommand { get; private set; }

        public void Closing(bool saveData = true)
        {
            this.SeacretResultWindow.CloseConfirm = true;
            this.SeacretResultWindow.Close();
            if(saveData) this.SaveCommand.Execute(null);
        }

        //リセット
        public ICommand AllResetCommand { get; private set; }
        public void AllReset(string trpgSystemStr)
        {
            try
            {
                this.Data = DeserializeFromXml<TrpgManageData<P>>($@"{ProgramPath}\Data\InitialData.xml");
            }
            catch
            {
                this.Data = new TrpgManageData<P>();
            }
            this.SaveCommand.Execute(null);
            Environment.Exit(0);
        }

        //指定したテキストボックスにクリップボード内容をペースト
        public ICommand PasteClipboardCommand { get; private set; }
        public void PasteClipboard(TextBox target)
        {
            target.Text = Clipboard.GetText();
        }

        public ICommand ReloadWebBrowserCommand { get; private set; }
        public void ReloadWebBrowser()
        {
            this.Data.Server.ServerInfo.ForceRaiseUrlChanged();
        }

        //パラメータのURLに飛ぶ
        public ICommand JumpToUrlCommand { get; private set; }
        public void JumpToUrl(string url)
        {
            System.Diagnostics.Process.Start(url);
        }

        //シークレットダイス結果表示ウインドウを表示
        public ICommand ShowSeacretResultWindowCommand { get; private set; }

        //どどんとふキャラデータ受信
        public ICommand GetDodontoFPCDataCommand { get; private set; }
        public async Task GetDodontoFPCDataAsync(TrpgPC target)
        {
            this.UpdateStatus("処理中：どどんとふキャラデータ受信");

            var url = this.Data.Server.ServerInfo.Url;

            dynamic setdata;

            try
            {
                setdata = await this.Data.Server.GetCharacterAsync(target.Name).Timeout(TimeSpan.FromSeconds(SessionTimeOutSecond));
            }
            catch (TimeoutException)
            {
                this.UpdateStatus("エラー：タイムアウト、サーバーからの返答がありません。");
                return;
            }
            catch (ApplicationException e)
            {
                this.UpdateStatus($"エラー：{e.Message}");
                return;
            }

            if (setdata == null)
            {
                this.UpdateStatus($"エラー：({target.Name}) という名前のキャラクターはこの部屋に存在しません。");
            }
            else
            {
                var res = target.DodontoFPCData.SetRecievedData(setdata, url);
                this.UpdateStatus(res);
            }
        }

        //どどんとふキャラデータ送信(上書)
        public ICommand SetDodontoFPCDataCommand { get; private set; }
        public async Task SetDodontoFPCDataAsync(TrpgPC target)
        {
            this.UpdateStatus("処理中：WebIf送信(情報更新)");
            var res = target.DodontoFPCData.GetSendingData();
            if (res == null)
            {
                this.UpdateStatus("エラー：送信用キャラデータが作成できません。");
                return;
            }
            try
            {
                await Data.Server.ChangeCharacterAsync(target.Name, res).Timeout(TimeSpan.FromSeconds(SessionTimeOutSecond));
                this.UpdateStatus($"完了：どどんとふキャラデータ送信 ({target.Name})");
            }
            catch (TimeoutException)
            {
                this.UpdateStatus("エラー：タイムアウト、サーバーからの返答がありません。");
                return;
            }
            catch (ApplicationException e)
            {
                this.UpdateStatus($"エラー：{e.Message}");
                return;
            }
        }

        //どどんとふキャラデータ送信（補正済み上書)
        public ICommand SetDodontoFPCDataCorrectedCommand { get; private set; }
        public async Task SetDodontoFPCDataCorrectedAsync(TrpgPC target)
        {
            this.UpdateStatus("処理中：WebIf送信(情報更新)");

            if (!DialogUtils.ShowDialogWindow("<送信確認>\n\n" +
                                               target.DodontoFPCData.SendingText +
                                          "\n\n送信してもよろしいですか？"
                                            , "送信確認"))
            { return; }

            //カットイン
            if (this.Data.WindowSetting.CutIn.IsCutIn)
            {
                this.Data.WindowSetting.CutIn.ShowCutIn(target.DodontoFPCData.SendingText);
            }

            var res = target.DodontoFPCData.GetCorrectedSendingData();
            if (res == null)
            {
                this.UpdateStatus("エラー：送信用キャラデータが作成できません。");
                return;
            }
            try
            {
                await this.Data.Server.SendTalkAsync(target.Name, target.DodontoFPCData.SendingText, false).Timeout(TimeSpan.FromSeconds(SessionTimeOutSecond));

                await this.Data.Server.ChangeCharacterAsync(target.Name, res).Timeout(TimeSpan.FromSeconds(SessionTimeOutSecond));

                this.Data.PCs.SelectedItem.DodontoFPCData.UpdateResource();
                this.UpdateStatus($"完了：どどんとふキャラデータ送信 ({target.Name})");

                if (this.Data.IsLogging)
                {
                    Logger.Info("[Resource]\t" + target.Name + "\t" + target.DodontoFPCData.SendingText + "\t" + string.Join("\t", target.DodontoFPCData.Resources.Items.Select(i => i.Value).ToArray()));
                }
            }
            catch (TimeoutException)
            {
                this.UpdateStatus("エラー：タイムアウト、サーバーからの返答がありません。");
                return;
            }
            catch (ApplicationException e)
            {
                this.UpdateStatus($"エラー：{e.Message}");
                return;
            }
        }

        //どどんとふキャラデータ送信(新規)
        public ICommand MakePConDodontoFCommand { get; private set; }
        public async Task MakePConDodontoFAsync(TrpgPC target)
        {
            this.UpdateStatus("処理中：どどんとふキャラデータ作成");
            var res = target.DodontoFPCData.GetSendingData(-1);
            if (res == null)
            {
                this.UpdateStatus("エラー：送信用キャラデータが作成できません。");
                return;
            }
            res.Add("url", target.SheetUrl);

            try
            {
                var r = await Data.Server.AddCharacterAsync(target.Name, res).Timeout(TimeSpan.FromSeconds(SessionTimeOutSecond));
                this.UpdateStatus(r.result.ToString());
            }
            catch (TimeoutException)
            {
                this.UpdateStatus("エラー：タイムアウト、サーバーからの返答がありません。");
                return;
            }
            catch (ApplicationException e)
            {
                this.UpdateStatus($"エラー：{e.Message}");
                return;
            }
        }

        //どどんとふより最新ダイスロール結果を取得
        public ICommand GetResentDiceRollResultCommand { get; private set; }
        public async Task GetResentDiceRollResultAsync()
        {
            this.UpdateStatus("処理中：WebIF送信(ダイス結果取得)");
            int result;
            try
            {
                //システムごとに名前で選ぶか選択する。
                var SelectingName = UsingSystem.IsSelectingByNameOnDiceRollResult()? this.Data.PCs.SelectedItem.Name : "";
                result = await this.Data.Server.GetDiceRollResultAsync(Name: SelectingName).Timeout(TimeSpan.FromSeconds(SessionTimeOutSecond));
            }

            catch (TimeoutException)
            {
                this.UpdateStatus("エラー：タイムアウト、サーバーからの返答がありません。");
                return;
            }
            catch (ApplicationException e)
            {
                this.UpdateStatus($"エラー：{e.Message}");
                return;
            }

            if (result == -1)
            {
                this.UpdateStatus("エラー：ダイスロール結果を受信できませんでした。");
                return;
            }
            this.Data.PCs.SelectedItem.RecievedData = result;
            this.UpdateStatus($"完了：WebIF送信（ダイス結果取得:{result}）");
        }

        //トーク送信系
        public ICommand SendDiceRollCommand { get; private set; }
        public ICommand SendTalkCommand { get; private set; }

        //トーク実体
        public async Task SendDodontoFTalkAsync(string message, bool isSeacretDice)
        {
            var senderName = this.Data.PCs.SelectedItem.Name;

            this.UpdateStatus("処理中：WebIf送信(発言)");

            try
            {
                if (!DialogUtils.ShowDialogWindow("<送信確認>\n\n" +
                                                   message +
                               (isSeacretDice ? "\n※シークレットダイス※" : "") +
                                              "\n\n送信してもよろしいですか？"
                                                , "送信確認"))
                { return; }


                //文章送信
                await this.Data.Server.SendTalkAsync(senderName, message, isSeacretDice).Timeout(TimeSpan.FromSeconds(SessionTimeOutSecond));


                //結果受信
                this.UpdateStatus("処理中：WebIf送信(情報取得)");
                string result = this.Data.Server.ConvertResult(
                        await this.Data.Server.GetTalkAsync(senderName, "10", isSeacretDice).Timeout(TimeSpan.FromSeconds(SessionTimeOutSecond))
                    );

                //カットイン
                if (this.Data.WindowSetting.CutIn.IsCutIn)
                {
                    var resText = Regex.Match(result, @"→ [0-9]+( → .*)?$").Value;
                    var cutinText = $"{message}\n　{resText}";
                    this.Data.WindowSetting.CutIn.ShowCutIn(cutinText);
                }

                //受信完了
                this.UpdateStatus($"完了：{result}");
                this.Data.PCs.SelectedItem.ResetAllCorrection();

                //シークレットウインドウ処理(結果の追加、表示)
                if (isSeacretDice)
                {
                    this.SeacretResultWindow.SeacretResults.Add("シークレットダイス結果：\n" + senderName + "：" + message + "\n" + result);
                    this.SeacretResultWindow.SelectLatest();
                    this.SeacretResultWindow.Show();
                    this.SeacretResultWindow.Activate();
                }

                //ロギング
                if (this.Data.IsLogging)
                {
                    Logger.Info("[DiceRoll]\t" + senderName + "\t" + message.Replace("\n", "") + "\t" + this.Data.IsSeacretDice.ToString() + "\t" + result + "\t" + DodontoFServer.ConvertDiceResult(result).ToString());
                }

            }
            catch (TimeoutException)
            {
                this.UpdateStatus("エラー：タイムアウト、サーバーからの返答がありません。");
                return;
            }
            catch (ApplicationException e)
            {
                this.UpdateStatus($"エラー：{e.Message}");
                return;
            }
        }

        //キャラクター追加
        public ICommand AddPCFromWebCommand { get; private set; }
        public async Task AddPCFromWebAsync(string Url)
        {
            if (IsGettingWebSheet) return;　//CanExecuteで実行不可能にしているので実際は不要。安全のため。

            TrpgPC NewPC;
            try
            {
                this.UpdateStatus("処理中：キャラクターシート受信中(処理に3sec程かかります)");

                this.IsGettingWebSheet = true;
                
                NewPC = await UsingSystem.MakePCFromWebAsync().Invoke(Url).Timeout(TimeSpan.FromSeconds(SessionTimeOutSecond));

            }
            catch (TimeoutException)
            {
                this.UpdateStatus("エラー：タイムアウト、サーバーからの返答がありません。");
                return;
            }
            finally
            {
                this.IsGettingWebSheet = false;
            }

            if (NewPC == null)
            {
                this.UpdateStatus("エラー：キャラクター情報が読み込めません。キャラクター保管所のURLか確認してください。");
                return;
            }

            this.AddPC(NewPC);


        }

        //キャラクターインポート
        public void DragOver(IDropInfo dropInfo)
        {
            var files = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>();
            dropInfo.Effects = files.Any(fname => fname.EndsWith(".xml"))
                ? DragDropEffects.Copy : DragDropEffects.None;
        }

        public void Drop(IDropInfo dropInfo)
        {
            var files = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>()
                .Where(fname => fname.EndsWith(".xml")).ToList();
            foreach (var path in files)
            {
                this.AddPCFromFile(path);
            }
        }

        public void AddPCFromFile(string path)
        {
            TrpgPC aPC;
            try
            {
                aPC = DeserializeFromXml<P>(path);
            }
            catch(SerializationException)
            {
                this.UpdateStatus("エラー：読込不可。正規のキャラクターxmlファイルではないか、使用システムが違います。");
                return;
            }
            this.AddPC(aPC);
        }

        //キャラ追加実体
        public void AddPC(TrpgPC NewPC)
        {
            var index = Data.PCs.Items.IndexOf(Data.PCs.Items.FirstOrDefault(i => i.Name == NewPC.Name));
            if (index != -1)
            {
                if (DialogUtils.ShowDialogWindow($"<上書確認>\n\nキャラ名（{NewPC.Name}）はすでに登録されています。上書きしますか？", "上書確認"))
                {
                    this.Data.PCs.RemoveItem(index);
                    Data.PCs.SelectedIndex = this.Data.PCs.AddItem(NewPC as P);
                    this.Data.RefreshAllEvents();
                    this.UpdateStatus($"完了：キャラ名（{NewPC.Name}）を上書きしました。");
                }
                else
                {
                    this.UpdateStatus("エラー：同名のキャラクターは複数追加できません。");
                }
            }
            else
            {
                Data.PCs.SelectedIndex = this.Data.PCs.AddItem(NewPC as P);
                this.UpdateStatus($"完了：キャラ名（{NewPC.Name}）を追加しました。");
                this.Data.RefreshAllEvents();
            }
        }
        
        //キャラクターエクスポート
        public ICommand ExportPCCommand { get; private set; }
        public void ExportPC()
        {
            var Target = this.Data.PCs.SelectedItem;
            var Caption = $"{ Target.Name }({ UsingSystem.ToString()})";
            var Path = $@"{ProgramPath}\Export\{Caption}.xml";

            try
            {
                SerializeToXml(Path, Target);
                this.UpdateStatus($@"完了：エクスポートしました。(.\Export\{ Caption }.xml)");
            }
            catch(IOException)
            {
                this.UpdateStatus($"エラー：エクスポートに失敗しました。");
            }
        }

        //キャラクター削除（Confirmしたかった）
        public ICommand RemovePCCommand { get; private set; }
        public void RemovePC()
        {

            if (!DialogUtils.ShowDialogWindow($"<削除確認>\n\n" +
                                                this.Data.PCs.SelectedItem.Name +
                                           "\n\nキャラクターを削除してもよろしいですか？"
                                             , "削除確認"))
            { return; }

            //消す前に表示しないといけませんよ！
            this.UpdateStatus($"完了：キャラクター{this.Data.PCs.SelectedItem.Name}を削除しました。");
            this.Data.PCs.RemoveItem(this.Data.PCs.SelectedIndex);
        }

        //Udonariumエクスポート
        public ICommand ExportUdonaPCCommand { get; private set; }
        public async Task ExportUdonaPC(UdonaCharacter ePC)
        {
            //イメージタグがある場合、ネットよりダウンロード
            //イメージファイルは複数含めることが可能らしい。
            var imgTag = ePC.Data.Data.FirstOrDefault(i => i.Name == "image")?.Data?.FirstOrDefault(i => i.Name == "imageIdentifier");
            string imagePath = "";
            if (imgTag != null)
            {
                this.UpdateStatus("処理中：画像データ受信");
                imagePath = await GetImageAsync(imgTag);
            }

            //data.xmlを作成
            this.UpdateStatus("処理中：zipデータ作成中");
            string xmlPath = WriteXml(ePC);

            //Zip作成
            var pcName = ePC.Data.Data.FirstOrDefault(i => i.Name == "common")?.Data?.FirstOrDefault(i => i.Name == "name")?.Value;
            pcName += $"({UsingSystem.ToString()})";
            var zip = $@"{ProgramPath}\Export\{pcName}.zip";
            if(MakeZip(zip, imagePath, xmlPath))
            {
                this.UpdateStatus($@"完了：エクスポートしました。(.\Export\{pcName}.zip)");
            }
            else
            {
                this.UpdateStatus($@"エラー：作成に失敗しました。(.\Export\{pcName}.zip)");
            }

            //関数群
            async Task<string> GetImageAsync(UdonaCharacterData imagePC)
            {
                try
                {
                    var imgUrl = imagePC.Value;
                    var imgExt = Path.GetExtension(imgUrl);
                    var imgRes = await _httpClient.GetAsync(imgUrl, HttpCompletionOption.ResponseHeadersRead)
                                            .Timeout(TimeSpan.FromSeconds(10));
                    var imgPath = $@"{ProgramPath}\Data\pic{imgExt}";
                    using (var fs = File.Create(imgPath))
                    using (var hs = await imgRes.Content.ReadAsStreamAsync().Timeout(TimeSpan.FromSeconds(10)))
                    {
                        hs.CopyTo(fs);
                        fs.Flush();
                    }
                    string shash;
                    using (var fs = new FileStream(imgPath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] bhash = SHA256.Create().ComputeHash(fs);
                        shash = BitConverter.ToString(bhash).Replace("-", "").ToLower();
                    }
                    var destPath = $@"{ProgramPath}\Data\{shash}{imgExt}";
                    if (File.Exists(destPath)) File.Delete(destPath);
                    File.Move(imgPath, destPath);
                    imagePC.Value = shash;

                    return destPath;
                }
                catch { return ""; }
            }

            string WriteXml(UdonaCharacter writePC)
            {
                var udonapath = $@"{ProgramPath}\Data\data.xml";

                var xmlnsEmpty = new XmlSerializerNamespaces();
                xmlnsEmpty.Add("", "");

                var serializer = new XmlSerializer(typeof(UdonaCharacter));
                using (var sw = new StreamWriter(udonapath, false, Encoding.UTF8))
                {
                    serializer.Serialize(sw, ePC, xmlnsEmpty);
                    sw.Flush();
                }
                return udonapath;
            }

            bool MakeZip(string zipPath, params string[] filePaths)
            {
                try
                {
                    DirectoryUtils.SafeCreateDirectory(zipPath);
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    using (var z = ZipFile.Open(zipPath, ZipArchiveMode.Update))
                    {
                        foreach (var i in filePaths)
                        {
                            if (String.IsNullOrEmpty(i)) continue;
                            
                            z.CreateEntryFromFile(i, Path.GetFileName(i), CompressionLevel.Optimal);
                            File.Delete(i);
                            
                        }
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        //ステータス表示の更新
        public void UpdateStatus(string message)
        {
            this.Data.Server.ServerInfo.Status = message;
        }

        //起動時、Shift押下でWindowSettings初期化のための判別
        private static bool IsShiftKeyPressed
        {
            get
            {
                return (Keyboard.GetKeyStates(Key.LeftShift) & KeyStates.Down) == KeyStates.Down ||
                     (Keyboard.GetKeyStates(Key.RightShift) & KeyStates.Down) == KeyStates.Down;
            }
        }

        //上記条件を満たした場合WindowSettings初期化
        private void InitializeWindowSettings()
        {
            this.Data.WindowSetting = new WindowSetting();
        }

        //ServerSync送信
        private async Task SyncServerUpload(bool UseCooling = true)
        {
            //trueでクールタイム中(orクールタイムを使わない)、falseでフリー
            var isCoolingUpdate = UseCooling && this.IsCoolingUpload();

            if (!isCoolingUpdate && this.Data.Server.ServerInfo.IsSync)
            {
                await this.SetDodontoFPCDataAsync(this.Data.PCs.SelectedItem);
            }
        }

        //trueでクールタイム中、falseでフリー、
        //クールタイム中に変更があった場合クールタイム後に一度実行。
        private bool IsCoolingUpload()
        {
            if (_isCoolingUpload)
            {
                //クーリングタイム中に要求があった場合キューを入れておく
                _queueInCooling = true;
                return true;
            }

            _isCoolingUpload = true;

            //非同期的に実行
            Task.Run(() =>
           {
               if (_coolingUploadTimer == null) _coolingUploadTimer = new Timer(_syncUploadInterval);
               _coolingUploadTimer.Elapsed += async (sender, e) => { await OnFinishCooling(sender, e); };
               _coolingUploadTimer.Start();
           });

            return false;

            async Task OnFinishCooling(object sender, EventArgs e)
            {
                _isCoolingUpload = false;
                _coolingUploadTimer.Stop();
                _coolingUploadTimer.Dispose();
                _coolingUploadTimer = null;
                //キューが入っている場合のみ一回だけ最後に実行
                if(_queueInCooling) await this.SyncServerUpload(false);
                _queueInCooling = false;
            }

        }

        //ServerSync受信
        private void OnSyncServerDownload(object sender, EventArgs e)
        {
            this.GetDodontoFPCDataCommand.Execute(null);
        }

        //ServerSync設定変更時にタイマー等の初期化
        private void InitializeSyncServerDownloadTimer()
        {
            if(this.Data.Server.ServerInfo.IsSync)
            {
                _syncDownloadTimer?.Stop();
                _syncDownloadTimer?.Dispose();
                var interval = this.Data.Server.ServerInfo.SyncIntervalSec.InRange(5,3600) * 1000;
                _syncDownloadTimer = new Timer(interval);
                _syncDownloadTimer.Elapsed += this.OnSyncServerDownload;
                _syncDownloadTimer.Start();
            }
            else
            {
                _syncDownloadTimer?.Stop();
                _syncDownloadTimer?.Dispose();
                _syncDownloadTimer = null;
            }

        }

        public void OnSyncChanged(object sender, PropertyChangedEventArgs e)
        {
            string[] target =
            {
                nameof(this.Data.Server.ServerInfo.IsSync),
                nameof(this.Data.Server.ServerInfo.SyncIntervalSec),
            };
            if (target.Contains(e.PropertyName))
                this.InitializeSyncServerDownloadTimer();
        }

#endregion

        #region　イベント

        public void ClearAllEvents()
        {
            this.Data.Server.ServerInfo.PropertyChanged -= this.OnSyncChanged;
        }

        protected void SetAllEvents()
        {
            this.Data.Server.ServerInfo.PropertyChanged += this.OnSyncChanged; //SyncDownloadのため
        }

        public void RefreshAllEvents()
        {
            this.ClearAllEvents();
            this.SetAllEvents();
        }

        #endregion
    }

    public class TrpgManageData<P> : BindableBase where P : TrpgPC
    {
#region コンストラクタ

        public TrpgManageData()
        {
        }

#endregion
        
#region プロパティ

        private bool _isLogging = true;
        public bool IsLogging
        {
            get { return _isLogging; }
            set { this.SetProperty(ref this._isLogging, value); }
        }

        private bool _isSeacretDice = false;
        public bool IsSeacretDice
        {
            get { return _isSeacretDice; }
            set { this.SetProperty(ref this._isSeacretDice, value); }
        }

        private WindowSetting _windowSetting = new WindowSetting();
        public WindowSetting WindowSetting
        {
            get { return _windowSetting; }
            set { this.SetProperty(ref this._windowSetting, value); }
        }

        private DodontoFServer _server = new DodontoFServer();
        public DodontoFServer Server
        {
            get { return _server; }
            set { this.SetProperty(ref this._server, value); }
        }

        private BindableList<P> _pCs = new BindableList<P>();
        public BindableList<P> PCs
        {
            get { return _pCs; }
            set { this.SetProperty(ref this._pCs, value); }
        }

#endregion

#region コマンド

        //全ての補正値をリセット
        public void ResetAllCorrection()
        {
            foreach (var target in this.PCs.Items)
            {
                target.ResetAllCorrection();
            }
        }

#endregion

#region　イベント

        public void RefreshAllEvents()
        {
            foreach (var pc in this.PCs.Items)
            {
                pc.RefreshAllEvents();
            }
        }


#endregion
    }

}

