using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using ItoKonnyaku.Mvvm;

namespace ItoKonnyaku.TrpgManage
{
    public class CoCPC : TrpgPC
    {
        #region コンストラクタ

        public CoCPC() : base()
        {
            this.DodontoFPCData = new CoCDodontoFPCData();
        }

        #endregion

        #region プロパティ
        

        private CoCRollDices _rollDices = new CoCRollDices();
        public CoCRollDices RollDices
        {
            get { return _rollDices; }
            set { this.SetProperty(ref this._rollDices, value); }
        }

        private CoCCharacterValues _characterValues = new CoCCharacterValues();
        public CoCCharacterValues CharacterValues
        {
            get { return _characterValues; }
            set { this.SetProperty(ref this._characterValues, value); }
        }

        #endregion

        #region コマンド
        
        public new async static Task<TrpgPC> MakePCFromWebAsync(string url)
        {
            var html = await TrpgPC.GetSheetDataAsync(url, TrpgSystem.Cthulhu);
            if (html == null) return null;

            CoCPC result = ParsePC(url, html);

            return result;

        }

        private static CoCPC ParsePC(string url, HtmlDocument html)
        {
            var result = new CoCPC
            {
                SheetUrl = url,
            };
            result.DodontoFPCData.UsingSystem = TrpgSystem.Cthulhu;

            var ArtsTitles = new Dictionary<string, string>()
            {
                {"Table_battle_arts" , "戦闘" },
                {"Table_find_arts"   , "探索" },
                {"Table_act_arts"    , "行動" },
                {"Table_commu_arts"  , "交渉" },
                {"Table_know_arts"   , "知識" },
                {"Table_arms"        , "武具" },
                {"Table_item"        , "所持" }
            };

            var tables = html.DocumentNode.SelectNodes("//table");

            result.DodontoFPCData.Resources.Items.Clear();

            var cnt = 0;
            foreach (var table in tables)
            {
                switch (cnt++)
                {
                    case 0: //生まれ・能力値
                        {
                            ////$Characteristics = @("STR","CON","POW","DEX","APP","SIZ","INT","EDU","HP","MP","初期SAN","アイデア","幸運","知識")
                            ////                        0     1     2     3     4     5     6     7    8    9        10         11     12     13

                            var charaTitlesRaw = table.SelectNodes("tbody/tr/th");
                            var charaValuesRaw = table.SelectNodes(@"tbody/tr/td[@class=""sumTD""]/input");
                            if (charaTitlesRaw == null || charaValuesRaw == null) { break; }

                            var charaTitles = charaTitlesRaw.Select(i => i.InnerText).ToList();
                            var charaValues = charaValuesRaw.Select(i => i.Attributes["value"].Value).ToList<string>();
                            charaTitles.RemoveAt(0);

                            //"STR","CON","POW","DEX","APP","SIZ","INT","EDU"
                            foreach (int i in Enumerable.Range(0, 8))
                            {
                                int.TryParse(charaValues[i], out int sv);
                                result.CharacterValues.AddItem(new BindableValue<int>(sv));
                            }

                            //"HP","MP"
                            foreach (int i in Enumerable.Range(8, 2))
                            {
                                int.TryParse(charaValues[i], out int sv);
                                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(sv)); //HP,/HP
                                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(sv)); //MP,/MP
                            }
                            //"アイデア","幸運","知識"　→　ダイスロールに登録
                            foreach (int i in Enumerable.Range(11, 3))
                            {
                                int.TryParse(charaValues[i], out int sv);
                                var diceRoll = new RollDice(TrpgSystem.Cthulhu)
                                {
                                    RollGroup = "能力",
                                    RollName = charaTitles[i],
                                    DiceNumber = 1,
                                    DiceBase = 100,
                                    DiceConst = 0,
                                    Inequality = "<=",
                                    TargetValue = sv
                                };
                                result.RollDices.AddItem(diceRoll);
                            }
                            //イニシアティブをDEXから取得
                            result.DodontoFPCData.Initiative = result.CharacterValues[3].Value;

                            break;

                        }
                    case 1: //SAN
                        {
                            var cv = table.SelectNodes("tr/td/input").Select(i => i.Attributes["value"].Value).ToList<string>();
                            int.TryParse(cv[0], out int cvsan);
                            result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(cvsan));
                            //var currentSan = cv[0];
                            //var MaxSan     = cv[1];
                            //var FuteiSan   = cv[2];

                            break;

                        }
                    case 2: //技能ポイント

                        //未使用
                        break;


