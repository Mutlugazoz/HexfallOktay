using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using General;

public class HexagonSpawner : MonoBehaviour
{
    int collumnIndex;
    List<GameObject> myCollumn;
    private bool spawning = false;
    GameManager manager;
    private UIManager uIManager;

    //This script is added to component by AddComponent method so it can't have a constructor with parameters
    //initialize is used as constructor
    public void initialize(int collumnIndex, GameManager manager, List<GameObject> myCollumn) {
        this.collumnIndex = collumnIndex;
        this.manager = manager;
        this.myCollumn = myCollumn;
        uIManager = FindObjectOfType<UIManager>();
    }

    //This function counts empty tile in this hexagons spawner's column
    //If it encounters hexagons after encountering any empty tile, it moves these hexagons down 
    public void checkEmpties() {
        int emptyCount = 0;
        for(int i = 0; i < myCollumn.Count; i++) {
            Hexagon hex = myCollumn[i].GetComponent<Tile>().getHex();
            if(hex == null) {
                emptyCount++;
            } else {
                if(emptyCount > 0) {
                    hex.move(myCollumn[i - emptyCount].transform.position);
                }
            }
        }

        if(emptyCount >= 1){
            spawning = true;
            StartCoroutine(GeneralMonoBehaviour.delay(manager.settings.blockMovementDuration, delegate () {
                StartCoroutine(spawningRoutine(emptyCount));
            }));
        }
    }

    //This function spawns hexagons as many as number of empty blocks in collumn with a delay
    private IEnumerator spawningRoutine (int numOfBlocksToSpawn) {
        GameObject newHex;
        
        //Hexagon spawner asks UIManager if it can spawn a bomb or not
        //If spawn is available then spawn bomb
        //Else spawn hexagon
        if(uIManager.isBombAvailable()) {
            newHex = newHex = Instantiate(manager.settings.bombs[Random.Range(0, manager.settings.hexagons.Count)], transform.position, Quaternion.identity);
        } else {
            newHex = Instantiate(manager.settings.hexagons[Random.Range(0, manager.settings.hexagons.Count)], transform.position, Quaternion.identity);
        }
        
        //Set hexes dimensions according to calculations that has been made at the beginning of game
        newHex.transform.localScale *= manager.settings.blockDimensions.width;

        //This line puts a little area between hexagons
        newHex.transform.Find("Sprite").localScale *= 0.95f;

        //Tell hexagon which tile to go
        newHex.GetComponent<Hexagon>().move(myCollumn[myCollumn.Count - numOfBlocksToSpawn].transform.position);

        //If there are still hexagons to spawn then call spawn routine again
        int remainingBlocks = numOfBlocksToSpawn - 1;
        if(remainingBlocks > 0) {
            yield return new WaitForSeconds(manager.settings.blockMovementDuration * 0.5f);
            StartCoroutine(spawningRoutine(remainingBlocks));
        } else {
            StartCoroutine(GeneralMonoBehaviour.delay(manager.settings.blockMovementDuration, delegate () {
                spawning = false;
                manager.notifyStoppedSpawning();
            }));
        }
    }

    public bool getIfSpawning() {
        return spawning;
    }
}
