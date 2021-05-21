using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using General;

public class GameManager : MonoBehaviour
{
    public Settings settings;

    private float edgeLength;
    private float root_3;
    private Vector3 firstBlockLoc;
    private List<List<GameObject>> tiles = new List<List<GameObject>>();
    private List<HexagonSpawner> spawners = new List<HexagonSpawner>();
    private MyEventHandler eventHandler;
    private bool failed = false;
    
    // Start is called before the first frame update
    void Start()
    {
        eventHandler = FindObjectOfType<MyEventHandler>();
        eventHandler.playerLost += fail;

        calculateBlockDims();
        tile();
        checkEmpties();
    }

    // Update is called once per frame
    void Update()
    {
    }

    
    private void calculateBlockDims() {

        //This function calculates the width / height ratio of hexagon grid
        root_3 = Mathf.Sqrt(3);
        float X = 2 + (settings.numOfHorizontalBlocks - 1) * 1.5f;
        float Y = settings.numOfVerticalBlocks * root_3;
        float inGameRectangleRatio = X / Y;

        //Then with given ratio values calculates width / height ratio of playable area of screen
        float screenRectangleWidth = (Screen.width * settings.playAreaHorizontalRatio);
        float screenRectangleHeight =  (Screen.height * settings.playAreaVerticalRatio);
        float playableScreenRatio = screenRectangleWidth / screenRectangleHeight;

        //Calculates location of most bottom left block by using subtracting dimensions of playable screen area from mobile device's physical dimenions
        firstBlockLoc = Camera.main.ScreenToWorldPoint(new Vector3((Screen.width - screenRectangleWidth) / 2, (Screen.height - screenRectangleHeight) / 2, -Camera.main.transform.position.z));

        //This code here compares grid and screen rectangle ratios to decide which one is wider
        //If grid rectangle is wider then calculates real width of in game rectangle
        //then divides it by first calculated width
        // real width / X
        //If screen rectangle is wider then divides real height of in game rectangle by first calculated height
        if(inGameRectangleRatio > playableScreenRatio) {
            float inGameRectangleWidth = Vector3.Distance(firstBlockLoc, Camera.main.ScreenToWorldPoint(new Vector3(firstBlockLoc.x + screenRectangleWidth, firstBlockLoc.y, -Camera.main.transform.position.z)));
            edgeLength = (inGameRectangleWidth / X);
        } else {
            float inGameRectangleHeight = Vector3.Distance(firstBlockLoc, Camera.main.ScreenToWorldPoint(new Vector3(firstBlockLoc.x, firstBlockLoc.y + screenRectangleHeight, -Camera.main.transform.position.z)));
            edgeLength = inGameRectangleHeight / Y;
        }
        settings.blockDimensions.width = 2 * edgeLength;
        settings.blockDimensions.height = edgeLength * root_3;
    }

    //This function here tiles the main objects of grid
    private void tile() {

        //First calculates the world position of screen's top edge
        //This variable topMostY will be used to decide which y value hexagons should be spawn on
        float topMostY = Camera.main.ScreenToWorldPoint(new Vector3(0, Screen.height, -Camera.main.transform.position.z)).y;
        
        //The code between lines 75-83 spawns a game object over each column of grid
        //Then attaches the Spawner script to it
        for(int i = 0; i < settings.numOfHorizontalBlocks; i++) {
            tiles.Add(new List<GameObject>());

            GameObject newSpawnerGO = new GameObject();
            newSpawnerGO.transform.position = new Vector3(firstBlockLoc.x + edgeLength + i * edgeLength * 1.5f, topMostY + settings.blockDimensions.height * 0.5f, 0);
            newSpawnerGO.name = "Spawner_" + i;
            HexagonSpawner newSpawner = newSpawnerGO.AddComponent<HexagonSpawner>();
            newSpawner.initialize(i, this, tiles[i]);
            spawners.Add(newSpawner);
            
            //Then tiles gameobjects according to collumn number
            //Tiles with an even column number column must be higher than odd ones
            //After spawning code attaches the Tile script
            bool isEven = i % 2 == 0;
            for(int j = 0; j < settings.numOfVerticalBlocks; j ++) {
                GameObject newObject = new GameObject();
                newObject.transform.position = isEven ? 
                    firstBlockLoc + 
                    new Vector3(
                        edgeLength + i * edgeLength * 1.5f, 
                        j * settings.blockDimensions.height + settings.blockDimensions.height, 
                        1
                    ) 
                    : 
                    firstBlockLoc +
                    new Vector3(
                        edgeLength + i * edgeLength * 1.5f,
                        j * settings.blockDimensions.height + settings.blockDimensions.height / 2, 
                        1
                    );
                Tile newTile = newObject.AddComponent<Tile>();
                tiles[i].Add(newObject);
            }
        }
    }

    //Function here starts the process of finding the tiles without any hexagon on them
    //checkEmpties directly function does this too.
    //Difference is checkEmptiesCoroutine function that is called here puts a delay between collumns
    //This delay makes a good looking animation at the beginning of game
    //checkEmptiesDirectly function gets called during the game
    //checkEmpties gets called only at the beginning
    private void checkEmpties() {
        StartCoroutine(checkEmptiesCoroutine(0));
    }

