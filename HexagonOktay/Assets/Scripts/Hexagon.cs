using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using General;

public class Hexagon : MonoBehaviour
{   
    public Color hexagonColor;
    private GameManager manager;
    private DoOnce getManagerDoOnce = new DoOnce();
    private SpriteRenderer spriteRenderer;
    // Start is called before the first frame update
    void Start()
    {
        //Set given color to sprite
        spriteRenderer = transform.Find("Sprite/MyHexagonSprite").GetComponent<SpriteRenderer>();
        spriteRenderer.material.SetColor("_Color", hexagonColor);

        //Set given color to debrises that are disabled at the moment
        //get all descendant gameobjects functions returns all the gameobjects under given object
        //The usual approach returns only first layer
        GeneralMonoBehaviour.getAllDescendantGameObjects(transform.Find("Destructor").gameObject)
        .ForEach(delegate (GameObject go) {
            Renderer debrisRenderer = go.GetComponent<Renderer>();
            if(debrisRenderer != null) {
                debrisRenderer.material.EnableKeyword("_EMISSION");
                debrisRenderer.material.SetColor("_EmissionColor", hexagonColor);
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        //Keep sprite angle same even if rotates
        spriteRenderer.transform.eulerAngles = Vector3.zero;
    }

    //Function that starts the vertical movement process of hexagon
    public void move(Vector3 dest) {
        getManagerDoOnce.doOnce(delegate () {
            manager = FindObjectOfType<GameManager>();
        });
        StartCoroutine(moveRoutine(0, Vector3.Scale(transform.position, new Vector3(1, 1, 0)), Vector3.Scale(dest, new Vector3(1, 1, 0))));
    }

    //Keep moving block until movement progress is 1
    IEnumerator moveRoutine(float progress, Vector3 startPos, Vector3 dest) {
        yield return 0;
        float updatedProgress = progress + Time.deltaTime / manager.settings.blockMovementDuration;
        if(progress <= 1) {
            transform.position = Vector3.Lerp(startPos, dest, Mathf.Sin(Mathf.Clamp01(updatedProgress) * Mathf.Deg2Rad * 90));
            StartCoroutine(moveRoutine(updatedProgress, startPos, dest));
        } else {
            //Movement ended
        }
    }

    //This function finds and returns hexagons that is closest to given point by using raycasts
    //Given point is on the current hexagon btw
    public List<Transform> getSelectedHexagons(Vector3 point) {
        List<Transform> listToReturn = new List<Transform>();

        //This vector is rotated by 120 degrees for 3 times 
        //There is a detailed explanation below
        Vector3 v = new Vector3(manager.settings.blockDimensions.width / 2, 0, 0);
        
        //Get the corner holder object
        //This object has a game object on every edge of hexagon
        Transform cornerHolder = transform.Find("CornerHolder");
        float minDist = Mathf.Infinity;
        Transform closest = null;
        int indexOfClosest = -1;

        //This for loop finds the closest edge to given point
        for(int i = 0; i < cornerHolder.childCount; i++) {
            float distance = Vector3.Magnitude(cornerHolder.GetChild(i).position - point);
            if(distance < minDist) {
                minDist = distance;
                closest = cornerHolder.GetChild(i);
                indexOfClosest = i;
            }
        }

        //Then adds it to returning list
        //The rest of the list will be filled with hexagons but first element is the edge that hexagons will rotate around
        listToReturn.Add(closest);

        //Edges are put on hexagon in a certain pattern
        //If index of selected edge is even than v function will be used as it is
        //If not v function will be used by multiplied with -1
        //v function will be rotated around selected edge by 120 degrees and a raycast will be fired at the end of v vector
        //If this raycasts hit hexagons they will be added to returning list
        //Obviously if there is not any hexagon on neighbour tile (it can be destroyed by game manager or the current hexagon is at the edge of map)
        //then returnin list will not contain 3 hexagons and will not be valid
        if(indexOfClosest % 2 == 0) {
            for(int i = 0; i < 3; i++) {
                RaycastHit hit;
                Vector3 rotatedVector = Quaternion.Euler(0, 0, i * 120) * v;

                if(Physics.Raycast(closest.position + rotatedVector + new Vector3(0, 0, -10), new Vector3(0, 0, 1), out hit, Mathf.Infinity, 1)) {
                    Hexagon hex = hit.collider.transform.parent.GetComponent<Hexagon>();

                    if(hex != null) {
                        listToReturn.Add(hex.transform);
                    }
                }
            }
        } else {
            for(int i = 0; i < 3; i++) {
                RaycastHit hit;
                Vector3 rotatedVector = Quaternion.Euler(0, 0, i * 120) * (v * -1);

                if(Physics.Raycast(closest.position + rotatedVector + new Vector3(0, 0, -10), new Vector3(0, 0, 1), out hit, Mathf.Infinity, 1)) {
                    Hexagon hex = hit.collider.transform.parent.GetComponent<Hexagon>();

                    if(hex != null) {
                        listToReturn.Add(hex.transform);
                    }
                }
            }
        }

        return listToReturn;
    }

    //Select this hexagon
    //move this hexagon forward and put outline and parent it with rotating object
    public void getSelected(Transform parent) {
        transform.parent = parent;
        transform.position += new Vector3(0, 0, -1);
        transform.Find("Sprite/Outline").GetComponent<SpriteRenderer>().enabled = true;
    }

    //Do reverse
    public void getDeselected() {
        transform.parent = null;
        transform.Find("Sprite/Outline").GetComponent<SpriteRenderer>().enabled = false;
        transform.position -= new Vector3(0, 0, -1);
    }

    //Disable sprite
    //Disable collider
    //Enable debrises
    //After 0.5f seconds destroy gameobject
    //This destroys debrises too
    public void destroyHex() {
            spriteRenderer.enabled = false;
            Destroy(transform.Find("Collider").gameObject);
            
            GeneralMonoBehaviour.getAllDescendantGameObjects(transform.Find("Destructor").gameObject).ForEach(delegate (GameObject go) {
                Renderer debrisRenderer = go.GetComponent<Renderer>();
                Rigidbody debrisRB = go.GetComponent<Rigidbody>();
                if(debrisRenderer != null) {
                    debrisRenderer.enabled = true;
                }
                if(debrisRB != null) {
                    debrisRB.isKinematic = false;
                    debrisRB.velocity = (go.transform.position - (transform.position)) * 4;
                }
            });
            Transform counter = spriteRenderer.transform.Find("CounterText");
            if(counter != null) {
                Destroy(counter.gameObject);   
            }
            Destroy(gameObject, 0.5f);
            DestroyImmediate(this);
    }

}
