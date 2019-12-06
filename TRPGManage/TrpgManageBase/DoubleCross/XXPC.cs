using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using HtmlAgilityPack;
using ItoKonnyaku.Commons.Extensions;
using ItoKonnyaku.Mvvm;
using Prism.Commands;
using Prism.Mvvm;

namespace ItoKonnyaku.TrpgManage
{


    public class XXPC : TrpgPC
    {
        #region コンストラクタ

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public XXPC() : base()
        {
            this.DodontoFPCData = new XXDodontoFPCData();
            this._rollDices = new XXRollDices();
            this._characterValues = new XXCharacterValues();
            this._effects = new XXEffects();
            this._combos = new XXCombos(this.Effects);
            this.SendRollDice = TrpgSystem.DoubleCross.MakeNormalRollDice();
            this.CorrectRollDice = TrpgSystem.DoubleCross.MakeNormalRollDice();
            this.InitializeDelegateCommand();
        }

        #endregion

        #region プロパティ

        private XXRollDices _rollDices;
        public XXRollDices RollDices
        {
            get { return _rollDices; }
            set { this.SetProperty(ref this._rollDices, value); }
        }

        private XXCharacterValues _characterValues;
        public XXCharacterValues CharacterValues
        {
            get { return _characterValues; }
            set { this.SetProperty(ref this._characterValues, value); }
        }

        private XXEffects _effects;
        public XXEffects Effects
        {
            get { return _effects; }
            set { this.SetProperty(ref this._effects, value); }
        }

        private XXCombos _combos;
        public XXCombos Combos
        {
            get { return _combos; }
            set { this.SetProperty(ref this._combos, value); }
        }

        private IXXECRollable _targetECRoll;
        public IXXECRollable TargetECRoll
        {
            get { return _targetECRoll; }
            private set { this.SetProperty(ref this._targetECRoll, value); }
        }

        private IXXECCostable _targetECCost;
        public IXXECCostable TargetECCost
        {
            get { return _targetECCost; }
            private set { this.SetProperty(ref this._targetECCost, value); }
        }

        public bool IsCombo
        {
            get
            {
                return (this.TargetECCost is XXCombo);
            }
            private set { }
        }

        //行為判定

        private RollDice _sendRollDice;
        public RollDice SendRollDice
        {
            get { return _sendRollDice; }
            private set { this.SetProperty(ref this._sendRollDice, value); }
        }

        private RollDice _correctRollDice;
        public RollDice CorrectRollDice
        {
            get { return _correctRollDice; }
            private set { this.SetProperty(ref this._correctRollDice, value); }
        }

        private string _sendingText = "";
        public string SendingText
        {
            get { return _sendingText; }
            private set { this.SetProperty(ref this._sendingText, value); }
        }

        private bool _isUsingLongName = true;
        public bool IsUsingLongName
        {
            get { return _isUsingLongName; }
            set { this.SetProperty(ref this._isUsingLongName, value); }
        }

        //ダメージ算出

        public override int RecievedData
        {
            get { return _recievedData; }
            set {
                this.DamageDice = value / 10 + 1;
                this.SetProperty(ref this._recievedData, value);
            }
        }

        private int _damageDice = 1;
        public int DamageDice
        {
            get { return _damageDice; }
            private set { this.SetProperty(ref this._damageDice, value); }
        }

        private int _correctDamage = 0;
        public int CorrectDamage
        {
            get { return _correctDamage; }
            set { this.SetProperty(ref this._correctDamage, value); }
        }

        private string _sendingDamageDiceText = "";
        public string SendingDamageDiceText
        {
            get { return _sendingDamageDiceText; }
            private set { this.SetProperty(ref this._sendingDamageDiceText, value); }
        }

        //コスト消費

        private string _sendingCostText = "";
        public string SendingCostText
        {
            get { return _sendingCostText; }
            private set { this.SetProperty(ref this._sendingCostText, value); }
        }

        private int _sendingHP = 0;
        public int SendingHP
        {
            get { return _sendingHP; }
            private set { this.SetProperty(ref this._sendingHP, value); }
        }

        private int _sendingErosion = 0;
        public int SendingErosion
        {
            get { return _sendingErosion; }
            private set { this.SetProperty(ref this._sendingErosion, value); }
        }

