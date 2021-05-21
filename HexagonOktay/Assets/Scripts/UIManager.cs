using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using General;

public class UIManager : MonoBehaviour
{
    private GameManager manager;
    private MyInputManager inputManager;

    private Text scoreText;
    private Text moveCounterText;
    private Transform failScreen;
    private float currentBombThreshold;
    private bool bombAvailability = false;
    private MyEventHandler eventHandler;
    private bool failed = false;
    // Start is called before the first frame update
    void Start()
    {
        eventHandler = FindObjectOfType<MyEventHandler>();
        manager = FindObjectOfType<GameManager>();

        eventHandler.destruction += countScore;
        eventHandler.successfulMove += countMoves;
        eventHandler.playerLost += fail;

        scoreText = transform.Find("MainBar").Find("ScoreText").GetComponent<Text>();
        moveCounterText = transform.Find("MainBar").Find("RightSection").Find("MoveCounterText").GetComponent<Text>();

        currentBombThreshold = manager.settings.bombSpawnThreshold;

    }

    //this function gets called whenever a matching occurs
    //It is subscribed to destruction event of event handler
    private void countScore(int blockNum) {
        float score;
        float newScore = 0;
        if(float.TryParse(scoreText.text, out score)) {
            newScore = score + blockNum * manager.settings.scorePerHex;
            scoreText.text = Mathf.FloorToInt(newScore).ToString();
            

            //If old score is below bomb spawn threshold and the new score is over
            //Then let the first hexagon spawner, asks for bomb, spawn bomb
            if(score < currentBombThreshold && newScore >= currentBombThreshold) {
                currentBombThreshold += manager.settings.bombSpawnThreshold;
                bombAvailability = true;
            }
        }
    }

    //Count the moves of player
    //This function is subscribed to successfulMove event of event handler
    private void countMoves() {
        int moveCount = 0;
        if(int.TryParse(moveCounterText.text, out moveCount)) {
            ++moveCount;
            moveCounterText.text = moveCount.ToString();
        }
    }

    //Hexagon spawners call this function
    //If bomb is available let only first hexagon spawner to spawn it
    public bool isBombAvailable() {
        if(bombAvailability) {
            bombAvailability = false;
            return true;
        } else {
            return false;
        }
    }

    //Play fail screen animation
    private void fail() {
        failed = true;
        StartCoroutine(GeneralMonoBehaviour.delay(0, delegate () {
            transform.Find("FailScreen").Find("FinalScore").GetComponent<Text>().text = "SCORE: " + scoreText.text;
            transform.Find("FailScreen").Find("TotalMoves").GetComponent<Text>().text = "TOTAL MOVES: " + moveCounterText.text;
        }));
        GetComponent<Animator>().Play("FailPopUp");
    }

    // Update is called once per frame
    void Update()
    {
        //If player touches screen after fail, then open scene again
        if(failed) {
            if(Input.GetMouseButtonDown(0))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
