using ItoKonnyaku.Mvvm;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItoKonnyaku.TrpgManage
{

    public class ISItems : BindableValueList<int>
    {
        #region コンストラクタ
        // 0　…　鎮痛剤
        // 1　…　武器
        // 2　…　お守り

        public ISItems()
        {
            this.AddItem(new BindableValue<int>(0));
            this.AddItem(new BindableValue<int>(0));
            this.AddItem(new BindableValue<int>(0));
        }

        #endregion

    }
}