        #endregion

        #region コマンド

        public new async static Task<TrpgPC> MakePCFromWebAsync(string url)
        {
            var html = await TrpgPC.GetSheetDataAsync(url, TrpgSystem.DoubleCross);
            if (html == null) return null;

            XXPC result = ParsePC(url, html);

            return result;

        }

        private static XXPC ParsePC(string url, HtmlDocument html)
        {
            var result = new XXPC
            {
                SheetUrl = url,
            };
            result.DodontoFPCData.UsingSystem = TrpgSystem.DoubleCross;


            var charaTitles = new List<string>()
            {
                "肉体", "感覚", "精神", "社会", "HP", "侵蝕", "行動", "移動"
            };

            var tables = html.DocumentNode.SelectNodes("//table");

            result.DodontoFPCData.Resources.Items.Clear();

            var cnt = 0;
            var erosion = 0;

            foreach (var table in tables)
            {
                switch (cnt++)
                {
                    case 0: //能力値・HP
                        {
                            //var charaTitles = table.SelectNodes("tbody/tr/th").Select(i => i.InnerText).ToList();
                            var charaValues = table.SelectNodes(@"tbody/tr/td[@class=""sumTD""]/input").Select(i => i.Attributes["value"].Value).ToList();

                            //Physical, Perceptional, Mental, Social
                            foreach (int i in Enumerable.Range(0, 4))
                            {
                                int.TryParse(charaValues[i], out int cv);
                                result.CharacterValues.AddItem(new BindableValue<int>(cv));
                            }

                            //HP, /HP
                            {
                                int.TryParse(charaValues[4], out int cv);
                                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(cv));
                                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(cv));
                            }

                            //侵蝕率,ロイス
                            {
                                int.TryParse(charaValues[5], out int cv);
                                erosion = cv;
                                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(cv));
                                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(7));
                            }


                            //var data = charaValues.Zip(titles, (i, j) => j + ":" + i).ToList()

                            break;
                        }

                    case 1: //ライフパス

                        //未使用
                        break;

                    case 2: //エフェクト
                    case 3: //イージーエフェクト
                        {
                            var Effects = table.SelectNodes("tbody/tr");

                            foreach (var EffectRow in Effects)
                            {
                                //var Memo = EffectRow.SelectSingleNode("th")?.InnerText;
                                var Effect = EffectRow.SelectNodes("td")?.Select(i => getEffectName(i)).ToList();
                                //0エフェクト名 1SL 2タイミング 3判定 4対象 5射程 6コスト 7制限 8効果など

                                if (Effect != null)
                                {
                                    var ae = new XXEffect
                                    {
                                        Name = Effect[0],
                                        Usage = $"[{Effect[2]}]{Effect[8]}"
                                    };

                                    int.TryParse(Effect[1], out var cv1);
                                    ae.EffectLevel = cv1;

                                    int.TryParse(Effect[6], out var cv6);
                                    ae.CostErosion = cv6;

                                    ae.IsMajor = (Effect[2] == "メジャー");

                                    ae.SetErosion(erosion);
                                    result.Effects.AddItem(ae);
                                }


                            }

                            break;
                        }

                    case 4: //技能_初期習得

                        //未使用
                        break;

