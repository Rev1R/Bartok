using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;                  //неоходимо для классов ПИ(UI), таких как текст

public class GameOverUI : MonoBehaviour
{
    private Text txt;
    
    void Awake()
    {
        txt = GetComponent<Text>();
        txt.text = "";
    }
    void Update()
    {
        if(Bartok.S.phase != TurnPhase.gameOver)
        {
            txt.text = "";
            return;
        }
        //в эту точку мы попадаем, только когда игра завершилась
        if (Bartok.CURRENT_PLAYER == null) return;                //a
        if(Bartok.CURRENT_PLAYER.type == PlayerType.human)
        {
            txt.text = "You won!";
        }
        else
        {
            txt.text = "Game Over";
        }
    }
}
