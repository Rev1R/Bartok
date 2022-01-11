using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Globalization;

[System.Serializable]  //a
public class SlotDef   //b
{
    public float x;
    public float y;
    public bool faceUp = false;
    public string layerName = "Default";
    public int layerID = 0;
    public int id;
    public List<int> hiddenBy = new List<int>();   //не используется в Bartok
    public float rot;                              //поворот в зависимости от игрока
    public string type = "slot";
    public Vector2 stagger;
    public int player;                              //порядковый номер игрока
    public Vector3 pos;                             //вычисляется на основе х, у и multiplier
}

public class BartokLayout : MonoBehaviour
{
    [Header("Set Dynamically")]
    public PT_XMLReader xmlr;   //так же как Deck имеет PT_XMLReader
    public PT_XMLHashtable xml;  //используется для ускорения доступа к xml
    public Vector2 multiplier;  //смещение в раскладке
    //ссылки на SlotDef
    public List<SlotDef> slotDefs;  //список SlotDef для игроков
    public SlotDef drawPile;
    public SlotDef discardPile;
    public SlotDef target;

    //этот метод вызывается для чтения файла BartokLayoutXML.xml
    public void ReadLayout(string xmlText)
    {
        xmlr = new PT_XMLReader();
        xmlr.Parse(xmlText);    //загрузить XML
        xml = xmlr.xml["xml"][0]; //и определить xml для ускорения доступа к XML

        //прочитать множители определяющие расстояние между картами
        multiplier.x = float.Parse(xml["multiplier"][0].att("x"), CultureInfo.InvariantCulture);
        multiplier.y = float.Parse(xml["multiplier"][0].att("y"), CultureInfo.InvariantCulture);
    

        //прочитать слоты 
        SlotDef tSD;
        // slotsX используется для ускорения доступа к элементам <slot>
        PT_XMLHashList slotsX = xml["slot"];

        for(int i = 0; i<slotsX.Count; i++)
        {
            tSD = new SlotDef();   //создать новый экземпляр SlotDef
            if (slotsX[i].HasAtt("type"))
            {
                //если <slot> имеет атрибут type, прочитать его
                tSD.type = slotsX[i].att("type");
            }
            else
            {
                //иначе определить тип как "slot"; это отдельная карта в ряду
                tSD.type = "slot";
            }
            //преобразовать некоторые атрибуты в числовые значения
            tSD.x = float.Parse(slotsX[i].att("x"),CultureInfo.InvariantCulture);
            tSD.y = float.Parse(slotsX[i].att("y"),CultureInfo.InvariantCulture);
            tSD.pos = new Vector3(tSD.x * multiplier.x, tSD.y * multiplier.y, 0);

            //слои сортировки
            tSD.layerID = int.Parse(slotsX[i].att("layer"));           //a
            tSD.layerName = tSD.layerID.ToString();                     //b

            //прочитать дополнительные атрибуты, опираясь на тип слота
            switch (tSD.type)
            {
                case "slot":
                    //игнорировать слоты с типом "slot"
                    break;
                case "drawpile":                  //c
                    tSD.stagger.x = float.Parse(slotsX[i].att("xstagger"),CultureInfo.InvariantCulture);
                    drawPile = tSD;
                    break;
                case "discardpile":
                    discardPile = tSD;
                    break;
                case "target":
                    target = tSD;
                    break;
                case "hand":                    //d
                    tSD.player = int.Parse(slotsX[i].att("player"));
                    tSD.rot = float.Parse(slotsX[i].att("rot"), CultureInfo.InvariantCulture);
                    slotDefs.Add(tSD);
                    break;
            }
        }
    }
}
