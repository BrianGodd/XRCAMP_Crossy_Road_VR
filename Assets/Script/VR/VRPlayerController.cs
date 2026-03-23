using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VRPlayerController : MonoBehaviour
{
    [Header("References")]
    public CharacterController characterController;
    [Tooltip("Optional: assign transforms for left/right controllers if device velocity is not available.")]
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;
    public Transform trackingOriginTransform; // optional reference to the XR rig's tracking origin (for applying offset if needed)
    public Vector3 trackingOriginOffset = Vector3.zero; // optional offset to apply to the player's position based on tracking origin

    [Header("Flap detection settings")]
    public float downVelocityThreshold = -0.8f; // when controller moves downward fast
    public float upVelocityThreshold = 0.8f; // then moves upward fast
    public float maxStrokeTime = 0.5f; // max time between down and up to count as one stroke
    public float syncWindow = 0.3f; // both controllers must stroke within this window
    public float flapCooldown = 1.0f; // seconds between accepted flaps

    [Header("Jump/impulse settings")]
    public float forwardForce = 4f; // forward impulse strength
    public float upwardForce = 1.2f; // small upward lift
    [Tooltip("Optional: reference to the HMD/head transform. If set, the horizontal forward direction for the flap will use this transform's forward.")]
    public Transform headTransform;

    public Transform bodyTransform; // optional reference for body orientation (if different from head)
    [Header("Auto-align Body")]
    [Tooltip("If true, rotate the player's body Y to match the HMD/camera horizontal forward every frame.")]
    public bool autoAlignToHead = true;
    [Tooltip("Degrees per second to rotate the body yaw towards the head direction. Set <=0 for instant alignment.")]
    public float autoAlignSpeed = 720f;
    
    [Header("Ground Detection")]
    [Tooltip("LayerMask used to determine what counts as ground. If none set, falls back to CharacterController.isGrounded.")]
    public LayerMask groundLayer;
    [Tooltip("Offset from the player position to check for ground (usually Vector3.down * some value).")]
    public Vector3 groundCheckOffset = new Vector3(0, -1.0f, 0);
    [Tooltip("Radius used for ground check sphere.")]
    public float groundCheckRadius = 0.2f;

    // internal
    InputDevice leftDevice;
    InputDevice rightDevice;

    struct StrokeState
    {
        public bool sawDown;
        public float downTime;
        public float lastStrokeTime;
    }

    StrokeState leftState;
    StrokeState rightState;

    float lastFlapTime = -999f;

    // Simple applied velocity when flap triggers
    Vector3 appliedVelocity = Vector3.zero;
    public float gravity = -9.81f;
    
    [Header("Fly (energy) settings")]
    [Tooltip("Horizontal fly speed applied when casting energy.")]
    public float flyForwardSpeed = 6f;
    [Tooltip("Upward speed while flying.")]
    public float flyUpwardSpeed = 1.5f;

    [Tooltip("If true, when player's world Y reaches maxFlyHeight during flying, vertical movement will be stopped to maintain that height.")]
    public bool useMaxFlyHeight = false;
    [Tooltip("Maximum world Y height to maintain while flying (used when useMaxFlyHeight is true).")]
    public float maxFlyHeight = 5f;

    // flying state
    bool isFlying = false;
    float flyEndTime = 0f;

    // river override state: when true, X velocity is forced to riverSpeedX
    bool isOnRiverOverride = false;
    float riverSpeedXOverride = 0f;

    bool isDead = false;
    public Animator deadVisualEffect;
    [Header("Death Roll")]
    [Tooltip("Target local Z angle (degrees) to rotate to when dying.")]
    public float deathRollTargetZ = 90f;
    [Tooltip("Seconds to take to rotate the Z angle to the target on death.")]
    public float deathRollDuration = 2f;
    // keep a reference to the running death coroutine so it can be stopped if needed
    Coroutine deathCoroutine;

    void Start()
    {
        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        // try to find devices
        TryInitDevices();

        // apply tracking origin offset if set
        if (trackingOriginOffset != Vector3.zero)
        {
            trackingOriginTransform.position += trackingOriginOffset;
        }
    }

    void TryInitDevices()
    {
        var leftCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
        var rightCharacteristics = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;

        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(leftCharacteristics, devices);
        if (devices.Count > 0)
            leftDevice = devices[0];

        devices.Clear();
        InputDevices.GetDevicesWithCharacteristics(rightCharacteristics, devices);
        if (devices.Count > 0)
            rightDevice = devices[0];
    }

    void Update()
    {
        if(transform.position.x < -20 || transform.position.x > 20)
        {
            Die(Vector3.zero);
        }

        // Auto-align body yaw to head/camera forward if enabled (skip while dead)
        if (autoAlignToHead && !isDead)
        {
            Vector3 headForward = Vector3.forward;
            if (headTransform != null)
                headForward = headTransform.forward;
            else if (Camera.main != null)
                headForward = Camera.main.transform.forward;
            else
                headForward = transform.forward;

            headForward.y = 0f;
            if (headForward.sqrMagnitude < 0.0001f) headForward = transform.forward;

            float desiredYaw = Quaternion.LookRotation(headForward, Vector3.up).eulerAngles.y;

            Transform t = (bodyTransform != null) ? bodyTransform : transform;
            Vector3 e = t.eulerAngles;
            float currentYaw = e.y;

            float newYaw = (autoAlignSpeed <= 0f) ? desiredYaw : Mathf.MoveTowardsAngle(currentYaw, desiredYaw, autoAlignSpeed * Time.deltaTime);
            t.eulerAngles = new Vector3(e.x, newYaw, e.z);
        }

        // if devices are invalid, try to re-init
        if (!leftDevice.isValid || !rightDevice.isValid)
            TryInitDevices();

        // read vertical velocities
        float leftVelY = GetDeviceVerticalVelocity(leftDevice, leftControllerTransform);
        float rightVelY = GetDeviceVerticalVelocity(rightDevice, rightControllerTransform);

        ProcessStroke(ref leftState, leftVelY);
        ProcessStroke(ref rightState, rightVelY);

        // if both have recent strokes, check sync
        if (leftState.lastStrokeTime > 0 && rightState.lastStrokeTime > 0 && !isDead)
        {
            if (Mathf.Abs(leftState.lastStrokeTime - rightState.lastStrokeTime) <= syncWindow)
            {
                if (Time.time - lastFlapTime >= flapCooldown)
                {
                    TriggerFlap();
                    lastFlapTime = Time.time;
                    // reset stroke times so we don't retrigger immediately
                    leftState.lastStrokeTime = -1f;
                    rightState.lastStrokeTime = -1f;
                }
            }
        }

        // apply simple physics to character controller using appliedVelocity
        if (characterController != null)
        {
            // if currently flying due to energy cast, maintain flying until time elapses
            if (isFlying && !isDead)
            {
                if (Time.time >= flyEndTime)
                {
                    isFlying = false;
                }
                else
                {
                    // maintain a steady forward+up velocity while flying
                    Vector3 forward;
                    if (headTransform != null)
                        forward = headTransform.forward;
                    else if (Camera.main != null)
                        forward = Camera.main.transform.forward;
                    else
                        forward = transform.forward;
                    forward.y = 0; forward.Normalize();

                    // if configured, maintain maxFlyHeight by zeroing vertical motion when reached
                    if (useMaxFlyHeight && transform.position.y >= maxFlyHeight)
                    {
                        appliedVelocity = forward * flyForwardSpeed + Vector3.up * 0f;
                    }
                    else
                    {
                        appliedVelocity = forward * flyForwardSpeed + Vector3.up * flyUpwardSpeed;
                    }
                    Vector3 moveFly = appliedVelocity * Time.deltaTime;
                    characterController.Move(moveFly);

                    // skip normal gravity/damping while flying
                    return;
                }
            }


            // if river override is active, force the X velocity to the river speed
            if (isOnRiverOverride)
            {
                appliedVelocity.x = riverSpeedXOverride;
            }

            // gravity
            appliedVelocity.y += gravity * Time.deltaTime;
            Vector3 move = appliedVelocity * Time.deltaTime;
            //Debug.Log($"Applying move: {move}, velocity: {appliedVelocity}");
            characterController.Move(move);

            // damp horizontal velocity over time (don't damp X when on river)
            if (!isOnRiverOverride)
                appliedVelocity.x = Mathf.MoveTowards(appliedVelocity.x, 0, 5f * Time.deltaTime);
            appliedVelocity.z = Mathf.MoveTowards(appliedVelocity.z, 0, 5f * Time.deltaTime);

            // determine grounded either via layer-based check or CharacterController.isGrounded
            bool isGrounded = characterController.isGrounded;

            // when grounded, clear downward vertical velocity so we don't keep falling
            if (isGrounded && appliedVelocity.y < 0)
            {
                if (!isOnRiverOverride)
                {
                    appliedVelocity.x = 0f;
                }
                appliedVelocity.z = 0f;
                appliedVelocity.y = -1f; // small stick to ground
            }
        }
    }

    float GetDeviceVerticalVelocity(InputDevice device, Transform fallback)
    {
        Vector3 vel;
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.deviceVelocity, out vel))
        {
            return vel.y;
        }

        // fallback: approximate from transform delta
        if (fallback != null)
        {
            return GetTransformVerticalVelocity(fallback);
        }

        return 0f;
    }

    // We'll keep a small cache for transform previous positions
    Dictionary<Transform, float> lastPosY = new Dictionary<Transform, float>();

    float GetTransformVerticalVelocity(Transform t)
    {
        if (t == null) return 0f;
        float prev = 0f;
        if (!lastPosY.TryGetValue(t, out prev))
            prev = t.position.y;

        float vel = (t.position.y - prev) / Mathf.Max(0.0001f, Time.deltaTime);
        lastPosY[t] = t.position.y;
        return vel;
    }

    void ProcessStroke(ref StrokeState s, float velY)
    {
        // detect a downward crossing
        if (!s.sawDown)
        {
            if (velY <= downVelocityThreshold)
            {
                s.sawDown = true;
                s.downTime = Time.time;
            }
        }
        else
        {
            // waiting for up
            if (velY >= upVelocityThreshold)
            {
                if (Time.time - s.downTime <= maxStrokeTime)
                {
                    // stroke completed
                    s.lastStrokeTime = Time.time;
                }

                s.sawDown = false; // reset
            }

            // timeout
            if (Time.time - s.downTime > maxStrokeTime)
                s.sawDown = false;
        }
    }

    void TriggerFlap()
    {
        // compute forward direction relative to HMD/player forward (use headTransform if assigned, else Camera.main, else this.transform)
        Vector3 forward;
        if (headTransform != null)
            forward = headTransform.forward;
        else if (Camera.main != null)
            forward = Camera.main.transform.forward;
        else
            forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        appliedVelocity += forward * forwardForce;
        appliedVelocity += Vector3.up * upwardForce;
        //Debug.Log($"TriggerFlap: Applied velocity: {appliedVelocity}");
    }

    /// <summary>
    /// Start a flying state for given duration (seconds). While flying the player will be propelled forward and slightly upward.
    /// </summary>
    /// <param name="duration">flight duration in seconds</param>
    public void CastEnergyFly(float duration)
    {
        if (duration <= 0f) return;
        // set flying state
        isFlying = true;
        flyEndTime = Time.time + duration;

        // initialize appliedVelocity to desired fly speed
        Vector3 forward;
        if (headTransform != null)
            forward = headTransform.forward;
        else if (Camera.main != null)
            forward = Camera.main.transform.forward;
        else
            forward = transform.forward;
        forward.y = 0; forward.Normalize();

        appliedVelocity = forward * flyForwardSpeed + Vector3.up * flyUpwardSpeed;
    }

    /// <summary>
    /// Called by environment code to force the player's horizontal (X) speed to a river flow value.
    /// </summary>
    /// <param name="speedX">Target X speed in world units/sec (can be negative for -X).</param>
    public void EnterRiver(float speedX)
    {
        isOnRiverOverride = true;
        riverSpeedXOverride = speedX;
        // immediately set horizontal velocity to river speed
        appliedVelocity.x = riverSpeedXOverride;
    }

    /// <summary>
    /// Stop forcing the river horizontal speed; resume normal movement behavior.
    /// </summary>
    public void ExitRiver()
    {
        isOnRiverOverride = false;
    }

    // draw some debug info
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (characterController != null)
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1f, 0.1f);

        // draw ground check
        Gizmos.color = Color.yellow;
        Vector3 checkPos = transform.position + groundCheckOffset;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
    }

    void OnValidate()
    {
        if (groundCheckRadius <= 0f) groundCheckRadius = 0.01f;
    }

    public void Die(Vector3 hitDirection)
    {
        if (isDead) return;
        isDead = true;

        // add ragdoll effect by applying a force to the character controller's transform (this is a very simplified approach, for better results consider using a proper ragdoll setup with rigidbodies)
        appliedVelocity = hitDirection.normalized * 5f + Vector3.up * 2f;
        deadVisualEffect.SetTrigger("Die");
        // start rotating the player's local Z angle toward the target over configured duration
        if (deathCoroutine != null)
            StopCoroutine(deathCoroutine);
        deathCoroutine = StartCoroutine(DeathRollCoroutine());
    }

    IEnumerator DeathRollCoroutine()
    {
        float duration = Mathf.Max(0.0001f, deathRollDuration);
        float elapsed = 0f;

        // read starting local Z angle (use localEulerAngles to modify the local Z axis)
        float startZ = transform.localEulerAngles.z;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float z = Mathf.LerpAngle(startZ, deathRollTargetZ, t);
            Vector3 e = transform.localEulerAngles;
            e.z = z;
            transform.localEulerAngles = e;
            yield return null;
        }

        // ensure final angle exact
        Vector3 finalE = transform.localEulerAngles;
        finalE.z = deathRollTargetZ;
        transform.localEulerAngles = finalE;
        deathCoroutine = null;
    }
}
