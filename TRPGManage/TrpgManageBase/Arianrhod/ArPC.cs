using HtmlAgilityPack;
using ItoKonnyaku.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ItoKonnyaku.TrpgManage {


    public class ArPC : TrpgPC
    {
        #region コンストラクタ

        public ArPC() : base()
        {
            this.DodontoFPCData = new ArDodontoFPCData();
        }

        #endregion

        #region プロパティ

        private ArRollDices _rollDices = new ArRollDices();
        public ArRollDices RollDices
        {
            get { return _rollDices; }
            set { this.SetProperty(ref this._rollDices, value); }
        }

        private ArCharacterValues _characterValues = new ArCharacterValues();
        public ArCharacterValues CharacterValues
        {
            get { return _characterValues; }
            set { this.SetProperty(ref this._characterValues, value); }
        }

        public override int RecievedData
        {
            get { return base.RecievedData;  }
            set {
                base.RecievedData = value;
                (this.DodontoFPCData as ArDodontoFPCData).Costs.DamageBase = value;
            }
        }

        #endregion

        #region コマンド
        
        public new async static Task<TrpgPC> MakePCFromWebAsync(string url)
        {

            var html = await TrpgPC.GetSheetDataAsync(url, TrpgSystem.Arianrhod);
            if (html == null) return null;

            ArPC result = ParsePC(url, html);

            return result;


        }

        private static ArPC ParsePC(string url, HtmlDocument html)
        {
            var result = new ArPC
            {
                SheetUrl = url
            };
            result.DodontoFPCData.UsingSystem = TrpgSystem.Arianrhod;


            var tables = html.DocumentNode.SelectNodes("//table");
            result.DodontoFPCData.Resources.Items.Clear();

            var cnt = 0;
            foreach (var table in tables)
            {
                var fate = "";
                switch (cnt++)
                {
                    case 0: //能力値・HP・MP
                        {
                            var charaTitles = table.SelectNodes("tbody/tr/th").Select(i => i.InnerText).ToList();
                            var charaValues = table.SelectNodes(@"tbody/tr/td[@class=""sumTD""]/input").Select(i => i.Attributes["value"].Value).ToList();
                            fate = table.SelectNodes(@"tbody/tr/th/input[@id=""V_fate""]")[0].Attributes["value"].Value; //Levelはfateをレベルに

                            //Titlesは1から、Valuesは9から9個（能力値）
                            //*[@id="SL_level"]
                            var titles = new List<string>()
                        {
                            "筋力","器用","敏捷","知力","感知","精神","幸運"
                        };

                            foreach (int i in Enumerable.Range(9, 7))
                            {
                                int.TryParse(charaValues[i], out int sv);

                                result.CharacterValues.AddItem(new BindableValue<int>(sv));

                                var rolldice = new RollDice(TrpgSystem.Arianrhod)
                                {
                                    RollGroup = "能力",
                                    RollName = titles[i - 9],
                                    DiceNumber = 2,
                                    DiceBase = 6,
                                    DiceConst = sv,
                                    Inequality = ">=",
                                    TargetValue = 0
                                };

                                result.RollDices.AddItem(rolldice);
                            }
                            //"HP","MP"
                            foreach (int i in Enumerable.Range(16, 2))
                            {
                                int.TryParse(charaValues[i], out int sv);
                                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(sv)); //HP,/HP
                                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(sv)); //MP,/MP
                            }
                            //"Fate"
                            int.TryParse(fate, out int fateint);
                            result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(fateint));

                            break;
                        }
                    case 1: //ライフパス
                        //未使用
                        break;

                    case 2: //スキル
                    case 3: //一般スキル
                        {
                            var SkillCosts = table.SelectNodes("tbody/tr");

                            foreach (var sCost in SkillCosts)
                            {
                                var sc = sCost.SelectNodes("td")?.Select(i => getSkillName(i)).ToList();

                                if (sc != null)
                                {
                                    var nCv = new ObservableCollection<BindableValue<int>>();
                                    for (var i = 0; i < 5; i++)
                                    {
                                        nCv.Add(new BindableValue<int>(0));
                                    }

                                    int.TryParse(sc[6], out int cost);
                                    nCv[(int)ArResource.MP].Value = -cost;

                                    //0名前　6コスト　8効果
                                    var nsc = new ArCost()
                                    {
                                        CostGroup = "スキル使用",
                                        CostName = sc[0],
                                        Items = nCv,
                                        CostUsage = $"[SL:{sc[1]}/{sc[2]}]{sc[8]}"
                                    };

                                    (result.DodontoFPCData as ArDodontoFPCData)?.Costs.AddItem(nsc);
                                }
                            }
                        }
                        break;

                    case 4: //戦闘・武器・防具
                        {
                            var artsTitlesRaw = table.SelectNodes("tbody/tr/th").Select(i => i.InnerText.Replace(" ", "").Replace("\r\n", "").Replace("判定", "").Replace("力", "")).ToList<string>();
                            var artsValues = table.SelectNodes(@"tbody/tr/td[@class=""sumTD""]/input").Select(i => i.Attributes["value"].Value).ToList<string>();
                            var diceValuesRaw = table.SelectNodes(@"tbody/tr/td/input[contains(@id,'dice')]").Select(i => i.Attributes["value"].Value).ToList<string>();

                            //命中、攻撃がそれぞれ2回、回避1回（ごり押し！）
                            var artsTitles = new List<string>()
                            {
                               "命中(右手)","命中(左手)","攻撃(右手)","攻撃(左手)","回避"
                            };

                            var diceValues = new List<string>()
                            {
                                diceValuesRaw[0], diceValuesRaw[0], diceValuesRaw[1], diceValuesRaw[1], diceValuesRaw[2]
                            };

                            //"命右","命左","攻右","攻左","回避"
                            foreach (int i in Enumerable.Range(9, 5))
                            {
                                int.TryParse(diceValues[i - 9], out int dv);
                                int.TryParse(artsValues[i], out int av);

                                var rolldice = new RollDice(TrpgSystem.Arianrhod)
                                {
                                    RollGroup = "戦闘",
                                    RollName = artsTitles[i - 9],
                                    DiceNumber = dv,
                                    DiceBase = 6,
                                    DiceConst = av,
                                    Inequality = ">=",
                                    TargetValue = 0
                                };

                                result.RollDices.AddItem(rolldice);

                            }

                            //"物防","魔防","行動力","移動力"
                            {
                                var target = result.DodontoFPCData as ArDodontoFPCData;
                                target.Costs.DefPointPhysical = int.TryParse(artsValues[14], out var sv14) ? sv14 : 0;
                                target.Costs.DefPointMagical = int.TryParse(artsValues[15], out var sv15) ? sv15 : 0;
                                target.Initiative = int.TryParse(artsValues[16], out var sv16) ? sv16 : 0;
                                //var _                       = int.TryParse(artsValues[18], out var sv18) ? sv18 : 0;
                            }

                            break;
                        }
                    case 5: //装備（価格・重量など）
                        //未使用
                        break;

                    case 6: //所持品・所持金

                        break;

                    case 7: //特殊な判定
                        {
                            var SkillTitles = table.SelectNodes("tbody/tr/th").Select(i => i.InnerText).ToList<string>();
                            var SkillValues = table.SelectNodes(@"tbody/tr/td[@class=""sumTD""]/input").Select(i => getSkillValue(i)).ToList<string>();
                            var SkillDiceValues = table.SelectNodes(@"tbody/tr/td/input[contains(@id,'dice')]").Select(i => getSkillValue(i)).ToList<string>();

                            SkillTitles.RemoveAt(0);

                            //"罠探知","罠解除","危険感知","敵識別","魔術","呪歌","錬金術"
                            foreach (int i in Enumerable.Range(0, 8))
                            {
                                int.TryParse(SkillDiceValues[i], out int dv);
                                int.TryParse(SkillValues[i], out int sv);

                                var rolldice = new RollDice(TrpgSystem.Arianrhod)
                                {
                                    RollGroup = "特殊",
                                    RollName = SkillTitles[i],
                                    DiceNumber = dv,
                                    DiceBase = 6,
                                    DiceConst = sv,
                                    Inequality = ">=",
                                    TargetValue = 0
                                };

                                result.RollDices.AddItem(rolldice);
                            }

                            var addDr = TrpgSystem.Arianrhod.MakeNormalRollDice();
                            addDr.RollGroup = "その他";

                            var addDrNames = new string[] { "アイテムドロップ", "HPポーション", "MPポーション" };

                            foreach (var ad in addDrNames)
                            {
                                addDr.RollName = ad;
                                result.RollDices.AddItem(new RollDice(addDr));
                            }

                            break;
                        }
                    case 8: //レベルアップ記録
                        //未使用
                        break;

                    case 9: //成長履歴
                        //未使用
                        break;

                    case 10: //成長履歴その２
                        //未使用
                        break;

                    case 11: //パーソナルデータ
                        {
                            //名前のみ使用。将来的に他のデータも保持する。
                            var FieldNames = table.SelectNodes("tbody/tr/th").Select(i => i.InnerText.Trim()).ToList<string>();
                            var FieldValues = table.SelectNodes("tbody/tr/td/input").Select(i => i.Attributes["value"].Value).ToList<string>();
                            result.Name = string.IsNullOrEmpty(FieldValues[0]) ? "名無し（パーソナルデータ-キャラクター名に名前を入力！）" : FieldValues[0];
                            break;
                        }
                    case 12: //コネクション
                        //未使用
                        break;
                }
            }

            return result;

            string getSkillValue(HtmlNode node)
            {
                var resSV = "";
                try
                {
                    resSV = node.Attributes["value"].Value;
                }
                catch
                {
                    resSV = "0";
                }
                return resSV;
            }

            string getSkillName(HtmlNode node)
            {
                var resSN = "";
                try
                {
                    resSN = node.SelectSingleNode("input").Attributes["Value"].Value;
                }
                catch
                {
                    resSN = "";
                }
                return resSN;
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
                        CurrentValue = this.DodontoFPCData.Resources[(int)ArResource.HP].Value.ToString(),
                        Value        = this.DodontoFPCData.Resources[(int)ArResource.MaxHP].Value.ToString()
                    },
                    new UdonaCharacterData{Name="MP", Type="numberResource",
                        CurrentValue = this.DodontoFPCData.Resources[(int)ArResource.MP].Value.ToString(),
                        Value        = this.DodontoFPCData.Resources[(int)ArResource.MaxMP].Value.ToString()
                    },
                    new UdonaCharacterData{Name="Fate", Type="numberResource",
                        CurrentValue = this.DodontoFPCData.Resources[(int)ArResource.Fate].Value.ToString(),
                        Value        = "100"
                    }
                }
            };
            result.Data.Add(resource);

            //能力値
            var characterValues = new UdonaCharacterData { Name = "能力値" };
            characterValues.Data = this.CharacterValues.Items.Select((v, i) =>
               new UdonaCharacterData
               {
                   Name = ((ArCharacterValueName)i).Name()+"値",
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
                    Value = $"{i.DiceNumber}D{i.DiceBase}+{i.DiceConst}"
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

        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            RollDices.RefreshAllEvents();
        }

        public override void ResetAllCorrection()
        {
            base.ResetAllCorrection();
            this.RollDices.ResetAllCorrection();
        }

        #endregion

    }

    public class IntelliTrpgManageBaseAr : TrpgManageBase<ArPC> { }
}
