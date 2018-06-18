using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Relationship = NPCManager.Relationship;

public class NPC : MonoBehaviour {

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

    private Vector2 currentDirection;
    private float currentSpeed;
    private float nextDirectionChangeTime;

    private float nextStateTransitionTime;

    private KeyCode associatedKey;

    private bool shouldFollowPlayer = false;

    private StateMachine stateMachine = new StateMachine();

    #endregion

    #region Properties

    private Vector2 Position2D
    {
        get { return new Vector2(transform.position.x, transform.position.y); }
    }

    private Vector2 Right2D
    {
        get { return new Vector2(transform.right.x, transform.right.y); }
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Add all the states to the state machine
        stateMachine.AddState((int)Relationship.Stranger, null, null, null);
        stateMachine.AddState((int)Relationship.Acquaintance, OnEnterAcquaintanceState, null, OnUpdateAcquaintanceState);
        stateMachine.AddState((int)Relationship.Friend, OnEnterFriendState, null, null);
        stateMachine.AddState((int)Relationship.ReceivingEncouragement, OnEnterReceivingEncourgaementState, null, OnUpdateReceivingEncourgaementState);
        stateMachine.AddState((int)Relationship.CloseFriend, OnEnterCloseFriendState, null, null);

        // Initialize the NPC with random values so it behaves differently fromt other NPCs
        nextDirectionChangeTime = GetNextDirectionChangeTime();
        currentSpeed = Random.Range(minSpeed, maxSpeed);
    }

    private void Start()
    {
        // Initialize the state machine
        stateMachine.Initialize((int)Relationship.Stranger);
        currentDirection = GetRandomUnitVector();
    }

    // All physics calculations should always be done in FixedUpdate
    private void FixedUpdate()
    {
        // Get the new direction
        if (Time.time > nextDirectionChangeTime)
        {
            currentSpeed = Random.Range(minSpeed, maxSpeed);
            currentDirection = GetRandomUnitVector();
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

        // TODO: Move out of screen
    }

    #endregion

    #region Helpers

    private float GetNextDirectionChangeTime()
    {
        return Time.time + Random.Range(minTimeBeforeDirectionChange, maxTimeBeforeDirectionChange);
    }
    
    public Vector2 GetRandomUnitVector()
    {
        float angleRadians = Random.Range(0, Mathf.PI * 2);
        return new Vector2(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians));
    }

    #endregion
}
