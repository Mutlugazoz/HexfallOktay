using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    private Transform sprite;
    private TextMesh counterText;
    private MyEventHandler eventHandler;
    private GameManager manager;
    // Start is called before the first frame update
    void Start()
    {
        eventHandler = FindObjectOfType<MyEventHandler>();
        manager = FindObjectOfType<GameManager>();
        sprite = transform.Find("Sprite");
        counterText = sprite.Find("MyHexagonSprite").Find("CounterText").GetComponent<TextMesh>();
        counterText.text = manager.settings.bombStartValue.ToString();

        eventHandler.successfulMove += moveHasBeenMade;
    }

    //Function that triggers when player makes a valid move
    //This function has been subscribed to successfulMove event of eventHandler
    private void moveHasBeenMade() {
        int value;

        //Read counter value
        if(int.TryParse(counterText.text, out value)) {

            //decrement value
            value--;
            counterText.text = value.ToString();

            if(value == 0) {
                eventHandler.triggerPlayerLost();
            }
        }
    }

    private void OnDestroy() {
        //If bomb gets destroyed, unsubscribe from successful move
        eventHandler.successfulMove -= moveHasBeenMade;
    }

    // Update is called once per frame
    void Update()
    {
        //Keep bomb rotation zero
        sprite.eulerAngles = Vector3.zero;
    }
}
