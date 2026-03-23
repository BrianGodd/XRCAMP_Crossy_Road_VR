using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEnvController : MonoBehaviour
{
    public VRPlayerController vrPlayer;

    [Tooltip("Name of the physics layer that represents rivers. Objects on this layer will push the player.")]
    public string riverLayerName = "River";
    // cached layer index
    int riverLayer = -1;

    public bool isOnRiver = false;

    [Tooltip("Default horizontal river flow speed (world X). Use negative values to flow toward -X).")]
    public float riverFlowSpeedX = -2f;

    [Tooltip("Seconds after last river contact to consider the player has left the river.")]
    public float riverExitTimeout = 0.25f;

    // runtime tracking
    float lastRiverContactTime = -999f;

    void Start()
    {
        // try to find the player controller if not set
        if (vrPlayer == null)
        {
            vrPlayer = GetComponent<VRPlayerController>();
            if (vrPlayer == null)
                vrPlayer = GetComponentInChildren<VRPlayerController>();
        }

        // cache the layer index for the named river layer
        riverLayer = LayerMask.NameToLayer(riverLayerName);
        if (riverLayer < 0)
            Debug.LogWarning($"PlayerEnvController: Layer named '{riverLayerName}' was not found. Please create it in the Tags & Layers settings.");
    }

    void Update()
    {
        // detect exit by timeout since CharacterController doesn't provide OnControllerExit
        if (isOnRiver && Time.time - lastRiverContactTime > riverExitTimeout)
        {
            isOnRiver = false;
            if (vrPlayer != null)
                vrPlayer.ExitRiver();
        }
    }

    // CharacterController collisions are reported through OnControllerColliderHit
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit == null || hit.collider == null) return;

        // check by layer index (if configured)
        if (riverLayer >= 0 ? hit.collider.gameObject.layer == riverLayer : false)
        {
            // record contact time
            lastRiverContactTime = Time.time;

            // if not already marked as on-river, enter river state
            if (!isOnRiver)
            {
                isOnRiver = true;
                if (vrPlayer == null)
                    vrPlayer = GetComponent<VRPlayerController>();

                if (vrPlayer != null)
                {
                    // start river override: set horizontal speed to configured flow
                    vrPlayer.EnterRiver(riverFlowSpeedX);
                }
            }
        }
    }
}