                    case 3: //戦闘技能
                    case 4: //探索技能
                    case 5: //行動技能
                    case 6: //交渉技能
                    case 7: //知識技能
                        {
                            var ArtsTitle = ArtsTitles[table.Id];

                            //技能名、input対応用に別関数
                            var AbilityNames = table.SelectNodes("tr/th").Select(i => getAbilityName(i)).ToList<string>();
                            var Abcnt = AbilityNames.Count() - 1;
                            AbilityNames = AbilityNames.Where((name, index) => Enumerable.Range(8, Abcnt).Contains(index)).ToList();

                            //技能値
                            var AbilityValues = table.SelectNodes(@"tr/td[@class=""sumTD""]/input").Select(i => i.Attributes["value"].Value).ToList<string>();

                            //RollDice作成
                            Abcnt = AbilityValues.Count();

                            foreach (int i in Enumerable.Range(0, Abcnt))
                            {
                                int.TryParse(AbilityValues[i], out int sv);

                                var diceRoll = new RollDice(TrpgSystem.Cthulhu)
                                {
                                    RollGroup = ArtsTitle,
                                    RollName = AbilityNames[i],
                                    DiceNumber = 1,
                                    DiceBase = 100,
                                    DiceConst = 0,
                                    Inequality = "<=",
                                    TargetValue = sv
                                };
                                result.RollDices.AddItem(diceRoll);

                            }

                            break;

                        }
                    case 8: //戦闘・武器・防具

                        //未使用
                        break;


                    case 9: //所持品・所持金

                        //未使用
                        break;


                    case 10: //パーソナルデータ
                        {
                            //名前のみ使用。将来的に他のデータも保持する。
                            var FieldNames = table.SelectNodes("tbody/tr/th").Select(i => i.InnerText.Trim()).ToList<string>();
                            var FieldValues = table.SelectNodes("tbody/tr/td/input").Select(i => i.Attributes["value"].Value).ToList<string>();
                            result.Name = string.IsNullOrEmpty(FieldValues[0]) ? "名無し（パーソナルデータ-キャラクター名に名前を入力！）" : FieldValues[0];
                            break;

                        }
                    case 11: //クトゥルフ神話TRPG

                        //未使用
                        break;
                }
            }

            return result;

            string getAbilityName(HtmlNode node)
            {
                //技能名(〇〇)の")"を消す
                var preName = node.InnerText.Replace(")", "");

                //入力欄がある場合は抽出する。古いキャラシーにはなぜかvalueが設定されていないものもあるため、エラー時は入力ないものと判断。
                HtmlNode inputNode;
                var inputName = "";

                try
                {
                    inputNode = node.SelectSingleNode("input");
                    inputName = inputNode == null ? "" : inputNode.Attributes["value"].Value.Trim();
                }
                catch
                {
                    inputName = "";
                }

                //技能名(〇〇)の場合は")"で閉じるが、〇〇未記入の場合は"("を消す。
                if (!string.IsNullOrEmpty(preName) && !string.IsNullOrEmpty(inputName)) { inputName += ")"; }
                if (string.IsNullOrEmpty(inputName)) { preName = preName.Replace("(", ""); }

                //もともとに入っている括弧が全角でダサいので直す
                preName = preName.Replace("（", "(").Replace("）", ")");
                return preName + inputName;
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
                        CurrentValue = this.DodontoFPCData.Resources[(int)CoCResource.HP].Value.ToString(),
                        Value        = this.DodontoFPCData.Resources[(int)CoCResource.MaxHP].Value.ToString()
                    },
                    new UdonaCharacterData{Name="MP", Type="numberResource",
                        CurrentValue = this.DodontoFPCData.Resources[(int)CoCResource.MP].Value.ToString(),
                        Value        = this.DodontoFPCData.Resources[(int)CoCResource.MaxMP].Value.ToString()
                    },
                    new UdonaCharacterData{Name="SAN", Type="numberResource",
                        CurrentValue = this.DodontoFPCData.Resources[(int)CoCResource.SAN].Value.ToString(),
                        Value        = "100"
                    }
                }
            };
            result.Data.Add(resource);

            //能力値
            var characterValues = new UdonaCharacterData { Name = "能力値" };
            characterValues.Data = this.CharacterValues.Items.Select( (v,i) => 
                new UdonaCharacterData
                {
                    Name = ((CoCCharacterValueName)i).ToString(),
                    Value = v.Value.ToString()
                }
                ).ToList();

            result.Data.Add(characterValues);

            //ダイスロール
            var rolls = this.RollDices.Items.GroupBy(i => i.RollGroup);

            foreach(var group in rolls)
            {
                var abilities = new UdonaCharacterData{ Name = $"{group.Key}判定" };
                abilities.Data = group.Select(i =>
                new UdonaCharacterData
                {
                    Name = i.RollName,
                    Value = i.TargetValue.ToString()
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
                result += $"\r\n//////////////////////////////{group.Key}判定//////////////////////////////\r\n" ;
                result += string.Join("\r\n", group.Select(i => "1D100<={" + i.RollName + "} " + i.RollName));
                
            }

            return result;
        }

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            RollDices.RefreshAllEvents();
            CharacterValues.RefreshAllEvents();
        }

        public override void ResetAllCorrection()
        {
            base.ResetAllCorrection();
            this.RollDices.ResetAllCorrection();
            this.CharacterValues.ResetAllCorrection();
        }

        #endregion

    }

    public class IntelliTrpgManageBaseCoC : TrpgManageBase<CoCPC> { }
}