    private void checkEmptiesDirectly() {
        spawners.ForEach(delegate (HexagonSpawner spawner) {
            spawner.checkEmpties();
        });
    }

    private IEnumerator checkEmptiesCoroutine(int i) {
        spawners[i].checkEmpties();
        if(i < spawners.Count - 1) {
            yield return new WaitForSeconds(settings.blockMovementDuration / 2);
            StartCoroutine(checkEmptiesCoroutine(i + 1));
        }
    }

    //Hexagon spawners notifies game manager that they have stopped spawning and game manager should check mathcing blocks again
    public void notifyStoppedSpawning() {
        if(countSpawningSpawners() == 0) {
            checkDoubleBlocks();
        }
    }

    //Gets called after matching blocks get destroyed
    public void notifyDestructionEnd() {
        if(countSpawningSpawners() == 0 && !failed) {
            checkEmptiesDirectly();
        }
    }

    //This function returns number of spawners that are currently spawning
    //Input manager uses this function to decide whether player can play or not
    //Game manager uses to decide if its time to start checking matching blocks
    public int countSpawningSpawners() {
        int count = 0;
        spawners.ForEach(delegate (HexagonSpawner spawner) {
            if(spawner.getIfSpawning())
                count++;
        });

        return count;
    }

    //This function here finds the block pairs that are in same collumn, has same color and one is over on top of other
    //If there is a match on grid with at least 3 blocks 2 of the blocks must be in same column and on top of each other
    //So finding double blocks first is an optimization
    //After finding them, we can check if there is a 3rd block participating this two
    //But my algorithm checks more than 3 block matches 
    public void checkDoubleBlocks() {
        List<List<Coordinates>> doublesInColum = new List<List<Coordinates>>();
        for(int i = 0; i < tiles.Count; i++) {

            for(int j = 1; j < tiles[i].Count; j++) {
                Hexagon currentTile = tiles[i][j].GetComponent<Tile>().getHex();
                Hexagon tileUnder = tiles[i][j - 1].GetComponent<Tile>().getHex();
                if(currentTile != null && tileUnder != null) {
                    if(
                        currentTile.gameObject.tag
                        ==
                        tileUnder.gameObject.tag
                    ) {
                        List<Coordinates> coordinates = new List<Coordinates>();
                        coordinates.Add(new Coordinates() {i = i, j = j});
                        coordinates.Add(new Coordinates() {i = i, j = j - 1});
                        doublesInColum.Add(coordinates);
                    }
                }
                
            }
        }

        //If there is any double blocks found, then send pairs to findMatchingBlocks function
        //findMatchingBlocks function returns a list that has matching blocks in it
        //If list has more than or equal 3 elements these blocks must be destroyed
        //But this function is run for both blocks to see which one returns more that other
        //Meaning it catches the bigger matching set of blocks
        if(doublesInColum.Count >= 1) {
            for(int i = 0; i < doublesInColum.Count; i++) {
                List<Hexagon> hex1 = findMatchingBlocks(doublesInColum[i][0], doublesInColum[i][1]);
                List<Hexagon> hex2 = findMatchingBlocks(doublesInColum[i][1], doublesInColum[i][0]);
                

                if(hex1.Count >= 3 && hex2.Count >= 3) {
                    if(hex1.Count > hex2.Count) {
                        eventHandler.triggerDestruction(hex1.Count);
                        
                        for(int j = 0; j < hex1.Count; j++)
                            hex1[j].destroyHex();

                    } else {

                        eventHandler.triggerDestruction(hex2.Count);
                        for(int j = 0; j < hex2.Count; j++)
                            hex2[j].destroyHex();
                    }
                } else if(hex1.Count >= 3) {
                    eventHandler.triggerDestruction(hex1.Count);
                    for(int j = 0; j < hex1.Count; j++)
                            hex1[j].destroyHex();

                } else if(hex2.Count >= 3) {
                    eventHandler.triggerDestruction(hex2.Count);
                    for(int j = 0; j < hex2.Count; j++)
                            hex2[j].destroyHex();
                }
            }
            notifyDestructionEnd();
        }
    }

    //It would be better if this code is explained by talking but i will try to explain it here

