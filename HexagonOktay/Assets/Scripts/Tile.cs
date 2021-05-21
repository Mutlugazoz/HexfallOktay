using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{

    //Fire a raycast
    //If it hits to a hexagon than return it
    //If it does not return null, this means this tile is empty and needs a new hexagon
    public Hexagon getHex() {
        RaycastHit hit;
        if(Physics.Raycast(transform.position, new Vector3(0, 0, -1), out hit, Mathf.Infinity, 1)) {
            Hexagon hex = hit.collider.transform.parent.GetComponent<Hexagon>();
            return hex;
        } else {
            return null;
        }
    }
}
