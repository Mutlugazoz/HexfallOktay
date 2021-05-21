using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using General;

public class MyInputManager : MonoBehaviour
{
    public GameObject selectionIndicator;
    private GameManager manager;
    private bool pressing = false;
    private float pressTime;
    private Vector3 tapStartPos;
    private GameObject currentSelectionIndicator;
    private DoOnce rotateDoOnce = new DoOnce();
    private Coroutine rotatePart2;
    private Coroutine rotatePart3;
    private Coroutine rotateEnd;
    private MyEventHandler eventHandler;
    private bool inputEnabled = true;
    // Start is called before the first frame update
    void Start()
    {
        eventHandler = FindObjectOfType<MyEventHandler>();
        manager = FindObjectOfType<GameManager>();
        eventHandler.destruction += cancelSelection;
        eventHandler.destruction += cancelRotation;
        eventHandler.playerLost += fail;
    }

    // Update is called once per frame
    void Update()
    {
        //input enabled is false when game is over
        if(inputEnabled) {

            //If users touches screen in an available moment save touch time and touch location
            if(Input.GetMouseButtonDown(0)) {
                if(isValidPress()) {
                    pressing = true;
                    pressTime = Time.time;
                    tapStartPos = Input.mousePosition;
                }
            }

            //This code checks the time between start and end of touch
            //If value is smaller then threshold then user just tapped
            //Else user tried to rotate selected hexagons
            if(Input.GetMouseButtonUp(0)) {
                if(isValidPress() && pressing) {
                    if(Time.time - pressTime <= manager.settings.tapThreshold) {
                        triggerTap();
                        pressTime = -1;
                    } else {
                        if(currentSelectionIndicator != null && Vector3.Distance(tapStartPos, Input.mousePosition) > Screen.width * manager.settings.rotateThresholdRatio)
                            triggerRotation();
                    }
                }
                pressing = false;
            }

        }
        
    }

    //Cancel selection get called if any matching occurs or user selects a new set of hexagons
    //it is subscribed to destruction event of event handler
    //Selection indicator object has former selected hexaongs under itself in hierarchy
    //Call hexagons' getDeselected method and destroy selection indicator
    private void cancelSelection(int s) {
        if(currentSelectionIndicator != null) {
                        
            List<Hexagon> childHexagones = new List<Hexagon>();
            for(int i = 0; i < currentSelectionIndicator.transform.childCount; i++) {
                Hexagon currentChildHex = currentSelectionIndicator.transform.GetChild(i).GetComponent<Hexagon>();
                if(currentChildHex != null)
                    childHexagones.Add(currentChildHex);
            }

            childHexagones.ForEach(delegate (Hexagon hex) {
                hex.getDeselected();
            });
                        
            DestroyImmediate(currentSelectionIndicator);
        }
    }

    //This function get called if any matching occurs
    //it is subscribed to destruction event of event handler
    private void cancelRotation(int s) {
        if(!rotateDoOnce.isOpen()) {
            eventHandler.triggerSuccessfulMove();
            
            if(rotatePart2 != null)
                StopCoroutine(rotatePart2);
            if(rotatePart3 != null)
                StopCoroutine(rotatePart3);
            if(rotateEnd != null)
                StopCoroutine(rotateEnd);

            rotateDoOnce.reset();
        }
        
    }

