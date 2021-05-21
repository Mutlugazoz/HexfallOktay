using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Settings 
{
    //Dimensions of grid
    public int numOfHorizontalBlocks;
    public int numOfVerticalBlocks;

    //Width ratio of playable area of screen
    [Range(0, 1)]
    public float playAreaHorizontalRatio;

    //Height ratio of playable area of screen
    [Range(0, 1)]
    public float playAreaVerticalRatio;

    //World height and width of hexagons
    //width = 2 * singleHexEdge
    //height = sqrt(3) * singleHexEdge
    public BlockDimensions blockDimensions;

    //Hexagon prefabs that can be used in game
    public List<GameObject> hexagons;

    //Bomb prefabs that can be used in game
    public List<GameObject> bombs;

    //Vertical movement duration of hexagons
    public float blockMovementDuration;

    //The threshold between tap and hold
    public float tapThreshold;

    //Duration of single 120 degree rotation
    [Range(0, 0.5f)]
    public float rotationDuration;

    //The pixel length of player's fingers must travel minimum to rotate selected hexagons
    [Range(0, 0.75f)]
    public float rotateThresholdRatio;

    //Score per hex
    public float scorePerHex;

    //A bomb is spawned when player score's moves over this variable. Default 1000
    public float bombSpawnThreshold;

    //Start value of bomb's counter
    public int bombStartValue;
}

public struct BlockDimensions {
    public float width;
    public float height;
}