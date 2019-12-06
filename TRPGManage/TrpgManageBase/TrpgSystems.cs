using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItoKonnyaku.TrpgManage
{

    public enum TrpgSystem
    {
        //どどんとふダイスボットと表示を合わせること！
        DiceBot, Arianrhod, Cthulhu, DoubleCross, Insane
    }

    public enum DiceBotResource
    {
        HP, MaxHP, MP, MaxMP
    }

    public enum CoCResource
    {
        HP, MaxHP, MP, MaxMP, SAN
    }

    public enum ArResource
    {
        HP, MaxHP, MP, MaxMP, Fate
    }

    public enum XXResource
    {
        HP, MaxHP, Erosion, Roice 
    }

    public enum ISResource
    {
        HP, MaxHP, SAN, MaxSan, Insanity
    }

    public static class TrpgSystemExtention
    {
        public static string[] ResourceNames(this TrpgSystem system)
        {
            var Names = new string[][]
            {
                //DiceBot
                new [] {"HP", "/HP", "MP", "/MP"},

                //Arianrhod
                new [] { "HP", "/HP", "MP", "/MP", "Fate" },

                //Cthulhu
                new [] { "HP", "/HP", "MP", "/MP", "SAN" },

                //DoubleCross
                new [] { "HP", "/HP", "侵蝕率", "ロイス" },

                //InSANe
                new [] { "HP", "/HP", "正気度", "/正気度", "狂気" }
            };

            return Names[(int)system].ToArray();

        }

        public static string ResourceName(this DiceBotResource resource)
        {
            return TrpgSystem.DiceBot.ResourceNames()[(int)resource];
        }

        public static string ResourceName(this CoCResource resource)
        {
            return TrpgSystem.Cthulhu.ResourceNames()[(int)resource];
        }

        public static string ResourceName(this ArResource resource)
        {
            return TrpgSystem.Arianrhod.ResourceNames()[(int)resource];
        }

        public static string ResourceName(this XXResource resource)
        {
            return TrpgSystem.DoubleCross.ResourceNames()[(int)resource];
        }

        public static string ResourceName(this ISResource resource)
        {
            return TrpgSystem.Insane.ResourceNames()[(int)resource];
        }

        public static string SendingTextFormat(this TrpgSystem system, bool isVissibleTargetVaule)
        {
            //      {0}    D    {1}   +    {2}        {3}         {4}       [    {5}    ]   {6}
            // (DiceNumber)D(DiceBase)+(DiceConst)(Inequality)(TargetValue) [(RollGroup)](RollName)
            // AR :  3     D     6    +     8          >=       (目標値)    [    攻撃　 ]　 回避
            // CoC:  1     D    100     (未使用)       <=          80       [    探索　 ]　 目星
            // XX :  13    DX    7    +     4          >=       (目標値)    [　 コンボ　]　獣爪撃
            // IS :  2     D     6      (未使用)       >=           5       [　  暴力 　]　 焼却

            string[,] values =
            {
                {   //DiceBot
                    "{0}D{1}{2:+0;-0;#}{3}{4} [ {5} ] {6}",
                    "{0}D{1}{2:+0;-0;#} [ {5} ] {6}"
                },

                {   //Arianrhod
                    "{0}D{1}{2:+0;-0;#}{3}{4} [ {5} ] {6}",
                    "{0}D{1}{2:+0;-0;#} [ {5} ] {6}"
                },

                {   //Cthulhu
                    //normal(1D100)
                    //"{0}D{1}{3}{4} [ {5} ] {6}",
                    //"{0}D{1} [ {5} ] {6}"
                    //ccb使用
                    "ccb{3}{4} [ {5} ] {6}",
                    "ccb [ {5} ] {6}"
                },

                {   //DoubleCross
                    //DiceBaseをクリティカル値入力欄として使用
                    "{0}DX{1}{2:+0;-0;#}{3}{4} [ {5} ] {6}",
                    "{0}DX{1}{2:+0;-0;#} [ {5} ] {6}"
                },

                {   //InSANe
                    "{0}D{1}{3}{4} [ {5} ] {6}",
                    "{0}D{1} [ {5} ] {6}"
                }
            };
            return values[(int)system, isVissibleTargetVaule ? 0 : 1];
        }

        public static string CheckUrl(this TrpgSystem system)
        {
            //キャラシー保管所で、それぞれのシステムのシートか確認するための文字列
            //この文字列が含まれていれば各システムのキャラシート。
            string[] values =
            {
                "",
                "https://charasheet.vampire-blood.net/ara2_pc_making.html",
                "https://charasheet.vampire-blood.net/coc_pc_making.html",
                "https://charasheet.vampire-blood.net/dx3_pc_making.html",
                ""
            };
            return values[(int)system];
        }

        public static bool IsSelectingByNameOnDiceRollResult(this TrpgSystem system)
        {
            //ダイスロール結果を受信する際に名前でソートするか
            bool[] values =
            {
                false,
                false,
                false,
                true,
                false
            };
            return values[(int)system];
        }

        public static Func<string, Task<TrpgPC>> MakePCFromWebAsync(this TrpgSystem system)
        {
            Func<string, Task<TrpgPC>>[] funcs =
            {
                //DiceBot
                TrpgPC.MakePCFromWebAsync,

                //Arianrhod
                ArPC.MakePCFromWebAsync,

                //Cthulhu
                CoCPC.MakePCFromWebAsync,

                //DoubleCross
                XXPC.MakePCFromWebAsync,

                //InSANe
                ISPC.MakePCFromWebAsync
            };

            return funcs[(int)system];

        }

        public static RollDice MakeNormalRollDice(this TrpgSystem system)
        {
            var result = new RollDice(system);

            switch (system)
            {
                case TrpgSystem.DiceBot:
                default:
                    break;

                case TrpgSystem.Arianrhod:
                    {
                        result.DiceNumber = 2;
                        result.DiceBase = 6;
                        result.Inequality = ">=";
                        break;
                    }
                case TrpgSystem.Cthulhu:
                    {
                        result.DiceNumber = 1;
                        result.DiceBase = 100;
                        result.Inequality = "<=";
                        break;
                    }
                case TrpgSystem.DoubleCross:
                    {
                        result.DiceNumber = 1;
                        result.DiceBase = 10;
                        result.Inequality = ">=";
                        break;
                    }
                case TrpgSystem.Insane:
                    {
                        result.DiceNumber = 2;
                        result.DiceBase = 6;
                        result.Inequality = ">=";
                        break;
                    }
            }

            return result;

        }

    }

    public static class TypeExtention
    {
        public static TrpgSystem GetTrpgSystem(this Type type)
        {
            switch (type.Name)
            {
                case nameof(ArPC):
                    return TrpgSystem.Arianrhod;
                case nameof(CoCPC):
                    return TrpgSystem.Cthulhu;
                case nameof(XXPC):
                    return TrpgSystem.DoubleCross;
                case nameof(ISPC):
                    return TrpgSystem.Insane;
                case nameof(TrpgPC):
                default:
                    return TrpgSystem.DiceBot;

            }
        }

    }


}