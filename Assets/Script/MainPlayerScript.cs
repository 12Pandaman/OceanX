using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class MainPlayerScript : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 10f;
    public float jumpForce = 5f;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer;
    private Vector2 moveInput;
    private bool isSprinting = false;
    private Rigidbody rb;

    [Header("Look (FPS)")]
    public float mouseSensitivityX = 50f; // Left-right turn speed
    public float mouseSensitivityY = 25f; // Up-down turn speed (adjusted to be lower than X)
    public Transform cameraTransform;
    public Transform cameraTarget; // [New] Target for the camera to follow

    [Header("Camera Zoom Settings")]
    public float zoomSensitivity = 0.02f; // Scroll speed multiplier
    public float minZoomDistance = -1f;   // Closest distance (3rd Person)
    public float maxZoomDistance = -15f;  // Furthest distance (3rd Person)
    public float minFOV = 30f;            // Closest FOV (1st Person)
    public float maxFOV = 75f;            // Furthest FOV (1st Person)
    public float zoomSmoothTime = 10f;    // Smooth speed
    
    private Vector2 lookInput;
    private float verticalLookRotation = 0f;

    [Header("Combat Settings")]
    public float attackRange = 5f; // Attack range
    public int attackDamage = 10; // Damage amount

    [Header("Hide For Local Player")]
    public Renderer[] visualsToHide;

    [Header("UI Settings")]
    public GameObject monsterUI; // Slot for Monster UI
    public GameObject survivorUI; // Slot for Survivor UI

    [Header("Prop Hunt Settings")]
    public Transform propVisualContainer; // Container holding the prop models
    public float interactRange = 3f;
    public float interactRadius = 0.5f; // Target radius, larger makes it easier to hit
    public Vector3 propPositionOffset = Vector3.zero; // [New] Offset to fix prop pivot issues

    [Header("Underwater Settings")]
    public bool isUnderwater = false;
    public float swimSpeed = 4f;
    public float swimSprintSpeed = 8f;
    public float waterDrag = 3f;
    private float defaultDrag = 0f;
    private bool defaultUseGravity = true;
    private bool defaultFogState;
    private Color defaultFogColor;
    private float defaultFogDensity;
    private FogMode defaultFogMode;

    // Variables to control Player Input
    private PlayerInput playerInput;
    private PlayerCameraSetup cameraSetup;

    // Variable to check if typing/console is open
    private bool isTyping = false;
    private Transform currentMeshTransform; // Tracks current active visual

    // Zoom tracking variables
    private Transform actualCameraTransform;
    private Camera actualCameraComponent;
    private float targetZoomZ;
    private float targetFOV;
    private bool isThirdPersonCamera = false;

    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            defaultDrag = rb.drag;
            defaultUseGravity = rb.useGravity;
        }

        playerInput = GetComponent<PlayerInput>(); // Get the Component

        // Try to find Camera automatically if not assigned in Inspector
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
        }

        // Prepare Camera for Zooming (Detect if 3rd person or 1st person)
        if (cameraTransform != null)
        {
            actualCameraComponent = cameraTransform.GetComponentInChildren<Camera>();
            if (actualCameraComponent != null)
            {
                if (actualCameraComponent.transform != cameraTransform)
                {
                    // 3rd Person (Camera is a child of the boom)
                    isThirdPersonCamera = true;
                    actualCameraTransform = actualCameraComponent.transform;
                    targetZoomZ = actualCameraTransform.localPosition.z;
                }
                else
                {
                    // 1st Person (Camera is the boom itself)
                    isThirdPersonCamera = false;
                    targetFOV = actualCameraComponent.fieldOfView;
                }
            }
        }

        if (IsOwner)
        {
            // Store the original scene Fog settings to restore them after exiting the water
            defaultFogState = RenderSettings.fog;
            defaultFogColor = RenderSettings.fogColor;
            defaultFogDensity = RenderSettings.fogDensity;
            defaultFogMode = RenderSettings.fogMode;

            // Enable local camera and Input
            if (cameraTransform != null) cameraTransform.gameObject.SetActive(true);
            if (playerInput != null) playerInput.enabled = true;

            currentMeshTransform = playerVisualBody; // Default target

            // 1. Hide character UI on spawn (since Lobby is active)
            if (monsterUI != null) monsterUI.SetActive(false);
            if (survivorUI != null) survivorUI.SetActive(false);

            // 2. Wait for LobbyManager to start the game
            if (LobbyManager.Instance != null)
            {
                // A. Subscribe to value changed event
                LobbyManager.Instance.IsGameStarted.OnValueChanged += OnGameStartedChanged;

                // B. Check current value immediately (fixes late joiner bug)
                if (LobbyManager.Instance.IsGameStarted.Value)
                {
                    OnGameStartedChanged(false, true);
                }
            }
            else
            {
                OnGameStartedChanged(false, true);
            }
        }
        else
        {
            // Disable camera and input for other players
            if (cameraTransform != null) cameraTransform.gameObject.SetActive(false);
            if (playerInput != null) playerInput.enabled = false;

            // Disable UI for other players
            if (monsterUI != null) monsterUI.SetActive(false);
            if (survivorUI != null) survivorUI.SetActive(false);
        }
        cameraSetup = GetComponent<PlayerCameraSetup>();
    }

    public override void OnNetworkDespawn()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.IsGameStarted.OnValueChanged -= OnGameStartedChanged;
        }
    }

    private void OnGameStartedChanged(bool previousValue, bool newValue)
    {
        if (newValue == true) // When Game Start becomes true
        {
            if (IsOwner)
            {
                // Force lock mouse cursor immediately
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // Ensure Game Menu is closed when game starts
                if (GameMenuManager.Instance != null) 
                    GameMenuManager.Instance.ToggleMenu(false);
            }
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.started || context.performed)
            isSprinting = true;
        else if (context.canceled)
            isSprinting = false;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        
        PlayerStateSync myState = GetComponent<PlayerStateSync>();
        bool isJUnlocked = myState != null && myState.IsCursorUnlocked;

        if (isTyping || (GameMenuManager.Instance != null && GameMenuManager.Instance.isMenuOpen) || isJUnlocked) return;
        bool isGameStarted = LobbyManager.Instance == null || LobbyManager.Instance.IsGameStarted.Value;
        if (!isGameStarted) return;

        if (isUnderwater) return; // Disable ground jump when underwater

        if (context.performed && IsGrounded())
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private bool IsGrounded()
    {
        // ขยับจุดยิงขึ้นมาเล็กน้อย (0.1f) ป้องกันเส้นจมลงไปใต้พื้น
        Vector3 origin = transform.position + (Vector3.up * 0.1f);
        
        bool isHit = Physics.Raycast(origin, Vector3.down, groundCheckDistance, groundLayer);
        
        // Draw debug ray in Scene view (Green = Grounded, Red = Airborne)
        Debug.DrawRay(origin, Vector3.down * groundCheckDistance, isHit ? Color.green : Color.red, 2f);
        
        return isHit;
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        // Prevent attack/transform if the character cursor is unlocked or typing
        PlayerStateSync stateForFire = GetComponent<PlayerStateSync>();
        bool isJUnlocked = stateForFire != null && stateForFire.IsCursorUnlocked;

        if (isTyping || isJUnlocked) return;

        // Don't attack if mouse is unlocked (cursor visible)
        if (Cursor.lockState == CursorLockMode.None) return;

        // Don't attack if menu is open
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.isMenuOpen) return;

        // Separate logic by Role on left click
        if (context.started)
        {
            Debug.Log("[OnFire] Left click triggered");
            PlayerStateSync myState = GetComponent<PlayerStateSync>();
            if (myState != null)
            {
                Debug.Log($"[OnFire] Current Role Index: {myState.RoleIndex.Value}");
                if (myState.RoleIndex.Value == 1) // Monster
                {
                    AttemptAttack();
                }
                else if (myState.RoleIndex.Value == 0) // Survivor
                {
                    HandlePropTransformation();
                }
            }
            else
            {
                Debug.LogWarning("[OnFire] PlayerStateSync component not found on character!");
            }
        }
    }

    public void OnResetToHuman(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        // Trigger only on button down (started)
        if (context.started)
        {
            Debug.Log("[PropHunt] Reset to Human Input Triggered");
            ResetToHumanServerRpc();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        // Trigger interact when the button is pressed (Started)
        if (context.started)
        {
            PlayerStateSync myState = GetComponent<PlayerStateSync>();
            bool isJUnlocked = myState != null && myState.IsCursorUnlocked;
            FishMinigameManager minigameManager = GetComponent<FishMinigameManager>();

            if (isTyping || (GameMenuManager.Instance != null && GameMenuManager.Instance.isMenuOpen) || isJUnlocked) return;
            if (minigameManager != null && minigameManager.IsMinigamePlaying) return;

            if (cameraTransform != null)
            {
                Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
                
                // ใช้ SphereCastAll เพื่อทะลุตัวละครของเราเองและทำให้เล็งโดนง่ายขึ้น
                RaycastHit[] hits = Physics.SphereCastAll(ray, interactRadius, interactRange);
                
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.transform.root == transform.root) continue; // ข้ามโมเดลตัวละครของเราเอง

                    FishMinigameInteractable interactable = hit.collider.GetComponentInParent<FishMinigameInteractable>();
                    if (interactable != null)
                    {
                        interactable.StartMinigame(minigameManager);
                        break; // เจอเป้าหมายแล้วหยุดค้นหา
                    }
                }
            }
        }
    }

    private void AttemptAttack()
    {
        PlayerStateSync myState = GetComponent<PlayerStateSync>();

        if (myState != null && myState.RoleIndex.Value == 1)
        {
            Debug.Log("Monster Attempting Attack!");
            if (cameraTransform != null)
            {
                Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
                RaycastHit[] hits = Physics.RaycastAll(ray, attackRange);
                bool hitSomeone = false;

                foreach (RaycastHit hit in hits)
                {
                    PlayerStateSync targetState = hit.collider.GetComponentInParent<PlayerStateSync>();
                    if (targetState == null)
                    {
                        targetState = hit.collider.transform.root.GetComponentInChildren<PlayerStateSync>();
                    }

                    if (targetState != null && targetState != myState)
                    {
                        if (targetState.RoleIndex.Value == 0)
                        {
                            Debug.Log($"Hit Survivor! Dealing {attackDamage} damage.");
                            targetState.TakeDamageServerRpc(attackDamage);
                            hitSomeone = true;
                            break;
                        }
                    }
                }

                if (!hitSomeone && hits.Length > 0)
                {
                    Debug.Log("Hit nothing actionable (or self/wall).");
                }
            }
        }
    }

    private void HandlePropTransformation()
    {
        Debug.Log("[PropHunt] Entered HandlePropTransformation function");
        if (cameraTransform != null)
        {
            // Shoot Ray from the center of the screen (camera)
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            
            // (Optional) Draw a red Ray in Scene View to show direction (lasts 2 seconds)
            Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.red, 2f);

            // Use SphereCastAll to pass through our own character and find the Prop in front
            RaycastHit[] hits = Physics.SphereCastAll(ray, interactRadius, interactRange);
            bool hitProp = false;

            // Loop through all hits
            foreach (RaycastHit hit in hits)
            {
                // Skip if hitting our own character
                if (hit.collider.transform.root == transform.root) continue;

                Debug.Log($"[PropHunt] SphereCast hit: {hit.collider.gameObject.name} | Tag: {hit.collider.tag}");

                if (hit.collider.CompareTag("Prop"))
                {
                    Debug.Log("[PropHunt] Correct tag! Sending transformation command...");
                    // Send command to Server to change model
                    ChangePropServerRpc(hit.collider.gameObject.name);
                    hitProp = true;
                    break; // Stop searching after finding the first Prop
                }
            }

            if (!hitProp)
            {
                Debug.Log("[PropHunt] SphereCast did not hit any valid 'Prop' in range");
            }
        }
        else
        {
            Debug.LogWarning("[PropHunt] Error: cameraTransform is Null, cannot shoot SphereCast!");
        }
    }

    [ServerRpc]
    private void ChangePropServerRpc(string propName)
    {
        // Send data to everyone to change model simultaneously
        ChangePropClientRpc(propName);
    }

    [ClientRpc]
    private void ChangePropClientRpc(string propName)
    {
        // 1. Hide original body (SurvivorsCapsule)
        if (playerVisualBody != null) playerVisualBody.gameObject.SetActive(false);

        if (propVisualContainer != null)
        {
            // 2. Disable all old models
            foreach (Transform child in propVisualContainer)
            {
                child.gameObject.SetActive(false);
            }

            // 3. Find and enable new model
            string target = propName.ToLower().Replace("(clone)", "").Trim();

            foreach (Transform child in propVisualContainer)
            {
                if (child.name.ToLower().Contains(target)) 
                {
                    child.gameObject.SetActive(true);
                    
                    // [Important!] Force position to reset to center + offset
                    child.localPosition = propPositionOffset; 
                    currentMeshTransform = child; // Update camera target
                    
                    Debug.Log($"Activated: {child.name} at {child.localPosition}");
                    break;
                }
            }
        }
    }

    [ServerRpc]
    private void ResetToHumanServerRpc()
    {
        ResetToHumanClientRpc();
    }

    [ClientRpc]
    private void ResetToHumanClientRpc()
    {
        // 1. ปิดโมเดลวัตถุ (Prop) ทุกตัวที่เคยแปลงร่างไว้
        if (propVisualContainer != null)
        {
            foreach (Transform child in propVisualContainer)
            {
                child.gameObject.SetActive(false);
            }
        }

        // 2. เปิดร่างคนปกติกลับมา (ตัวสีน้ำเงิน)
        if (playerVisualBody != null)
        {
            playerVisualBody.gameObject.SetActive(true);
            
            // บังคับให้ MeshRenderer แสดงผล (แก้ปัญหาตัวหายในเครื่องตัวเอง)
            MeshRenderer mr = playerVisualBody.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = true;
            
            currentMeshTransform = playerVisualBody; // Reset camera target to human
        }
        
        // 3. Re-enable Survivor UI for the owner
        if (IsOwner && survivorUI != null)
        {
            survivorUI.SetActive(true);
        }

        Debug.Log("Returned to Human form and UI restored");
    }

    private void Update()
    {
        if (!IsOwner) return;

        bool isGameStarted = LobbyManager.Instance == null || LobbyManager.Instance.IsGameStarted.Value;

        // เปิด/ปิด UI ให้ตรงกับฝ่ายที่เลือก (RoleIndex) และสถานะเกม
        PlayerStateSync stateSync = GetComponent<PlayerStateSync>();
        if (stateSync != null)
        {
            if (isGameStarted)
            {
                if (survivorUI != null && survivorUI.activeSelf != (stateSync.RoleIndex.Value == 0)) survivorUI.SetActive(stateSync.RoleIndex.Value == 0);
                if (monsterUI != null && monsterUI.activeSelf != (stateSync.RoleIndex.Value == 1)) monsterUI.SetActive(stateSync.RoleIndex.Value == 1);
            }
            else
            {
                if (survivorUI != null && survivorUI.activeSelf) survivorUI.SetActive(false);
                if (monsterUI != null && monsterUI.activeSelf) monsterUI.SetActive(false);
            }
        }

        if (!isGameStarted) return;

        // Check menu
        if (GameMenuManager.Instance != null && GameMenuManager.Instance.isMenuOpen) return;

        // Toggle typing mode when pressing Slash (/)
        if (Keyboard.current != null && Keyboard.current.slashKey.wasPressedThisFrame)
        {
            // Toggle character lock state
            isTyping = !isTyping;
        }

        // Exit typing lock when Enter or Escape is pressed
        else if (isTyping && Keyboard.current != null && 
                 (Keyboard.current.enterKey.wasPressedThisFrame || 
                  Keyboard.current.numpadEnterKey.wasPressedThisFrame || 
                  Keyboard.current.escapeKey.wasPressedThisFrame))
        {
            isTyping = false;
        }
        
        PlayerStateSync myState = GetComponent<PlayerStateSync>();
        bool isJUnlocked = myState != null && myState.IsCursorUnlocked;
        FishMinigameManager minigameManager = GetComponent<FishMinigameManager>();
        bool isMinigameOpen = minigameManager != null && minigameManager.IsMinigamePlaying;

        // [Important] Force hide and lock mouse every frame while in-game
        // (To combat plugins like Quantum Console from stealing the mouse)
        if (!isJUnlocked && !isMinigameOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (isMinigameOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Smoothly move the CameraTarget to the current active mesh position
        if (cameraTarget != null && currentMeshTransform != null)
        {
            cameraTarget.position = Vector3.Lerp(cameraTarget.position, currentMeshTransform.position, Time.deltaTime * 15f);
        }

        // Handle Camera Zoom via Mouse Scroll Wheel
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                if (isThirdPersonCamera && actualCameraTransform != null)
                {
                    // Positive scroll = zoom in (closer to 0)
                    targetZoomZ += scroll * zoomSensitivity;
                    targetZoomZ = Mathf.Clamp(targetZoomZ, maxZoomDistance, minZoomDistance);
                }
                else if (!isThirdPersonCamera && actualCameraComponent != null)
                {
                    // Positive scroll = zoom in (lower FOV)
                    targetFOV -= scroll * zoomSensitivity;
                    targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
                }
            }
        }

        // Apply smooth zoom
        if (isThirdPersonCamera && actualCameraTransform != null)
        {
            Vector3 localPos = actualCameraTransform.localPosition;
            localPos.z = Mathf.Lerp(localPos.z, targetZoomZ, Time.deltaTime * zoomSmoothTime);
            actualCameraTransform.localPosition = localPos;
        }
        else if (!isThirdPersonCamera && actualCameraComponent != null)
        {
            actualCameraComponent.fieldOfView = Mathf.Lerp(actualCameraComponent.fieldOfView, targetFOV, Time.deltaTime * zoomSmoothTime);
        }

        if (!isJUnlocked && !isTyping && !isMinigameOpen)
        {
            Look();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        bool isGameStarted = LobbyManager.Instance == null || LobbyManager.Instance.IsGameStarted.Value;

        if (!isGameStarted)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        PlayerStateSync myState = GetComponent<PlayerStateSync>();
        bool isJUnlocked = myState != null && myState.IsCursorUnlocked;
        FishMinigameManager minigameManager = GetComponent<FishMinigameManager>();
        bool isMinigameOpen = minigameManager != null && minigameManager.IsMinigamePlaying;

        // Stop moving if menu is open or typing
        if ((GameMenuManager.Instance != null && GameMenuManager.Instance.isMenuOpen) || isTyping || isJUnlocked || isMinigameOpen)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
            return;
        }

        Move();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;
        
        // Make sure to set the Water object's Tag to "Water" and check "Is Trigger"
        if (other.CompareTag("Water"))
        {
            isUnderwater = true;
            if (rb != null)
            {
                rb.drag = waterDrag;
                rb.useGravity = false;
            }
            
            // Underwater fog effect
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.1f, 0.4f, 0.7f, 0.6f);
            RenderSettings.fogDensity = 0.05f;
            RenderSettings.fogMode = FogMode.Exponential;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner) return;
        
        if (other.CompareTag("Water"))
        {
            isUnderwater = false;
            if (rb != null)
            {
                rb.drag = defaultDrag;
                rb.useGravity = defaultUseGravity;
            }
            
            // Restore original fog settings when exiting water
            RenderSettings.fog = defaultFogState;
            RenderSettings.fogColor = defaultFogColor;
            RenderSettings.fogDensity = defaultFogDensity;
            RenderSettings.fogMode = defaultFogMode;
        }
    }

    [Header("Visual Body")]
    public Transform playerVisualBody;
    public float bodyRotationSpeed = 10f;

    private void Look()
    {
        // Use separate sensitivity per axis
        float lookX = lookInput.x * mouseSensitivityX * Time.deltaTime;
        float lookY = lookInput.y * mouseSensitivityY * Time.deltaTime;

        // Rotate the entire character so the camera rotates along with it
        transform.Rotate(Vector3.up * lookX);

        if (cameraTransform != null)
        {
            verticalLookRotation -= lookY;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

            Vector3 camEuler = cameraTransform.localEulerAngles;
            camEuler.x = verticalLookRotation;
            cameraTransform.localEulerAngles = camEuler;
        }
    }

    private void Move()
    {
        Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;

        // If not underwater, lock movement to horizontal plane
        if (!isUnderwater)
        {
            forward.y = 0f;
            right.y = 0f;
        }

        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        
        // Check if sprinting (W key must be held to sprint on ground)
        bool isActuallySprinting = isSprinting && moveInput.y > 0;

        if (isUnderwater)
        {
            // Swimming System (Float and move 3D based on camera)
            bool isSwimSprinting = isSprinting; // Swim faster on Shift regardless of direction
            float currentSpeed = isSwimSprinting ? swimSprintSpeed : swimSpeed;
            
            // Allow swimming straight up with Spacebar
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            {
                moveDirection.y += 1f;
            }
            
            Vector3 targetVelocity = moveDirection.normalized * currentSpeed;
            
            // Smooth velocity transition for underwater drag effect
            rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, Time.deltaTime * 5f);

            // Rotate visual body toward movement direction (horizontal axis only)
            if (playerVisualBody != null && moveDirection.sqrMagnitude > 0.01f)
            {
                Vector3 lookDir = moveDirection;
                lookDir.y = 0; 
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                    playerVisualBody.rotation = Quaternion.Slerp(playerVisualBody.rotation, targetRotation, Time.deltaTime * bodyRotationSpeed);
                }
            }
        }
        else
        {
            // Ground movement system
            float currentSpeed = isActuallySprinting ? sprintSpeed : moveSpeed;
            Vector3 targetVelocity = moveDirection * currentSpeed;

            rb.velocity = new Vector3(targetVelocity.x, rb.velocity.y, targetVelocity.z);

            if (playerVisualBody != null && moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                playerVisualBody.rotation = Quaternion.Slerp(playerVisualBody.rotation, targetRotation, Time.deltaTime * bodyRotationSpeed);
            }
        }
    }
}