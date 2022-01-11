using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//перечисления со всеми возможными событиями начисления очков
public enum eScoreEvent
{
    draw,
    mine,
    mineGold,
    gameWin,
    gameLoss
}
//ScoreManager управляет подсчетом очков
public class ScoreManager : MonoBehaviour   //a
{
    static private ScoreManager S;   //b

    static public int SCORE_FROM_PREV_ROUND = 0;
    static public int HIGH_SCORE = 0;

    [Header("Set Dynamically")]
    //поля для хранения информации о заработаннх очках
    public int chain = 0;
    public int scoreRun = 0;
    public int score = 0;

    void Awake()
    {
        if (S == null)       //c
        {
            S = this;  //подготовка скрытого обьекта-одиночки
        }
        else
        {
            Debug.LogError("ERROR: ScoreManager.Awake(): S is already set!");
        }

        //проверить рекорд в PlayerPrefs
        if (PlayerPrefs.HasKey("ProspectorHighScore"))
        {
            HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");
        }
        //Добавить очки, заработанные в послднем раунде
        //которые должны быть>0 если раунд завершился победой
        score += SCORE_FROM_PREV_ROUND;
        //и сбросить SCORE_FROM_PREV_ROUND 
        SCORE_FROM_PREV_ROUND = 0;
    }
    static public void EVENT(eScoreEvent evt)      //d
    {
        try 
        { //try-catch не позволит ошибке аварийно завершить программу
            S.Event(evt);
        }
        catch (System.NullReferenceException nre)
        {
            Debug.LogError("ScoreManager.EVENT() called while S=null.\n" + nre);
        }
    }
    void Event(eScoreEvent evt)
    {
        switch (evt)
        {
            //в случае победы, проигрыша и завершения хода
            //выполняются одни и те же действия
            case eScoreEvent.draw:   //выбор свободной карты
            case eScoreEvent.gameWin:  //победа в раунде
            case eScoreEvent.gameLoss: //проигрыш в раунде
                chain = 0;   //сбросить цепочку подсчета очков
                score += scoreRun;   //добавить scoreRun к общему подсчету очков
                scoreRun = 0;        //сбросить scoreRun
                break;

            case eScoreEvent.mine:   //удаление карты из основной раскладки
                chain++;              //увеличить количество очков в цепочке
                scoreRun += chain;    //добавить очки за карту
                break;

        }
        //это вторая инструкция switch обрабатывает победу и проигрыш в раунде
        switch (evt) 
        {
            case eScoreEvent.gameWin:
                //в случае победы перенести очки в следующий рауед
                //статические поля НЕ сбрасываются вызовом
                //SceneManager.LoadScene()
                SCORE_FROM_PREV_ROUND = score;
                print("You won this round! Round score: " + score);
                break;

            case eScoreEvent.gameLoss:
                //в случае проигрыша сравнить с рекордом
                if(HIGH_SCORE <= score)
                {
                    print("You got the high score! high score: " + score);
                    HIGH_SCORE = score;
                    PlayerPrefs.SetInt("ProsoectorHighScore", score);
                }
                else
                {
                    print("Your final score for the game was : " + score);
                }
                break;

            default:
                print("score: " + score + " scoreRun:" + scoreRun + " chain:" + chain);
                break;
        }
    }
    static public int CHAIN { get { return S.chain;  } }      //e
    static public int SCORE { get { return S.score; } }
    static public int SCORE_RUN { get { return S.scoreRun; } }
}
