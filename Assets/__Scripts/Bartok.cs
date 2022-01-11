using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//это перечесление определяет разные этапы в течение одного игрового хода
public enum TurnPhase 
{
    idle, pre, waiting, post, gameOver
}


public class Bartok : MonoBehaviour
{
    static public Bartok S;
    static public Player CURRENT_PLAYER;                                    //a

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public Vector3 layoutCenter = Vector3.zero;
    public float handFanDegrees = 10f;              //a
    public int numStartingCards = 7;
    public float drawTimeStagger = 0.1f;

    [Header("Set Dynamically")]
    public Deck deck;
    public List<CardBartok> drawPile;
    public List<CardBartok> discardPile;
    public List<Player> players;                    //b
    public CardBartok targetCard; 
    public TurnPhase phase = TurnPhase.idle;

    private BartokLayout layout;
    private Transform layoutAnchor;

    void Awake()
    {
        S = this;
    }
    void Start()
    {
        deck = GetComponent<Deck>();   //получить компонент Deck
        deck.InitDeck(deckXML.text);   //передать ему DeckXML
        Deck.Shuffle(ref deck.cards);  //перетасовать колоду              //a

        layout = GetComponent<BartokLayout>();   //получить ссылку на компонент Layout
        layout.ReadLayout(layoutXML.text);       //передать ему LayoutXML

        drawPile = UpgradeCardsList(deck.cards);
        LayoutGame();
    }
    List<CardBartok> UpgradeCardsList(List<Card> lCD)           //a
    {
        List<CardBartok> lCB = new List<CardBartok>();
        foreach(Card tCD in lCD)
        {
            lCB.Add(tCD as CardBartok);
        }
        return (lCB);
    }

    //позиционирует все карты в drawPile
    public void ArrangeDrawPile()
    {
        CardBartok tCB;
        for(int i=0; i<drawPile.Count; i++)
        {
            tCB = drawPile[i];
            tCB.transform.SetParent(layoutAnchor);
            tCB.transform.localPosition = layout.drawPile.pos;

            //угол поворота начинает с 0
            tCB.faceUp = false;
            tCB.SetSortingLayerName(layout.drawPile.layerName);
            tCB.SetSortOrder(-i * 4);   //упорядочить от первых к последним
            tCB.state = CBState.drawpile;
        }
    }