    //Get ray of player's touch position
    //Raycast and it hits to a hexagon, call that hexagon's get selected hexagons method
    //If number of selected hexagons is smaller than 3 then it is an invalid selection
    //It it is a valid selection then spawn a selection indicator (a circle with blue outline)
    //and make selected hexagons child of this selection indicator by calling get selected method of hexagon class
    private void triggerTap() {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(tapStartPos);
        if(Physics.Raycast(ray, out hit, Mathf.Infinity, 1)) {
            Hexagon hitHex = hit.collider.transform.parent.GetComponent<Hexagon>();
            if(hitHex != null) {
                List<Transform> returnedHexagons = hitHex.getSelectedHexagons(hit.point);
                
                if(returnedHexagons.Count == 4) {
                    cancelSelection(0);
                    currentSelectionIndicator = Instantiate(
                        selectionIndicator, 
                        new Vector3(
                            returnedHexagons[0].position.x, 
                            returnedHexagons[0].position.y,
                            -2
                            ),
                        Quaternion.identity
                    );
                    currentSelectionIndicator.transform.localScale *= manager.settings.blockDimensions.width * 0.4f;

                    for(int i = 1; i < returnedHexagons.Count; i++) {
                        returnedHexagons[i].GetComponent<Hexagon>().getSelected(currentSelectionIndicator.transform);
                    }
                }
            }
        }
    }

    //Disable input when user fails
    private void fail() {
        inputEnabled = false;
    }

    //If there are spawners that are currently spawning or selected hexagons are currently rotating don't let user tap
    private bool isValidPress() {
        return manager.countSpawningSpawners() == 0 && rotateDoOnce.isOpen();
    }

    //Rotate selection indicator 3 times
    //Decide rotation direction by using Signed angle function
    //If function returns negative value then rotate it counter clockwise
    //else rotate clockwwise
    //Put delays between rotations
    private void triggerRotation() {
        rotateDoOnce.doOnce(delegate () {
            Vector3 selectionPosInScreen = Camera.main.WorldToScreenPoint(currentSelectionIndicator.transform.position);
            selectionPosInScreen = new Vector3(selectionPosInScreen.x, selectionPosInScreen.y, 0);
            
            if(Vector3.SignedAngle(tapStartPos - selectionPosInScreen, Input.mousePosition - selectionPosInScreen, Vector3.forward) < 0) {
                StartCoroutine(rotateRoutine(0, -120, 0));
                
                rotatePart2 = StartCoroutine(GeneralMonoBehaviour.delay(manager.settings.rotationDuration + 0.1f, delegate () {
                    StartCoroutine(rotateRoutine(-120, -240, 0));
                }));

                rotatePart3 = StartCoroutine(GeneralMonoBehaviour.delay(manager.settings.rotationDuration * 2 + 2 * 0.1f, delegate () {
                    StartCoroutine(rotateRoutine(-240, -360, 0));
                }));

                rotateEnd = StartCoroutine(GeneralMonoBehaviour.delay(manager.settings.rotationDuration * 3 + 2 * 0.1f, delegate() {
                    rotateDoOnce.reset();
                }));

            } else {
                StartCoroutine(rotateRoutine(0, 120, 0));
                
                rotatePart2 = StartCoroutine(GeneralMonoBehaviour.delay(manager.settings.rotationDuration + 0.1f, delegate () {
                    StartCoroutine(rotateRoutine(120, 240, 0));
                }));

                rotatePart3 = StartCoroutine(GeneralMonoBehaviour.delay(manager.settings.rotationDuration * 2 + 2 * 0.1f, delegate () {
                    StartCoroutine(rotateRoutine(240, 360, 0));
                }));

                rotateEnd = StartCoroutine(GeneralMonoBehaviour.delay(manager.settings.rotationDuration * 3 + 2 * 0.1f, delegate() {
                    rotateDoOnce.reset();
                }));
            }
        });
    }

    //Routine of single 120 degree rotation
    private IEnumerator rotateRoutine(float startDegree, float endDegree, float progress) {
        yield return 0;
        float updatedProgress = Mathf.Clamp01(progress + Time.deltaTime / manager.settings.rotationDuration);

        if(currentSelectionIndicator != null) {
            currentSelectionIndicator.transform.eulerAngles = new Vector3(0, 0, Mathf.Lerp(startDegree, endDegree, progress));
            if(progress < 1) {
                StartCoroutine(rotateRoutine(startDegree, endDegree, updatedProgress));
            } else {
                manager.checkDoubleBlocks();
            }
        }
    }


}
