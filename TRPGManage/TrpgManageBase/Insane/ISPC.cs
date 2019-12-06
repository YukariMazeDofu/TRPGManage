using ItoKonnyaku.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItoKonnyaku.TrpgManage
{
    public class ISPC : TrpgPC
    {
        #region コンストラクタ
        
        public ISPC() : base()
        {
            this.DodontoFPCData = new DodontoFPCData();
        }
        #endregion

        #region プロパティ

        private ISRollDices _rollDices = new ISRollDices();
        public ISRollDices RollDices
        {
            get { return _rollDices; }
            set { SetProperty(ref _rollDices, value); }
        }

        private ISAvoidance _avoidance = new ISAvoidance();
        public ISAvoidance Avoidance
        {
            get { return _avoidance; }
            set { SetProperty(ref _avoidance, value); }
        }

        private ISAbilities _abilities = new ISAbilities();
        public ISAbilities Abilities
        {
            get { return _abilities; }
            set { SetProperty(ref _abilities, value); }
        }

        private ISConnections _connections = new ISConnections();
        public ISConnections Connections
        {
            get { return _connections; }
            set { SetProperty(ref _connections, value); }
        }

        private ISItems _items = new ISItems();
        public ISItems Items
        {
            get { return _items; }
            set { SetProperty(ref _items, value); }
        }

        #endregion

        #region コマンド

        public new async static Task<TrpgPC> MakePCFromWebAsync(string url)
        {
            //Web受信はとりあえずあきらめる

            return await Task.Run( () => MakeISPC(url) );

            static ISPC MakeISPC (string name)
            {
                var result = new ISPC
                {
                    Name = name
                };
                result.DodontoFPCData.UsingSystem = TrpgSystem.Insane;
                result.DodontoFPCData.Resources.Items.Clear();

                //HP・SAN
                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(6));
                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(6));
                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(6));
                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(6));
                result.DodontoFPCData.Resources.AddItem(new BindableValue<int>(0));

                //特技・ハイパーごり押しタイム
                var template = new ISRollDice() { UsingSystem = TrpgSystem.Insane, DiceNumber = 2, DiceBase = 6, Inequality = ">=", TargetValue = 15};
                                
                result.RollDices.AddItem(new ISRollDice(template) { X = 1, Y =  2, RollGroup = "暴力", RollName = "焼却" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 1, Y =  3, RollGroup = "暴力", RollName = "拷問" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 1, Y =  4, RollGroup = "暴力", RollName = "緊縛" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 1, Y =  5, RollGroup = "暴力", RollName = "脅す" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 1, Y =  6, RollGroup = "暴力", RollName = "破壊" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 1, Y =  7, RollGroup = "暴力", RollName = "殴打" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 1, Y =  8, RollGroup = "暴力", RollName = "切断" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 1, Y =  9, RollGroup = "暴力", RollName = "刺す" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 1, Y = 10, RollGroup = "暴力", RollName = "射撃" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 1, Y = 11, RollGroup = "暴力", RollName = "戦争" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 1, Y = 12, RollGroup = "暴力", RollName = "埋葬" });

                result.RollDices.AddItem(new ISRollDice(template) { X = 2, Y =  2, RollGroup = "情動", RollName = "恋"   });
                result.RollDices.AddItem(new ISRollDice(template) { X = 2, Y =  3, RollGroup = "情動", RollName = "悦び" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 2, Y =  4, RollGroup = "情動", RollName = "憂い" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 2, Y =  5, RollGroup = "情動", RollName = "恥じらい" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 2, Y =  6, RollGroup = "情動", RollName = "笑い" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 2, Y =  7, RollGroup = "情動", RollName = "我慢" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 2, Y =  8, RollGroup = "情動", RollName = "驚き" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 2, Y =  9, RollGroup = "情動", RollName = "怒り" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 2, Y = 10, RollGroup = "情動", RollName = "恨み" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 2, Y = 11, RollGroup = "情動", RollName = "哀しみ" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 2, Y = 12, RollGroup = "情動", RollName = "愛"   });

                result.RollDices.AddItem(new ISRollDice(template) { X = 3, Y =  2, RollGroup = "知覚", RollName = "痛み" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 3, Y =  3, RollGroup = "知覚", RollName = "官能" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 3, Y =  4, RollGroup = "知覚", RollName = "手触り" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 3, Y =  5, RollGroup = "知覚", RollName = "におい" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 3, Y =  6, RollGroup = "知覚", RollName = "味"   });
                result.RollDices.AddItem(new ISRollDice(template) { X = 3, Y =  7, RollGroup = "知覚", RollName = "物音" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 3, Y =  8, RollGroup = "知覚", RollName = "情景" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 3, Y =  9, RollGroup = "知覚", RollName = "追跡" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 3, Y = 10, RollGroup = "知覚", RollName = "芸術" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 3, Y = 11, RollGroup = "知覚", RollName = "第六感" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 3, Y = 12, RollGroup = "知覚", RollName = "物陰" });

                result.RollDices.AddItem(new ISRollDice(template) { X = 4, Y =  2, RollGroup = "技術", RollName = "分解" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 4, Y =  3, RollGroup = "技術", RollName = "電子機器" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 4, Y =  4, RollGroup = "技術", RollName = "整理" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 4, Y =  5, RollGroup = "技術", RollName = "薬品" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 4, Y =  6, RollGroup = "技術", RollName = "効率" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 4, Y =  7, RollGroup = "技術", RollName = "メディア" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 4, Y =  8, RollGroup = "技術", RollName = "カメラ" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 4, Y =  9, RollGroup = "技術", RollName = "乗り物" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 4, Y = 10, RollGroup = "技術", RollName = "機械" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 4, Y = 11, RollGroup = "技術", RollName = "罠"   });
                result.RollDices.AddItem(new ISRollDice(template) { X = 4, Y = 12, RollGroup = "技術", RollName = "兵器" });

                result.RollDices.AddItem(new ISRollDice(template) { X = 5, Y =  2, RollGroup = "知識", RollName = "物理学" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 5, Y =  3, RollGroup = "知識", RollName = "数学" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 5, Y =  4, RollGroup = "知識", RollName = "化学" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 5, Y =  5, RollGroup = "知識", RollName = "生物学" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 5, Y =  6, RollGroup = "知識", RollName = "医学" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 5, Y =  7, RollGroup = "知識", RollName = "教養" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 5, Y =  8, RollGroup = "知識", RollName = "人類学" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 5, Y =  9, RollGroup = "知識", RollName = "歴史" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 5, Y = 10, RollGroup = "知識", RollName = "民俗学" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 5, Y = 11, RollGroup = "知識", RollName = "考古学" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 5, Y = 12, RollGroup = "知識", RollName = "天文学" });

                result.RollDices.AddItem(new ISRollDice(template) { X = 6, Y =  2, RollGroup = "怪異", RollName = "時間" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 6, Y =  3, RollGroup = "怪異", RollName = "混沌" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 6, Y =  4, RollGroup = "怪異", RollName = "深海" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 6, Y =  5, RollGroup = "怪異", RollName = "死"   });
                result.RollDices.AddItem(new ISRollDice(template) { X = 6, Y =  6, RollGroup = "怪異", RollName = "霊魂" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 6, Y =  7, RollGroup = "怪異", RollName = "魔術" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 6, Y =  8, RollGroup = "怪異", RollName = "暗黒" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 6, Y =  9, RollGroup = "怪異", RollName = "終末" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 6, Y = 10, RollGroup = "怪異", RollName = "夢"   });
                result.RollDices.AddItem(new ISRollDice(template) { X = 6, Y = 11, RollGroup = "怪異", RollName = "地底" });
                result.RollDices.AddItem(new ISRollDice(template) { X = 6, Y = 12, RollGroup = "怪異", RollName = "宇宙" });

                //アビリティ

                result.Abilities.AddItem(new ISAbility() { Name="基本攻撃", Usage="命中時、ダメージ1D6" });
                result.Abilities.AddItem(new ISAbility() { Name="戦場移動", Usage="次ラウンド開始時、全員再プロット" });
                result.Abilities.AddItem(new ISAbility() { Name="", Usage="" });
                result.Abilities.AddItem(new ISAbility() { Name="", Usage="" });

                //コネクション

                result.Connections.AddItem(new ISConnection() );
                result.Connections.AddItem(new ISConnection() );
                result.Connections.AddItem(new ISConnection() );

                //アイテム

                result.Items.AddItem(new BindableValue<int>(0));
                result.Items.AddItem(new BindableValue<int>(0));
                result.Items.AddItem(new BindableValue<int>(0));

                //チャットパレット
                result.ChatPalettes.AddItem(new BindableValue<string>("1D6 [ ダメージ ]基本攻撃"));

                return result;
            }
        }

        #endregion

        #region イベント
        public override void RefreshAllEvents()
        {
            base.RefreshAllEvents();
            this.RollDices.RefreshAllEvents();
            this.Avoidance.RefreshAllEvents();
            this.Abilities.RefreshAllEvents();
            this.Connections.RefreshAllEvents();

            this.Abilities.SetRollDicesInstance(this.RollDices); 
        }

        public override void ResetAllCorrection()
        {
            base.ResetAllCorrection();
            this.RollDices.ResetAllCorrection();
            this.Abilities.ResetAllCorrection();
        }

        #endregion
    }


    public class IntelliTrpgManageBaseIS : TrpgManageBase<ISPC> { }
}
