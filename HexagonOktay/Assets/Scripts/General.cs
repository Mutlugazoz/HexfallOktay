using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//The library that i wrote on my own
//I use this in most of my projects

namespace General
{

    /// <summary>
    /// If you want to run a block of code only once in update function or another loop,
    /// then create an instance of this class.
    /// 
    /// Example:
    /// 
    /// DoOnce myDoOnce = new DoOnce();
    /// myDoOnce.doOnce(delegate () {
    ///     //You can write your code that you want to run only once here
    /// });
    /// 
    /// If you want to reset and execute your code one more time then call myDoOnce.reset();
    /// 
    /// </summary>
    public delegate void myDelegate();
    public class DoOnce
    {
        private bool didOnce = false;

        public DoOnce()
        {

        }

        public DoOnce(bool startClosed)
        {
            didOnce = startClosed;
        }

        public void doOnce(myDelegate function)
        {
            if (!didOnce)
            {
                function();
                didOnce = true;
            }
        }

        public void reset()
        {
            didOnce = false;
        }

        public bool isOpen() {
            return !didOnce;
        }
    }

    public class Gate{
        
        private bool open;
        private myDelegate func;
        public Gate(myDelegate func) {
            this.func = func;
        }

        public Gate(myDelegate func, bool startOpen) {
            this.func = func;
            open = startOpen;
        }

        public bool isOpen() {
            return open;
        }

        public void openGate() {
            open = true;
        }

        public void closeGate() {
            open = false;
        }

        public void run() {
            func();
        }

    }

    public class CameraShake : MonoBehaviour
    {
        private float speed = 0.1f;
        private Vector3 shakeBase;
        private Vector3 target;
        private bool targetReached = true;
        public bool shake = false;
        private Vector3 movementStartPos;
        private float progress;
        private float currentAnimDuration = 0;
        private float animStartDuration = 0;
        private float maxSpeed;
        private Vector3 ranges;

        private void Start()
        {
            shakeBase = transform.localPosition;
        }

        private void Update()
        {
            if (shake)
                if (targetReached)
                {
                    float x = shakeBase.x + Random.Range(-ranges.x, ranges.x);
                    float y = shakeBase.y + Random.Range(-ranges.y, ranges.y);
                    float z = shakeBase.y + Random.Range(-ranges.z, ranges.z);
                    target = new Vector3(x, y, z);
                    movementStartPos = transform.localPosition;
                    progress = 0;
                    targetReached = false;
                }
                else
                {
                    progress = Mathf.Clamp(progress + Time.deltaTime / speed, 0, 1);
                    transform.localPosition = Vector3.Lerp(movementStartPos, target, progress);
                    if (progress == 1)
                    {
                        targetReached = true;
                    }
                }
        }

        public void shakeCam(float durationInSeconds, float maxSpeed, Vector3 ranges, Vector3 shakeBase)
        {
            this.shakeBase = shakeBase;
            shake = true;
            currentAnimDuration = durationInSeconds;
            animStartDuration = durationInSeconds;
            this.ranges = ranges;
            this.maxSpeed = maxSpeed;
            StartCoroutine(startCounter());
        }

        private IEnumerator startCounter()
        {
            yield return 0;
            currentAnimDuration = Mathf.Clamp(currentAnimDuration - Time.deltaTime, 0, currentAnimDuration);
            speed = Mathf.Lerp(0, maxSpeed, currentAnimDuration / animStartDuration);
            if (currentAnimDuration != 0)
            {
                StartCoroutine(startCounter());
            }
            else
            {
                shake = false;
            }
        }
    }

    public class FlipFlop
    {
        private bool is_A = true;

        public void flipFlop(myDelegate A, myDelegate B)
        {
            if (is_A)
            {
                A();
            }
            else
            {
                B();
            }
            is_A = !is_A;
        }

        public void reset()
        {
            is_A = true;
        }

        public bool isA()
        {
            return is_A;
        }
    }

    /// <summary>
    /// This class holds some general functions that can be called without creating an instance of this object.
    /// </summary>
    public class GeneralMonoBehaviour : MonoBehaviour
    {

        public static GeneralMonoBehaviour Instance;

