using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationandMovementController : MonoBehaviour
{
    // Start is called before the first frame update
    PlayerInput playerInput;
    Vector2 CurrentMovementInput;
    Vector3 CurrentMovement;
    Vector3 CurrentRunMovement;
    Vector3 AppliedMovement;
    bool isMovementPressed;
    bool isRunPressed;
    CharacterController characterController;
    Animator animator;
    float rotationFactorPerFrame = 15.0f;
    int isRunningHash;
    int isWalkingHash;
    bool isJumpPressed=false;
    float initialJumpVelocity;
    [SerializeField]
    float maxJumpHeight = 2.0f;
    float maxJumpTime=0.75f;
    bool isJumping = false;
    int jumpCountHash;
    float groundedGravity = -0.5f;
    float gravity = -9.8f;
    int isJumpingHash;
    bool isJumpAnimating = false;
    int JumpCount = 0;
    Dictionary<int,float> initialJumpVelocities = new Dictionary<int,float>();
    Dictionary<int, float> jumpGravities = new Dictionary<int, float>();

    Coroutine currentJumpResetRoutine = null;
    private void Awake()
    {
        playerInput = new PlayerInput();
        characterController = GetComponent<CharacterController>();
        playerInput.CharacterControl.Move.started += onMovement;
        playerInput.CharacterControl.Move.canceled += onMovement;
        playerInput.CharacterControl.Move.performed += onMovement;
        playerInput.CharacterControl.Run.started += onRun;
        playerInput.CharacterControl.Run.canceled += onRun;
        playerInput.CharacterControl.Jump.started += onJump;
        playerInput.CharacterControl.Jump.canceled += onJump;
        animator = GetComponent<Animator>();
        isRunningHash = Animator.StringToHash("IsRunning");
        isWalkingHash = Animator.StringToHash("IsWalking");
        isJumpingHash = Animator.StringToHash("IsJumping");
        jumpCountHash = Animator.StringToHash("jumpcount");

        setupJumpVariables();
    }

    void setupJumpVariables()
    {
        float timetoApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timetoApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timetoApex;
        float secondJumpGravity = (-2 * (maxJumpHeight + 2)) / Mathf.Pow(timetoApex * 1.25f, 2);
        float secondJumpInitialVelocity = (2 * (maxJumpHeight + 2)) / (timetoApex*1.25f);
        float thirdJumpGravity = (-2 * (maxJumpHeight + 4)) / Mathf.Pow(timetoApex * 1.5f, 2);
        float thirdJumpInitialVelocity = (2 * (maxJumpHeight + 4)) / (timetoApex * 1.5f);

        initialJumpVelocities.Add(1, initialJumpVelocity);
        initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        jumpGravities.Add(0, gravity);
        jumpGravities.Add(1, gravity);
        jumpGravities.Add(2, secondJumpGravity);
        jumpGravities.Add(3, thirdJumpGravity);
    }

    void handleJump()
    {
        if(!isJumping && characterController.isGrounded && isJumpPressed)
        {
            if (JumpCount < 3 && currentJumpResetRoutine != null)
                StopCoroutine(currentJumpResetRoutine);
            animator.SetBool(isJumpingHash, true);
            isJumpAnimating = true;
            isJumping = true;
            JumpCount++;
            animator.SetInteger(jumpCountHash, JumpCount);
            CurrentMovement.y = initialJumpVelocities[JumpCount];
            AppliedMovement.y = initialJumpVelocities[JumpCount];
        }
        else if(!isJumpPressed && isJumping && characterController.isGrounded)
        {
            isJumping = false;
        }
    }

    IEnumerator JumpResetCoroutine()
    {
        yield return new WaitForSeconds(.5f);
        JumpCount = 0;
    }
    void handleGravity()
    {
        bool isFalling = CurrentMovement.y <= 0.0f || !isJumpPressed;
        float fallMultiplier = 2.0f;


        if(characterController.isGrounded)
        {
            if (isJumpAnimating)
            {
                animator.SetBool(isJumpingHash, false);
                isJumpAnimating = false;
                currentJumpResetRoutine=StartCoroutine(JumpResetCoroutine());
                if(JumpCount == 3)
                {
                    JumpCount = 0;
                    animator.SetInteger(jumpCountHash, JumpCount);
                }
            }
            CurrentMovement.y = groundedGravity;
            AppliedMovement.y = groundedGravity;
        }
        else if(isFalling)
        {
            float previousYvelocity = CurrentMovement.y;
            CurrentMovement.y = CurrentMovement.y + (jumpGravities[JumpCount] *fallMultiplier* Time.deltaTime);
            AppliedMovement.y = (previousYvelocity + CurrentMovement.y) / 2;
        }
        else
        {
            float previousYvelocity = CurrentMovement.y;
            CurrentMovement.y = CurrentMovement.y + (jumpGravities[JumpCount] *Time.deltaTime);
            AppliedMovement.y = (previousYvelocity + CurrentMovement.y) / 2;
        }
    }

    void onJump(InputAction.CallbackContext context)
    {
        isJumpPressed = context.ReadValueAsButton();
    }
    void onRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }
    void onMovement(InputAction.CallbackContext context)
    {
        CurrentMovementInput = context.ReadValue<Vector2>(); ;
        CurrentMovement.x = CurrentMovementInput.x;
        CurrentMovement.z = CurrentMovementInput.y;
        CurrentRunMovement.x = CurrentMovementInput.x * 3.0f;
        CurrentRunMovement.z = CurrentMovementInput.y * 3.0f;
        isMovementPressed = CurrentMovement.x != 0 || CurrentMovement.z != 0;
    }

    void handleAnimation()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isRunning = animator.GetBool(isRunningHash);

        if(isMovementPressed && !isWalking)
        {
            animator.SetBool(isWalkingHash, true);
        }
        else if (!isMovementPressed && isWalking)
        {
            animator.SetBool(isWalkingHash, false);
        }

        if(isMovementPressed && isRunPressed && !isRunning)
        {
            animator.SetBool(isRunningHash, true);
        }
        if (!isMovementPressed || !isRunPressed && isRunning)
        {
            animator.SetBool(isRunningHash, false);
        }
    }

    void handleRotation()
    {
        Vector3 positionToLookAt;
        
        positionToLookAt.x = CurrentMovement.x;
        positionToLookAt.y = 0.0f;
        positionToLookAt.z = CurrentMovement.z;
        Quaternion currentRotation = transform.rotation;
        if (isMovementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }
    }

    // Update is called once per frame
    void Update()
    {
        handleRotation();
        handleAnimation();


        if (isRunPressed)
        {
            AppliedMovement.x = CurrentRunMovement.x;
            AppliedMovement.z = CurrentRunMovement.z;
        }

        else
        {
            AppliedMovement.x = CurrentMovement.x;
            AppliedMovement.z = CurrentMovement.z;
        }
        characterController.Move(AppliedMovement * Time.deltaTime);
        handleGravity();
        handleJump();
    }

    private void OnEnable()
    {
        playerInput.CharacterControl.Enable();
    }

    private void OnDisable()
    {
        playerInput.CharacterControl.Disable();
    }
}
