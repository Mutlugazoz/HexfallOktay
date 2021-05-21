using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using General;

public delegate void destructionDelegate(int blockCount);

public class MyEventHandler : MonoBehaviour
{
    private DoOnce lostDoOnce = new DoOnce();
    public event myDelegate successfulMove;
    public event destructionDelegate destruction;
    public event myDelegate playerLost;

    public void triggerSuccessfulMove() {
        successfulMove();
    }

    public void triggerDestruction(int count) {
        destruction(count);
    }

    public void triggerPlayerLost() {
        lostDoOnce.doOnce(delegate () {
            playerLost();
        });
    }
}
