using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoundResultUI : MonoBehaviour
{
    private Text txt;

    void Awake()
    {
        txt = GetComponent<Text>();
        txt.text = "";
    }
    void Update()
    {
        if (Bartok.S.phase != TurnPhase.gameOver)
        {
            txt.text = "";
            return;
        }
        //в эту точку мы попадаем только когда игра завершилась
        Player cP = Bartok.CURRENT_PLAYER;
        if (cP == null || cP.type == PlayerType.human)          //a
        {
            txt.text = "";
        }
        else
        {
            txt.text = "Player " + (cP.playerNum) + "won";
        }
    }
}