        void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Aligns given gameobject to the ground
        /// </summary>
        /// <param name="g_o"> Gameobject you want to align </param>
        /// <param name="layer"> Spesific number of the ground's layer </param>
        public static void keepParallelToGround(GameObject g_o, int layer = 0)
        {
            RaycastHit hit;
            if (Physics.Raycast(g_o.transform.position, Vector3.down, out hit, Mathf.Infinity, 1 << layer))
            {
                g_o.transform.up = hit.normal;
            }
        }

        public static IEnumerator delay(float seconds, myDelegate codeAfterDelay)
        {
            yield return new WaitForSeconds(seconds);
            codeAfterDelay();
        }

        /// <summary>
        /// Aligns given gameobject's y rotation to direction of given velocity.
        /// Leaves x and z rotations as same as before
        /// </summary>
        /// <param name="g_o">Gameobject</param>
        /// <param name="velocity">Velocity</param>
        public static void alignObjectToVelocityDirection(GameObject g_o, Vector3 velocity)
        {
            g_o.transform.rotation = Quaternion.Slerp(
                Quaternion.Euler(g_o.transform.eulerAngles),
                Quaternion.Euler(
                    g_o.transform.eulerAngles.x,
                    Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg,
                    g_o.transform.eulerAngles.z
                ),
                0.25f
            );

        }
        /// <summary>
        /// Returns the distance between two positions without calculating vertical distance.
        /// </summary>
        /// <param name="pos1"></param>
        /// <param name="pos2"></param>
        /// <returns></returns>
        public static float getXZDistance(Vector3 pos1, Vector3 pos2)
        {
            return Vector3.Distance(new Vector3(pos1.x, 0, pos1.z), new Vector3(pos2.x, 0, pos2.z));
        }

        /// <summary>
        /// Returns a list of all descendants of a game object. 
        /// This includes children, grandChildren, children of grandChildren and so on...
        /// </summary>
        /// <param name="parent"> Parent game object </param>
        /// <returns></returns>
        public static List<GameObject> getAllDescendantGameObjects(GameObject parent, bool returnParent = false)
        {
            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in parent.transform)
            {
                if (!children.Contains(child.gameObject))
                {
                    children.Add(child.gameObject);
                }
            }

            List<GameObject> granChildren = new List<GameObject>();
            if (children.Count > 0)
                foreach (GameObject child in children)
                {
                    List<GameObject> childrenOfChild = getAllDescendantGameObjects(child);
                    foreach (GameObject child2 in childrenOfChild)
                    {
                        granChildren.Add(child2);
                    }
                }
            foreach (GameObject grandChild in granChildren)
            {
                children.Add(grandChild);
            }

            if (returnParent)
                children.Add(parent);

            return children;
        }
    }

    /// <summary>
    /// This class tiles a given gameobject
    /// Tiling starts from given gameobject's position
    /// Determine which way to tile and number of tiles with tileDirections variable
    /// </summary>
    [ExecuteInEditMode]
        public class Tiler : MonoBehaviour
    {
        public GameObject go;
        public bool tile = false;
        public Vector3 tileDirections;

        // Update is called once per frame
        void Update()
        {
            if(tile) {
                tile = false;
                Renderer renderer = go.GetComponent<Renderer>();
                if(renderer != null) {
                    tileDirections = new Vector3(Mathf.Floor(tileDirections.x), Mathf.Floor(tileDirections.y), Mathf.Floor(tileDirections.z));
                    GameObject parent = new GameObject();
                    for(int x = 0; x < tileDirections.x; x++) {
                        for(int y = 0; y < tileDirections.y; y++) {
                            for(int z = 0; z < tileDirections.z; z++) {
                                Instantiate(
                                    go, 
                                    go.transform.position + new Vector3(x * renderer.bounds.size.x, y * renderer.bounds.size.y, z * renderer.bounds.size.z) + new Vector3(renderer.bounds.size.x, 0, 0),
                                    Quaternion.identity,
                                    parent.transform
                                    );
                            }
                        }
                    }
                } else {
                    Debug.LogWarning("Root object doesn't has any renderer");
                }
            }
            
            
        }
        
    }

}

