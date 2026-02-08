using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("References")]
    public TrackGenerator trackGenerator;

    [Header("Movement")]
    public float maxSpeed = 100f;
    public float acceleration = 25f;
    public float turnSpeed = 90f;
    public float brakeStrength = 60f;

    [Header("Turbo")]
    public float turboMultiplier = 1.5f;
    public float turboDuration = 3f;
    public float turboCooldown = 45f;

    [Header("Camera Settings")]
    public Vector3 cameraOffset = new Vector3(0, 4, -8);
    public float cameraSmooth = 5f;

    // Internal
    float currentSpeed;
    float turboTimer;
    float turboCooldownTimer;
    bool turboActive;

    Transform cameraPivot;
    Camera cam;

    Rigidbody rb;

    void Start()
    {
        SetupVisualModel();
        SetupRigidbody();
        SetupCamera();
        SnapToTrackStart();
    }

    void Update()
    {
        HandleMovement();
        HandleTurbo();
        UpdateCamera();
    }

    // -------------------------
    // VISUAL CAPSULE + COLLIDER
    // -------------------------
    void SetupVisualModel()
    {
        if (GetComponentInChildren<MeshRenderer>() != null)
            return;

        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.transform.SetParent(transform, false);

        capsule.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        capsule.transform.localScale = new Vector3(0.25f, 0.5f, 0.25f); // 25% size

        // Collider is still useful for physics
        CapsuleCollider col = capsule.GetComponent<CapsuleCollider>();
        if (col == null)
            col = capsule.AddComponent<CapsuleCollider>();
    }

    // -------------------------
    // RIGIDBODY
    // -------------------------
    void SetupRigidbody()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.mass = 1f;
        rb.drag = 0.1f;
        rb.angularDrag = 0.05f;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // Prevent tipping over
    }

    // -------------------------
    // CAMERA
    // -------------------------
    void SetupCamera()
    {
        GameObject pivotGO = new GameObject("CameraPivot");
        pivotGO.transform.SetParent(transform, false);
        cameraPivot = pivotGO.transform;

        GameObject camGO = new GameObject("MainCamera");
        camGO.transform.SetParent(cameraPivot, false);
        cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.fieldOfView = 60f;

        camGO.AddComponent<AudioListener>();
    }

    void UpdateCamera()
    {
        if (cameraPivot == null) return;

        Vector3 desiredPos = transform.position + transform.TransformDirection(cameraOffset);
        cameraPivot.position = Vector3.Lerp(cameraPivot.position, desiredPos, cameraSmooth * Time.deltaTime);
        cameraPivot.LookAt(transform.position + transform.forward * 5f);
    }

    // -------------------------
    // SPAWN AT TRACK START
    // -------------------------
    void SnapToTrackStart()
    {
        if (trackGenerator == null)
        {
            Debug.LogWarning("No TrackGenerator assigned to ArcadeCarController.");
            return;
        }

        Vector3 startPos = trackGenerator.GetStartPosition();
        Vector3 startForward = trackGenerator.GetStartForward();

        transform.position = startPos + Vector3.up * 1.5f;
        transform.rotation = Quaternion.LookRotation(startForward, Vector3.up);
    }

    // -------------------------
    // MOVEMENT
    // -------------------------
    void HandleMovement()
    {
        float forwardInput = Input.GetAxis("Vertical");  // W/S
        float turnInput = Input.GetAxis("Horizontal");   // A/D

        // Accelerate forward/backward
        currentSpeed += acceleration * forwardInput * Time.deltaTime;

        // Turbo speed
        float max = turboActive ? maxSpeed * turboMultiplier : maxSpeed;
        currentSpeed = Mathf.Clamp(currentSpeed, -max * 0.5f, max); // allow half maxSpeed backwards

        // Hard brake (space)
        if (Input.GetKey(KeyCode.Space))
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, brakeStrength * Time.deltaTime);
        }

        // Turn based on current speed
        float turnAmount = turnInput * turnSpeed * Time.deltaTime * (Mathf.Abs(currentSpeed) / maxSpeed);
        transform.Rotate(0, turnAmount, 0);

        // Move using Rigidbody for physics
        if (rb != null)
        {
            Vector3 move = transform.forward * currentSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + move);
        }
        else
        {
            transform.position += transform.forward * currentSpeed * Time.deltaTime;
        }
    }

    // -------------------------
    // TURBO
    // -------------------------
    void HandleTurbo()
    {
        if (turboActive)
        {
            turboTimer -= Time.deltaTime;
            if (turboTimer <= 0)
            {
                turboActive = false;
                turboCooldownTimer = turboCooldown;
            }
        }
        else
        {
            if (turboCooldownTimer > 0)
                turboCooldownTimer -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.LeftShift) && turboCooldownTimer <= 0)
            {
                turboActive = true;
                turboTimer = turboDuration;
            }
        }
    }
}
