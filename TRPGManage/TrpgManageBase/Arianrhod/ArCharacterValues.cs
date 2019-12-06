using ItoKonnyaku.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItoKonnyaku.TrpgManage
{
    public class ArCharacterValues : BindableValueList<int>
    {
        //将来を見据えて
    }
}

public enum ArCharacterValueName
{//  0    1    2    3    4    5    6 
    STR, FAC, DEX, INT, SEN, POW, LUC
}

public static class ArharacterValueNameExtention
{
    public static string Name(this ArCharacterValueName target)
    {
        string[] names = { "筋力", "器用", "敏捷", "知力", "感知", "精神", "幸運" };
        return names[(int)target];
    }
}