    //If given blocks are on a column with even number there is seperate number of neighboor offsets than odd columns
    //Offset lists are written down below
    //This function regards first block as center block and runs two seperate searchs through its neighbours
    //First is clockwise and second is counter clockwise
    //adds the matching blocks to list that it returns
    private List<Hexagon> findMatchingBlocks(Coordinates cor1, Coordinates cor2) {
        List<Coordinates> evenOffsets = new List<Coordinates> () {
            new Coordinates() {i = 0, j = 1},
            new Coordinates() {i = 1, j = 1},
            new Coordinates() {i = 1, j = 0},
            new Coordinates() {i = 0, j = -1},
            new Coordinates() {i = -1, j = 0},
            new Coordinates() {i = -1, j = 1},
        };

        List<Coordinates> oddOffsets = new List<Coordinates> () {
            new Coordinates() {i = 0, j = 1},
            new Coordinates() {i = 1, j = 0},
            new Coordinates() {i = 1, j = -1},
            new Coordinates() {i = 0, j = -1},
            new Coordinates() {i = -1, j = -1},
            new Coordinates() {i = -1, j = 0},
        };

        List<Hexagon> returnList = new List<Hexagon>();

        Hexagon centerHex = tiles[cor1.i][cor1.j].GetComponent<Tile>().getHex();

        if(centerHex != null) {
            returnList.Add(centerHex);
            
            if(cor1.i % 2 == 0) {
            
                int neighbourIndex = evenOffsets.IndexOf(new Coordinates() { i = 0, j = cor2.j - cor1.j});

                //Clockwise search
                for(int i = neighbourIndex; i < neighbourIndex + 3; i++) {
                    Coordinates newOffsets = new Coordinates() {i = cor1.i + evenOffsets[i].i, j = cor1.j + evenOffsets[i].j};

                    if(newOffsets.i >= 0 && newOffsets.j >= 0 && newOffsets.i < settings.numOfHorizontalBlocks && newOffsets.j < settings.numOfVerticalBlocks) {
                        Hexagon neighbourHex = tiles[newOffsets.i][newOffsets.j].GetComponent<Tile>().getHex();
                        if(neighbourHex != null) 
                            if(neighbourHex.gameObject.tag == centerHex.gameObject.tag) {
                                returnList.Add(neighbourHex);
                            } else 
                                break;
                        else
                            break;
                    } else
                        break;
                }
                    
                
                //Counter clockwise search
                for(int i = (neighbourIndex == 0 ? 5 : neighbourIndex - 1); i > (neighbourIndex == 0 ? 2 : -1); i--) {
                    Coordinates newOffsets = new Coordinates() {i = cor1.i + evenOffsets[i].i, j = cor1.j + evenOffsets[i].j};

                    if(newOffsets.i >= 0 && newOffsets.j >= 0 && newOffsets.i < settings.numOfHorizontalBlocks && newOffsets.j < settings.numOfVerticalBlocks) {
                        Hexagon neighbourHex = tiles[newOffsets.i][newOffsets.j].GetComponent<Tile>().getHex();
                    
                        if(neighbourHex != null)
                            if(neighbourHex.gameObject.tag == centerHex.gameObject.tag) {
                                returnList.Add(neighbourHex);
                            } else 
                                break;
                        else
                            break;
                    } else 
                        break;
                }
                    

            } else {
                int neighbourIndex = oddOffsets.IndexOf(new Coordinates() { i = 0, j = cor2.j - cor1.j});

                //Clockwise search
                for(int i = neighbourIndex; i < neighbourIndex + 3; i++) {
                    Coordinates newOffsets = new Coordinates() {i = cor1.i + oddOffsets[i].i, j = cor1.j + oddOffsets[i].j};

                    if(newOffsets.i >= 0 && newOffsets.j >= 0 && newOffsets.i < settings.numOfHorizontalBlocks && newOffsets.j < settings.numOfVerticalBlocks) {
                        Hexagon neighbourHex = tiles[newOffsets.i][newOffsets.j].GetComponent<Tile>().getHex();

                        if(neighbourHex != null)
                            if(neighbourHex.gameObject.tag == centerHex.gameObject.tag) {
                                returnList.Add(neighbourHex);
                            } else 
                                break;
                        else
                            break;
                    } else 
                        break;
                    
                }
                
                //Counter clockwise search
                for(int i = (neighbourIndex == 0 ? 5 : neighbourIndex - 1); i > (neighbourIndex == 0 ? 2 : -1); i--) {
                    Coordinates newOffsets = new Coordinates() {i = cor1.i + oddOffsets[i].i, j = cor1.j + oddOffsets[i].j};

                    if(newOffsets.i >= 0 && newOffsets.j >= 0 && newOffsets.i < settings.numOfHorizontalBlocks && newOffsets.j < settings.numOfVerticalBlocks) {
                        Hexagon neighbourHex = tiles[newOffsets.i][newOffsets.j].GetComponent<Tile>().getHex();

                        if(neighbourHex != null)
                            if(neighbourHex.gameObject.tag == centerHex.gameObject.tag) {
                                returnList.Add(neighbourHex);
                            } else 
                                break;
                        else
                            break;
                    } else 
                        break;
                    
                }
            }
        }
        
        
        return returnList;
    }

    //This function gets triggered when player fails
    //Destroys all blocks
    private void fail() {
        failed = true;
        for(int i = 0; i < tiles.Count; i++) {
            for(int j = 0; j < tiles[i].Count; j++) {
                Hexagon hex = tiles[i][j].GetComponent<Tile>().getHex();
                if(hex != null) {
                    hex.destroyHex();
                }
            }
        }
    }
}

//struct of coordinates of tiles
public struct Coordinates {
    public int i;
    public int j;
}