                    case 5: //技能_各SL
                        {
                            var RollDices = table.SelectNodes("tbody/tr");

                            var rcnt = 0;
                            var addEffects = new int[] { 1, 4 };

                            foreach (var RollDiceRow in RollDices)
                            {
                                var RollTitle = RollDiceRow.SelectSingleNode("th")?.InnerText;
                                var RollDice = RollDiceRow.SelectNodes("td")?.Select(i => getEffectName(i)).ToList();

                                if (RollDice != null)
                                {
                                    //使用能力値index
                                    var cv = rcnt++ / 3;
                                    var rd = TrpgSystem.DoubleCross.MakeNormalRollDice();

                                    //0 SL   (使用しない)
                                    //1 能力 (使用しない)
                                    //2 技能 (使用しない)
                                    //3 判定 (<DiceNumber>r+<DiceConst>)
                                    //4 参考 (技能名の後ろに括弧書きで追加)

                                    var res = Regex.Match(RollDice[3], @"(?<DiceNumber>[0-9]+)r[+-]*(?<DiceConst>[0-9]*)");
                                    rd.RollGroup = charaTitles[cv];
                                    rd.RollName = RollTitle + (!string.IsNullOrEmpty(RollDice[4]) ? $"({RollDice[4]})" : "");

                                    rd.DiceNumber = int.Parse(res.Groups["DiceNumber"].ToString());

                                    int.TryParse(res.Groups["DiceConst"].ToString(), out var dc);
                                    rd.DiceConst = dc;

                                    result.RollDices.AddItem(rd);

                                    //0白兵、3射撃はコンボ作成に必要なのでエフェクトに追加する。
                                    if (addEffects.Contains(rcnt))
                                    {
                                        var ae = new XXEffect()
                                        {
                                            Name = $"{rd.RollName}攻撃({rd.RollGroup})",
                                            Usage = $"[EL:★/メジャー]{rd.RollName}攻撃({rd.RollGroup})"
                                        };
                                        ae.EffectValues[(int)XXEffectValueName.DiceNumber].Value = rd.DiceNumber.ToString();
                                        ae.EffectValues[(int)XXEffectValueName.DiceCritical].Value = rd.DiceBase.ToString();
                                        ae.EffectValues[(int)XXEffectValueName.DiceConst].Value = rd.DiceConst.ToString();

                                        ae.IsMajor = true;

                                        ae.SetErosion(erosion);
                                        result.Effects.AddItem(ae);
                                    }
                                }
                            }

                            //白兵、射撃を上段に移動
                            var ec = result.Effects.Items.Count();
                            result.Effects.MoveAt(ec - 1, "Top");
                            result.Effects.MoveAt(ec - 1, "Top");


                            break;
                        }

                    case 6: //戦闘・武器・防具_武器
                        {
                            break;
                        }

                    case 7: //戦闘・武器・防具_防具
                        {
                            break;
                        }

                    case 8: //所持品・所持金

                        //未使用
                        break;


                    case 9: //ロイス

                        //未使用
                        break;

                    case 10: //成長履歴_取得経験点
                    case 11: //成長履歴_使用経験点

                        //未使用
                        break;

                    case 12: //パーソナルデータ
                        {
                            //名前のみ使用。将来的に他のデータも保持する。
                            //var FieldNames = table.SelectNodes("tbody/tr/th").Select(i => i.InnerText.Trim()).ToList<string>();
                            var FieldValues = table.SelectNodes("tbody/tr/td/input").Select(i => i.Attributes["value"].Value).ToList<string>();
                            result.Name = string.IsNullOrEmpty(FieldValues[0]) ? "名無し（パーソナルデータ-キャラクター名に名前を入力！）" : FieldValues[0];
                            break;

                        }

                    case 13: //

