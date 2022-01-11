using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;   //подключает механизм запросов LINQ

//игрок может быть человеком или ИИ
public enum PlayerType
{
    human, ai
}
[System.Serializable]                             //a
public class Player                               //b
{
    public PlayerType type = PlayerType.ai;
    public int playerNum;
    public SlotDef handSlotDef;
    public List<CardBartok> hand;   //карты в руках игрока

    //добавляет карты в руки
    public CardBartok AddCard(CardBartok eCB)
    {
        if (hand == null) hand = new List<CardBartok>();

        //Добавить карту
        hand.Add(eCB);

        //есил это человек, отсортировать карты по достоинству с помощью LINQ
        if (type == PlayerType.human)
        {
            CardBartok[] cards = hand.ToArray();        //a

            //Это вызов LINQ
            cards = cards.OrderBy(cd => cd.rank).ToArray();    //b

            hand = new List<CardBartok>(cards);                 //c

            //примечание: LINQ выполняет операции довольно медленно
            // (затраченная по несколько миллисекунд), но так как
            // мы делаем это один раз за раунд, это не проблема.
        }
        eCB.SetSortingLayerName("10");   //перенести перемещаемою карту в верхний слой //a
        eCB.eventualSortLayer = handSlotDef.layerName;

        FanHand();
        return (eCB);
    }

    //удаляет карту из рук
    public CardBartok RemoveCard(CardBartok cb)
    {
        //Если список hand пуст или не содержит карты cb вернуть null
        if (hand == null || hand.Contains(cb)) return null;
        hand.Remove(cb);
        FanHand();
        return (cb);
    }
    public void FanHand()           //a
    {
        //startRot - угол поворота первой карты относительно оси Z   //b
        float startRot = 0;
        startRot = handSlotDef.rot;
        if(hand.Count > 1)
        {
            startRot += Bartok.S.handFanDegrees * (hand.Count - 1) / 2;
        }

        //переместить все карты в новые позиции 
        Vector3 pos;
        float rot;
        Quaternion rotQ;
        for(int i=0; i<hand.Count; i++)
        {
            rot = startRot - Bartok.S.handFanDegrees * i;
            rotQ = Quaternion.Euler(0, 0, rot);       //c

            pos = Vector3.up * CardBartok.CARD_HEIGHT / 2f;          //d

            pos = rotQ * pos;                                         //e

            //прибавить координаты позиции руки игрока
            //(внизу в центре веера карт)
            pos += handSlotDef.pos;                                     //f
            pos.z = -0.5f * i;                                           //g

            //если это не начальная раздача, начать перемещение карты немедленно
            if(Bartok.S.phase != TurnPhase.idle)      //a
            {
                hand[i].timeStart = 0;
            }

            //установить локальную позицию и поворот i-й карты в руках
            hand[i].MoveTo(pos, rotQ);     //сообщить карте что она начала интерпояцию
            hand[i].state = CBState.toHand;
            //закончив перемещение, карта запищет в поле state значение CBState.hand

            

            //установить локальную позицию и поворот i-й карты в руках
           /* hand[i].transform.localPosition = pos;                     //h
            hand[i].transform.rotation = rotQ;
            hand[i].state = CBState.hand;*/
            hand[i].faceUp = (type == PlayerType.human);                 //i

            //установить SortOrder карт, чтобы обеспечить правильное перекрытие
            hand[i].eventualSortOrder = i * 4;   //b
            //hand[i].SetSortOrder(i * 4);                                //j
        }
    }
    //функция TakeTurn() реализует ИИ для игроков, управляемых компьютером
    public void TakeTurn()
    {
        Utils.tr("Player.TakeTurn");

        //ничего не делать для игрока-человека
        if (type == PlayerType.human) return;

        Bartok.S.phase = TurnPhase.waiting;

        CardBartok cb;

        //если этим игроком управляет компьютер, нужно выбрать карту для хода
        //найти допустимые ходы
        List<CardBartok> validCards = new List<CardBartok>();     //b
        foreach(CardBartok tCB in hand)
        {
            if (Bartok.S.ValidPlay(tCB))
            {
                validCards.Add(tCB);
            }
        }
        //если допустимых ходов нет
        if(validCards.Count == 0)                                //c
        {
            //взять карту
            cb = AddCard(Bartok.S.Draw());
            cb.callbackPlayer = this;                         //e
            return;
        }

        //итак у нас есть одна или несколько карт которыми можно сыграть
        //теперь нужно выбрать одну из них

        cb = validCards[Random.Range(0, validCards.Count)];              //d
        RemoveCard(cb);
        Bartok.S.MoveToTarget(cb);
        cb.callbackPlayer = this;
    }
    public void CBCallback(CardBartok tCB)
    {
        Utils.tr("Player.CBCallback()", tCB.name, "Player" + playerNum);
        //карта завершила перемещение, передать право хода
        Bartok.S.PassTurn();
    }
}


