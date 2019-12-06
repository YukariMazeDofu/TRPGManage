using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItoKonnyaku.TrpgManage
{

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false, ElementName = "character")]
    public partial class UdonaCharacter
    {
        /// キャラクターデータ<remarks/>
        [System.Xml.Serialization.XmlElementAttribute("data")]
        public UdonaCharacterData Data { get; set; } = new UdonaCharacterData { Name="character" };

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("chat-palette")]
        public UdonaChatPallette Chatpalette { get; set; } = new UdonaChatPallette();

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("location.name")]
        public string LocationName { get; set; } = "table";

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("location.x")]
        public double LocationX { get; set; } = 0;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("location.y")]
        public double LocationY { get; set; } = 0;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("posZ")]
        public double PosZ { get; set; } = 0;
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class UdonaCharacterData
    {
        [System.Xml.Serialization.XmlElementAttribute("data")]
        public List<UdonaCharacterData> Data { get; set; } = null;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("type")]
        public string Type { get; set; } = null;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("currentValue")]
        public string CurrentValue { get; set; } = null;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("name")]
        public string Name { get; set; } = null;

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value { get; set; } = null;

        public override string ToString() => 
            $"<data name={Name} type={Type} currentValue={CurrentValue}>{Value}</data>";
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class UdonaChatPallette
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute("dicebot")]
        public string DiceBot { get; set; } = "DiceBot";

        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value { get; set; } = "";

        public override string ToString() => 
            $"<chat-palette dicebot={DiceBot}>{Value}</chat-palette>";
    }


}
