using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Relationship = GameManager.Relationship;

public class NPC : MonoBehaviour {

    #region Events

    public static System.Action<NPC> EnterInactiveStateEvent = null;
    public static System.Action<NPC> EnterCloseFriendStateEvent = null;

    #endregion

    #region Fields

    [Header("References")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Rigidbody2D rb;
    [SerializeField]
    private TextMesh textMesh;

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

    public bool IsActive = false;

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
        stateMachine.AddState((int)Relationship.Friend, OnEnterFriendState, null, null);
        stateMachine.AddState((int)Relationship.ReceivingEncouragement, OnEnterReceivingEncourgaementState, null, OnUpdateReceivingEncourgaementState);
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
            // Friends should tend towards the player
            if (shouldFollowPlayer)
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
            // Deactivate the NPC once it's no longer visible
            if (!MainCamera.IsObjectVisible(spriteRenderer.bounds))
            {
                stateMachine.EnterState((int)Relationship.Inactive);
            }
        }
        else
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
                // Don't use OnCollisionStay for this, since there will be a wrong action mechanic in future
                if (Input.GetKey(associatedKey))
                {
                    stateMachine.EnterState((int)Relationship.ReceivingEncouragement);
                }
            }
        }
    }

    #endregion

    #region States

    public void Activate()
    {
        stateMachine.EnterState((int)Relationship.Stranger);
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
        if(Time.time > nextStateTransitionTime)
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

    private void OnEnterReceivingEncourgaementState(int previousStateId)
    {
        // Debug
        spriteRenderer.color = Color.blue;

        textMesh.text = string.Empty;
        nextStateTransitionTime = Time.time + receivingEncouragementStateDuration;
    }

    private void OnUpdateReceivingEncourgaementState()
    {
        if (Time.time > nextStateTransitionTime)
        {
            stateMachine.EnterState((int)Relationship.CloseFriend);
        }
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
        gameObject.SetActive(false);
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