    //выполняет первоначальную раздачу карт в игре
    void LayoutGame()
    {
        //создать пустой GameObject - точку привязки для раскладки    //c
        if(layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");
            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        //позиционировать свободные карты
        ArrangeDrawPile();

        //настроить игроков 
        Player pl;
        players = new List<Player>();
        foreach(SlotDef tSD in layout.slotDefs)
        {
            pl = new Player();
            pl.handSlotDef = tSD;
            players.Add(pl);
            pl.playerNum = tSD.player;
        }
        players[0].type = PlayerType.human;   //0й игрок - человек

        CardBartok tCB;
        //раздать игрокам по 7 карт
        for(int i=0; i<numStartingCards; i++)          
        {
            for(int j=0; j<4; j++)          //a
            {
                tCB = Draw();   //снять карту
                //немного отложить начало перемещения карты
                tCB.timeStart = Time.time + drawTimeStagger * (i * 4 + j);    //b

                players[(j + 1) % 4].AddCard(tCB);   //c
            }
        }
        Invoke("DrawFirstTarget", drawTimeStagger * (numStartingCards * 4 + 4));  //d
    }

    public void DrawFirstTarget()
    {
        //перевернуть первую целевую карту лицевой стороной вверх
        CardBartok tCB = MoveToTarget(Draw());

        //вызвать метод CBCallback сценария Bartok, когда карта закончит перемещение
        tCB.reportFinishTo = this.gameObject;                                                    //b
    }

    //этот обратный вызов используется последней розданной картой в начале игры
    public void CBCallback (CardBartok cb)                                                       //c
    {
        //иногда желательно сообщить о выхзове метода как здесь
        Utils.tr("Bartok:CBCallback()", cb.name);                                                //d
        StartGame();    //начать игру
    }

    public void StartGame()
    {
        //право первого хода приинадлежит игроку слева от человека
        PassTurn(1);
    }

    public void PassTurn(int num = -1)         //f
    {
        //если порядочный номер игрока не указан, выбрать следующего по кругу
        if(num == -1)
        {
            int ndx = players.IndexOf(CURRENT_PLAYER);
            num = (ndx + 1) % 4;
        }
        int lastPlayerNum = -1;
        if(CURRENT_PLAYER != null)
        {
            lastPlayerNum = CURRENT_PLAYER.playerNum;
            //Проверить завершение игры и перетасовать стопку сброшенных карт
            if (CheckGameOver()) { return; }          //a
        }
        CURRENT_PLAYER = players[num];
        phase = TurnPhase.pre;

        CURRENT_PLAYER.TakeTurn();

        //сообщить о передаче хода
        Utils.tr("Bartok:PassTurn()", "Old: " + lastPlayerNum, "New: " + CURRENT_PLAYER.playerNum);
    }

    public bool CheckGameOver()
    {
        //проверить нужно ли перетасовать стопку сброшенных карт и перенести ее в стопку
        //свободных карт
        if(drawPile .Count == 0)
        {
            List<Card> cards = new List<Card>();
            foreach(CardBartok cb in discardPile)
            {
                cards.Add(cb);
            }
            discardPile.Clear();
            Deck.Shuffle(ref cards);
            drawPile = UpgradeCardsList(cards);
            ArrangeDrawPile();
        }

        //проверить победу текущего игрока
        if(CURRENT_PLAYER.hand.Count == 0)
        {
            //игрок, только что сделавший ход - победил!
            phase = TurnPhase.gameOver;
            Invoke("RestartGame", 1);            //b
            return (true);
        }
        return (false);
    }
    public void RestartGame()
    {
        CURRENT_PLAYER = null;
        SceneManager.LoadScene("__Bartok_Scene_0");
    }


    //ValidPlay проверяет возможность сыграть выбранной картой
    public bool ValidPlay(CardBartok cb)
    {
        //Картой можно сыграть если она имеет такое же достоинство как целевая карта
        if (cb.rank == targetCard.rank) return (true);

        //картой можно сыграть если ее масть совпадает с мастью целевой карты
        if(cb.suit == targetCard.suit)
        {
            return (true);
        }
        //иначе вернуть false
        return (false);
    }



    //делает указанную карту целеой
    public CardBartok MoveToTarget(CardBartok tCB)
    {
        tCB.timeStart = 0;
        tCB.MoveTo(layout.discardPile.pos + Vector3.back);
        tCB.state = CBState.toTarget;
        tCB.faceUp = true;

        tCB.SetSortingLayerName("10");
        tCB.eventualSortLayer = layout.target.layerName;
        if(targetCard != null)
        {
            MoveToDiscard(targetCard);
        }

        targetCard = tCB;

        return (tCB);
    }

    public CardBartok MoveToDiscard(CardBartok tCB)
    {
        tCB.state = CBState.discard;
        discardPile.Add(tCB);
        tCB.SetSortingLayerName(layout.discardPile.layerName);
        tCB.SetSortOrder(discardPile.Count * 4);
        tCB.transform.localPosition = layout.discardPile.pos + Vector3.back / 2;

        return (tCB);
    }

    //Функция Draw снимает верхнюю карту со стопки свободных карт и возвращает ее
    public CardBartok Draw()
    {
        CardBartok cd = drawPile[0];  //извлечь нулевую карту

        if (drawPile.Count == 0)   //если список drawPile опустел
        {
            //нужно перетасовать сброшенные карты и переложить их в стопку свободных карт
            int ndx;
            while (discardPile.Count > 0)
            {
                //вынуть случайную карту из стопки сброшенных карт
                ndx = Random.Range(0, discardPile.Count);           //a
                drawPile.Add(discardPile[ndx]);
                discardPile.RemoveAt(ndx);
            }
            ArrangeDrawPile();
            //показать перемещение карт в стопку свободных карт
            float t = Time.time;
            foreach(CardBartok tCB in drawPile)
            {
                tCB.transform.localPosition = layout.discardPile.pos;
                tCB.callbackPlayer = null;
                tCB.MoveTo(layout.drawPile.pos);
                tCB.timeStart = t;
                t += 0.02f;
                tCB.state = CBState.toDrawpile;
                tCB.eventualSortLayer = "0";
            }
        }
        drawPile.RemoveAt(0);         //удалить ее из списка drawPile
        return (cd);                  //и вернуть
    }
    public void CardClicked(CardBartok tCB)
    {
        if (CURRENT_PLAYER.type != PlayerType.human) return;          //a
        if (phase == TurnPhase.waiting) return;                          //b

        switch (tCB.state)                                              //c
        {
            case CBState.drawpile:                                     //d
                //взять верхнюю карту не обязательно ту по которой выполнен щелчок
                CardBartok cb = CURRENT_PLAYER.AddCard(Draw());
                cb.callbackPlayer = CURRENT_PLAYER;
                Utils.tr("Bartok:CardClicked()", "Draw", cb.name);
                phase = TurnPhase.waiting;
                break;

            case CBState.hand:               //e
                //проверить допустимость выбранной карты 
                if (ValidPlay(tCB))
                {
                    CURRENT_PLAYER.RemoveCard(tCB);
                    MoveToTarget(tCB);
                    tCB.callbackPlayer = CURRENT_PLAYER;
                    Utils.tr("Bartok:CardCliked()", "Play", tCB.name, targetCard.name + " is target");          //f
                    phase = TurnPhase.waiting;
                }
                else
                {
                    //игнорировать выбор недопустимой карты, но сообщить о попытке игрока
                    Utils.tr("Bartok:CardClicked()", "Attempted to play", tCB.name, targetCard.name + " is target");     //f
                }
                break;
        }
    }

  /*  //метод Update() временно используется для проверки добавления карты в руки игрока
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            players[0].AddCard(Draw());
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            players[1].AddCard(Draw());
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            players[2].AddCard(Draw());
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            players[3].AddCard(Draw());
        }
    }*/
}