                        //未使用
                        break;
                }
            }

            return result;

            string getEffectName(HtmlNode node)
            {
                var resEN = "";
                try
                {
                    resEN = node.SelectSingleNode("input").Attributes["Value"].Value;
                }
                catch
                {
                    resEN = node.InnerText;
                }
                return resEN;
            }
        }

        protected override UdonaCharacterData MakeUdonaDetailData()
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
                        CurrentValue = this.DodontoFPCData.Resources[(int)XXResource.HP].Value.ToString(),
                        Value        = this.DodontoFPCData.Resources[(int)XXResource.MaxHP].Value.ToString()
                    },
                    new UdonaCharacterData{Name="侵蝕率", Type="numberResource",
                        CurrentValue = this.DodontoFPCData.Resources[(int)XXResource.Erosion].Value.ToString(),
                        Value        = "300"
                    },
                    new UdonaCharacterData{Name="ロイス", Type="numberResource",
                        CurrentValue = this.DodontoFPCData.Resources[(int)XXResource.Roice].Value.ToString(),
                        Value        = "7"
                    }
                }
            };
            result.Data.Add(resource);

            //能力値
            var characterValues = new UdonaCharacterData { Name = "能力値" };
            characterValues.Data = this.CharacterValues.Items.Select((v, i) =>
               new UdonaCharacterData
               {
                   Name = ((XXCharacterValueName)i).Name() + "値",
                   Value = v.Value.ToString()
               }
                ).ToList();

            result.Data.Add(characterValues);

            //ダイスロール
            var rolls = this.RollDices.Items.GroupBy(i => i.RollGroup);

            foreach (var group in rolls)
            {
                var abilities = new UdonaCharacterData { Name = $"{group.Key}判定" };
                abilities.Data = group.Select(i =>
                new UdonaCharacterData
                {
                    Name = i.RollName,
                    Value = $"{i.DiceNumber}DX{i.DiceBase}+{i.DiceConst}"
                }
                ).ToList();

                result.Data.Add(abilities);
            }

            return result;
        }

        protected override string MakeUdonaChatPalette(string addChatPalette)
        {
            var result = base.MakeUdonaChatPalette(addChatPalette);

            //ダイスロール
            var rolls = this.RollDices.Items.GroupBy(i => i.RollGroup);

            foreach (var group in rolls)
            {
                result += $"\r\n//////////////////////////////{group.Key}判定//////////////////////////////\r\n";
                result += string.Join("\r\n", group.Select(i => "{" + i.RollName + "} " + i.RollName));

            }

            return result;
        }

        protected void InitializeDelegateCommand()
        {
            this.ResetAllCorrectionCommand = new DelegateCommand(this.ResetAllCorrection);
        }

        public ICommand ResetAllCorrectionCommand { get; private set; }
        public override void ResetAllCorrection()
        {
            base.ResetAllCorrection();
            this.RollDices.ResetAllCorrection();
            this.CharacterValues.ResetAllCorrection();
            this.CorrectRollDice.Clear();

            this.RecievedData = 0;
        }

        public void SetSendingText()
        {
            if (this.TargetECRoll == null) return;
            this.SetRollSendingText();
            this.SetCostSendingText();
        }

        protected void SetRollSendingText()
        {

            var IsEffect = !this.IsCombo;

            this.SendRollDice.Clear();

            this.SendRollDice.Inequality = ">=";
            this.SendRollDice.RollGroup = IsEffect ? "エフェクト" : "コンボ";
            this.SendRollDice.RollName = this.TargetECRoll.Name;

            if (IsEffect && !this.TargetECRoll.IsMajor)
            {
                this.SendingText = $"※行為不可※[ {this.SendRollDice.RollGroup} ]{this.SendRollDice.RollName}";
                this.SendingDamageDiceText = "※行為不可※";
                return;
            }

            if (this.TargetECRoll.IsLimited)
            {
                this.SendingText = $"※侵蝕値制限({this.DodontoFPCData.Resources[2]}<{this.TargetECRoll.LimitErosion})※[ {this.SendRollDice.RollGroup} ]{this.SendRollDice.RollName}";
                this.SendingDamageDiceText = $"※侵蝕値制限({this.DodontoFPCData.Resources[2]}<{this.TargetECRoll.LimitErosion})※";
                return;
            }

            this.SendRollDice.DiceNumber += this.TargetECRoll.EffectInts[0].Value + this.CorrectRollDice.DiceNumber + (this.DodontoFPCData as XXDodontoFPCData).ErosionDice;
            this.SendRollDice.DiceNumber = this.SendRollDice.DiceNumber.InRange(0, this.SendRollDice.DiceNumber);

            this.SendRollDice.DiceBase += this.TargetECRoll.EffectInts[1].Value + this.CorrectRollDice.DiceBase;
            this.SendRollDice.DiceBase = this.SendRollDice.DiceBase.InRange(2, 11);

            this.SendRollDice.DiceConst += this.TargetECRoll.EffectInts[2].Value + this.CorrectRollDice.DiceConst;

            this.SendRollDice.TargetValue = this.CorrectRollDice.TargetValue;

            var LongName = "";
            if (!IsEffect && this.IsUsingLongName)
            {
                LongName = $"({(this.TargetECRoll as XXCombo).EffectNames})";
            }

            var addDice = $"{(this.CorrectRollDice.DiceNumber + (this.DodontoFPCData as XXDodontoFPCData).ErosionDice):+0D;-0D;#}";
            var addConst = $"{this.CorrectRollDice.DiceConst:+0;-0;#}";
            var addCritical = $"{(this.CorrectRollDice.DiceBase):/CL+0;/CL-0;#}";
            var addEffectLv = $"{((this.DodontoFPCData as XXDodontoFPCData).ErosionEffectLv):/EL+0;/EL-0;#}";

            var addKakko = (!string.IsNullOrEmpty(addDice) ||
                            !string.IsNullOrEmpty(addCritical) ||
                            !string.IsNullOrEmpty(addEffectLv) ||
                            !string.IsNullOrEmpty(addConst));
            var preKakko = addKakko ? "(" : "";
            var postKakko = addKakko ? ")" : "";

            this.SendRollDice.RollName = $"{this.SendRollDice.RollName}{preKakko}{addDice}{addConst}{addCritical}{addEffectLv}{postKakko}{LongName}";

            this.SendingText = this.SendRollDice.Format(this.RollDices.IsVissibleTargetValue);

            var DmgConst = this.TargetECRoll.EffectInts[3].Value + this.CorrectDamage;
            var addDamageConst = $"{DmgConst:+0;-0;#}";
            this.SendingDamageDiceText = $"{this.DamageDice}D10{addDamageConst} [ ダメージ({this.SendRollDice.RollGroup}) ]{this.TargetECRoll.Name}{this.CorrectDamage:(+0);(-0);#}";

        }

        protected void SetCostSendingText()
        {
            var ECText = this.IsCombo ? "C" : "E";
            var MMText = this.TargetECCost.IsMajor ? "メジャー" : "マイナー";
            var LongName = "";
            if (this.IsCombo && this.IsUsingLongName)
            {
                LongName = $"({(this.TargetECRoll as XXCombo).EffectTimingNames})";
            }
            if (this.TargetECCost.IsLimited)
            {
                this.SendingHP = this.DodontoFPCData.Resources[(int)XXResource.HP].Value;
                this.SendingErosion = this.DodontoFPCData.Resources[(int)XXResource.Erosion].Value;
                this.SendingCostText = $"※侵蝕値制限({this.DodontoFPCData.Resources[2]}<{this.TargetECRoll.LimitErosion})※[ コスト({MMText}) ] {this.TargetECCost.Name} {LongName}";
            }
            else
            {
                this.SendingHP = (this.DodontoFPCData.Resources[(int)XXResource.HP].Value - TargetECCost.CostHP).InRange(0, 10000);
                this.SendingErosion = this.DodontoFPCData.Resources[(int)XXResource.Erosion].Value + TargetECCost.CostErosion;
                this.SendingCostText = $"[ コスト({ECText}/{MMText}) ] {this.TargetECCost.Name}{TargetECCost.CostHP:(HP:-0);(HP+0);#}{TargetECCost.CostErosion:(侵:+0);(侵蝕-0);#} {LongName}".Replace(")(", "/");
            }

            this.DodontoFPCData.SendingText = this.SendingCostText;

            (this.DodontoFPCData as XXDodontoFPCData).CorrectedResources[(int)XXResource.HP].Value = this.SendingHP;
            (this.DodontoFPCData as XXDodontoFPCData).CorrectedResources[(int)XXResource.MaxHP].Value = this.DodontoFPCData.Resources[(int)XXResource.MaxHP].Value;
            (this.DodontoFPCData as XXDodontoFPCData).CorrectedResources[(int)XXResource.Erosion].Value = this.SendingErosion;
            (this.DodontoFPCData as XXDodontoFPCData).CorrectedResources[(int)XXResource.Roice].Value = this.DodontoFPCData.Resources[(int)XXResource.Roice].Value;


        }

        #endregion

        #region イベント

        protected void ClearAllEvents()
        {
            //Erosionイベントを渡す
            var target = (this.DodontoFPCData as XXDodontoFPCData);
            target.ErosionChanged -= this.RollDices.OnErosionChanged;
            target.ErosionChanged -= this.Effects.OnErosionChanged;
            target.ErosionChanged -= this.Combos.OnErosionChanged;
            target.ErosionChanged -= this.OnCorrectionChanged;

            //Effectイベントを渡す
            this.Effects.EffectChanged -= this.Combos.OnEffectChanged;

            //Combo,Effectの選択が変わったらTargetECsを変更
            this.Effects.PropertyChanged -= this.OnEffectOrComboPropertyChanged;
            this.Combos.PropertyChanged -= this.OnEffectOrComboPropertyChanged;

            //Correctionが変わったらSendingText修正
            this.CorrectRollDice.PropertyChanged -= this.OnCorrectionChanged;

            //IsUsingLongNameが変わったらSendingText修正
            this.PropertyChanged -= this.OnPropertyChanged;
            this.Combos.ComboChanged -= this.OnPropertyChanged;

            //目標値表示が変わったらSendingText修正
            this.RollDices.PropertyChanged -= this.OnCorrectionChanged;
        }

        protected void SetAllEvents()
        {
            //Erosionイベントを渡す
            var target = (this.DodontoFPCData as XXDodontoFPCData);
            target.ErosionChanged += this.RollDices.OnErosionChanged;
            target.ErosionChanged += this.Effects.OnErosionChanged;
            target.ErosionChanged += this.Combos.OnErosionChanged;
            target.ErosionChanged += this.OnCorrectionChanged;
            

            //Effectイベントを渡す
            this.Effects.EffectChanged += this.Combos.OnEffectChanged;

            //Combo,Effectの選択が変わったらTargetECsを変更
            this.Effects.PropertyChanged += this.OnEffectOrComboPropertyChanged;
            this.Combos.PropertyChanged += this.OnEffectOrComboPropertyChanged;

            //Correctionが変わったらSendingText修正
            this.CorrectRollDice.PropertyChanged += this.OnCorrectionChanged;

            //IsUsingLongNameが変わったらSendingText修正
            this.PropertyChanged += this.OnPropertyChanged;
            this.Combos.ComboChanged += this.OnPropertyChanged;

            //目標値表示が変わったらSendingText修正
            this.RollDices.PropertyChanged += this.OnCorrectionChanged;
        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();

            //Effects参照をCombosに渡す
            this.Combos.SetRefEffects(this.Effects);

            //各種リフレッシュ
            this.RollDices.RefreshAllEvents();
            this.CharacterValues.RefreshAllEvents();
            this.Effects.RefreshAllEvents();
            this.Combos.RefreshAllEvents();

            //自身のリフレッシュ
            this.ClearAllEvents();
            this.SetAllEvents();

            (this.DodontoFPCData as XXDodontoFPCData).OnErosionChanged(this.DodontoFPCData, null);

        }

        public void OnEffectOrComboPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //エフェクトかコンボが変化したときに発火(選択が変わっただけなら対応しない)
            if (e.PropertyName == nameof(XXEffects.SelectedIndex)) return;

            switch (sender)
            {
                case XXEffects target:
                    {
                        this.TargetECRoll = target.SelectedItem as IXXECRollable;
                        this.TargetECCost = target.SelectedItem as IXXECCostable;
                        break;
                    }
                case XXCombos target:
                    {
                        this.TargetECRoll = target.SelectedItem as IXXECRollable;
                        this.TargetECCost = target.SelectedItem as IXXECCostable;
                        break;
                    }
            }

            this.RaisePropertyChanged(nameof(this.IsCombo));
            this.SetSendingText();
        }

        public void OnCorrectionChanged(object sender, PropertyChangedEventArgs e)
        {
            this.SetSendingText();
        }

        public void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var targets = new string[]
            {
                nameof(this.IsUsingLongName),
                nameof(this.RecievedData),
                nameof(this.CorrectDamage),
                nameof(XXCombo.CurrentCostText) //[NeedCheck]
            };
            if (!targets.Contains(e.PropertyName)) return;
            this.SetSendingText();
        }

        #endregion

    }

    public interface IXXECRollable
    {
        string Name { get; }
        BindableValue<int>[] EffectInts { get; }
        int LimitErosion { get; }
        bool IsLimited { get; }
        bool IsMajor { get; set; }
    }

    public interface IXXECCostable
    {
        string Name { get; }
        int CostErosion { get; }
        int CostHP { get; }
        bool IsLimited { get; }
        bool IsMajor { get; }
    }

    public class IntelliTrpgManageBaseXX : TrpgManageBase<XXPC> { }
}
