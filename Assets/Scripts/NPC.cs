using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Relationship = GameManager.Relationship;

public class NPC : MonoBehaviour {

    #region Events

    public static System.Action<NPC> EnterInactiveStateEvent = null;
    public static System.Action<NPC> EnterCloseFriendStateEvent = null;
    public static System.Action<NPC> WrongActionEvent = null;

    #endregion

    #region Fields

    [Header("References")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Rigidbody2D rb;
    [SerializeField]
    private TextMesh textMesh;
    [SerializeField]
    private GameObject aoe;

    [Header("Movement Variables")]
    [SerializeField]
    private float minSpeed;
    [SerializeField]
    private float maxSpeed;
    
    [SerializeField]
    private float minTimeBeforeDirectionChange;
    [SerializeField]
    private float maxTimeBeforeDirectionChange;
    
    [Header("States")]
    [SerializeField]
    private float minTimeBeforeFriendTransition;
    [SerializeField]
    private float maxTimeBeforeFriendTransition;

    [SerializeField]
    private float receivingEncouragementStateDuration;

    [SerializeField]
    private KeyCode[] keys = new KeyCode[0];

    private Vector2 currentDirection = Vector2.zero;
    private float currentSpeed = 0f;
    private float nextDirectionChangeTime = 0f;

    private float nextStateTransitionTime = 0f;

    private KeyCode associatedKey = KeyCode.None;

    private bool shouldFollowPlayer = false;

    private StateMachine stateMachine = new StateMachine();

    private Vector2 savedVelocity = Vector2.zero;

    #endregion

    #region Properties

    // Cache the main camera for perf reasons since we need to access it every frame
    // Unity calls Object.FindObjectWithTag("MainCamera") *every single time* you access Camera.main, which is ridiculous
    private Camera _mainCamera = null;
    private Camera MainCamera
    {
        get
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
            return _mainCamera;
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Add all the states to the state machine
        stateMachine.AddState((int)Relationship.Stranger, OnEnterStrangerState, null, null);
        stateMachine.AddState((int)Relationship.Acquaintance, OnEnterAcquaintanceState, null, OnUpdateAcquaintanceState);
        stateMachine.AddState((int)Relationship.Friend, OnEnterFriendState, OnExitFriendState, null);
        stateMachine.AddState((int)Relationship.ReceivingEncouragement, OnEnterReceivingEncourgaementState, OnExitReceivingEncourgaementState, OnUpdateReceivingEncourgaementState);
        stateMachine.AddState((int)Relationship.CloseFriend, OnEnterCloseFriendState, null, null);
        stateMachine.AddState((int)Relationship.Inactive, OnEnterInactiveState, null, null);

        // Immediately deactivate it
        stateMachine.Initialize((int)Relationship.Inactive);
    }

    // All physics calculations should always be done in FixedUpdate
    private void FixedUpdate()
    {
        // Get the new direction
        if (Time.time > nextDirectionChangeTime)
        {
            currentSpeed = Random.Range(minSpeed, maxSpeed);
            currentDirection = UnityExtensions.GetRandomUnitVector();
            nextDirectionChangeTime = GetNextDirectionChangeTime();
        }

        // Add a force to the NPC to move it
        if(stateMachine.CurrentStateId == (int)Relationship.Friend)
        {
            // Friends should tend towards the player when it's not recharging
            if (shouldFollowPlayer && !Player.Instance.IsInChurch)
            {
                shouldFollowPlayer = false;
                rb.AddForce((Player.Instance.transform.position - transform.position).normalized * currentSpeed);
            }
            else
            {
                shouldFollowPlayer = true;
                rb.AddForce((currentDirection) * currentSpeed);
            }
        }
        else if (stateMachine.CurrentStateId == (int)Relationship.CloseFriend)
        {
            // Use max speed as we want it to quickly get out of the screen to be recycled
            rb.AddForce((currentDirection) * maxSpeed);

            // Deactivate the NPC once it's no longer visible
            if (!MainCamera.IsObjectVisible(spriteRenderer.bounds))
            {
                stateMachine.EnterState((int)Relationship.Inactive);
            }
        }
        else if(stateMachine.CurrentStateId != (int)Relationship.ReceivingEncouragement)
        {
            rb.AddForce((currentDirection) * currentSpeed);
        }

        // Clamp the velocity
        if(rb.velocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            rb.velocity = Vector2.ClampMagnitude(rb.velocity, maxSpeed);
        }
    }

    private void Update()
    {
        stateMachine.Update();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject == Player.Instance.gameObject)
        {
            // Colliding with the player as a stranger triggers a state change to acquaintance
            if (stateMachine.CurrentStateId == (int)Relationship.Stranger)
            {
                stateMachine.EnterState((int)Relationship.Acquaintance);                
            }
            else if(stateMachine.CurrentStateId == (int)Relationship.Friend)
            {
                if (Input.anyKey)
                {
                    if (Input.GetKey(associatedKey))
                    {
                        stateMachine.EnterState((int)Relationship.ReceivingEncouragement);
                    }
                    else
                    {
                        WrongActionEvent.SafeRaise(this);
                        stateMachine.EnterState((int)Relationship.Acquaintance);
                    }
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var npc = collision.GetComponent<NPC>();
        if (npc != null)
        {
            npc.ChainReaction();
        }
    }

    #endregion

    #region States

    private void ChainReaction()
    {
        // Stranger, Aquaintance, Friend
        if(stateMachine.CurrentStateId <= 2)
        {
            stateMachine.EnterState(stateMachine.CurrentStateId + 1);
        }
    }

    public void Activate()
    {
        stateMachine.EnterState((int)Relationship.Stranger);
    }

    public void Deactivate()
    {
        stateMachine.EnterState((int)Relationship.Inactive);
    }

    private void OnEnterStrangerState(int previousStateId)
    {
        if (previousStateId == (int)Relationship.Inactive)
        {
            gameObject.SetActive(true);

            // Give the NPC with random values so it behaves differently fromt other NPCs
            nextDirectionChangeTime = GetNextDirectionChangeTime();
            currentDirection = UnityExtensions.GetRandomUnitVector();
            currentSpeed = Random.Range(minSpeed, maxSpeed);

            // Debug
            spriteRenderer.color = Color.white;
        }
    }

    private void OnEnterAcquaintanceState(int previousStateId)
    {
        // Debug
        spriteRenderer.color = Color.black;

        nextStateTransitionTime = Time.time + Random.Range(minTimeBeforeFriendTransition, maxTimeBeforeFriendTransition);
    }

    private void OnUpdateAcquaintanceState()
    {
        // Only transition the state when the player can see it
        if(Time.time > nextStateTransitionTime && MainCamera.IsObjectVisible(spriteRenderer.bounds))
        {
            stateMachine.EnterState((int)Relationship.Friend);
        }
    }

    private void OnEnterFriendState(int previousStateId)
    {
        // Debug
        spriteRenderer.color = Color.yellow;

        associatedKey = keys[Random.Range(0, keys.Length)];
        textMesh.text = associatedKey.ToString();
    }

    private void OnExitFriendState(int previousStateId)
    {
        textMesh.text = string.Empty;
    }

    private void OnEnterReceivingEncourgaementState(int previousStateId)
    {
        // Debug
        spriteRenderer.color = Color.blue;

        // NPC pauses in place while receiving encouragement
        savedVelocity = rb.velocity;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;

        nextStateTransitionTime = Time.time + receivingEncouragementStateDuration;
        aoe.SetActive(true);
    }

    private void OnUpdateReceivingEncourgaementState()
    {
        if (Time.time > nextStateTransitionTime)
        {
            stateMachine.EnterState((int)Relationship.CloseFriend);
        }
    }

    private void OnExitReceivingEncourgaementState(int previousStateId)
    {
        rb.isKinematic = false;

        // Set to max speed because:
        // 1. Velocity magnitude might be close to 0 and will take awhile to increase back up
        // 2. We want the NPC to quickly exit the screen and get recycled
        rb.velocity = savedVelocity.normalized*maxSpeed;
        aoe.SetActive(false);
    }

    private void OnEnterCloseFriendState(int previousStateId)
    {
        // Debug
        spriteRenderer.color = Color.green;

        // Don't change the direction anymore so it will leave the screen
        nextDirectionChangeTime = float.MaxValue;

        EnterCloseFriendStateEvent.SafeRaise(this);
    }
    
    private void OnEnterInactiveState(int previousStateId)
    {
        // Reset everything
        gameObject.SetActive(false);
        textMesh.text = string.Empty;
        aoe.SetActive(false);

        EnterInactiveStateEvent.SafeRaise(this);
    }

    #endregion

    #region Helpers

    private float GetNextDirectionChangeTime()
    {
        return Time.time + Random.Range(minTimeBeforeDirectionChange, maxTimeBeforeDirectionChange);
    }

    #endregion
}
