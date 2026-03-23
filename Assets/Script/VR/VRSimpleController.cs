using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRSimpleControler : MonoBehaviour
{
    public Transform trackingOriginTransform; // optional reference to the XR rig's tracking origin (for applying offset if needed)
    public Vector3 trackingOriginOffset = Vector3.zero; // optional offset to apply to the player's position based on tracking origin

    // Start is called before the first frame update
    void Start()
    {
        // apply tracking origin offset if set
        if (trackingOriginOffset != Vector3.zero)
        {
            trackingOriginTransform.position += trackingOriginOffset;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
