using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Relationship = NPCManager.Relationship;

public class NPC : MonoBehaviour {

    #region Fields

    // Debug
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private Rigidbody2D rb;

    [SerializeField]
    private float minSpeed;
    [SerializeField]
    private float maxSpeed;
    
    [SerializeField]
    private float rotationalSpeed;

    [SerializeField]
    private float minTimeBeforeDirectionChange;
    [SerializeField]
    private float maxTimeBeforeDirectionChange;

    private float currentSpeed;
    private float nextDirectionChangeTime;
    
    private Quaternion targetRotation = Quaternion.identity;

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
        stateMachine.AddState((int)Relationship.Acquaintance, null, null, null);
        stateMachine.AddState((int)Relationship.Friend, null, null, null);
        stateMachine.AddState((int)Relationship.ReceivingEncouragement, null, null, null);
        stateMachine.AddState((int)Relationship.CloseFriend, null, null, null);

        // Initialize the NPC with random values so it behaves differently fromt other NPCs
        transform.rotation = GetRandom2DRotation();
        nextDirectionChangeTime = GetNextDirectionChangeTime();

        // TODO: Change speed randomly throughout the game?
        currentSpeed = Random.Range(minSpeed, maxSpeed);
    }

    private void Start()
    {
        // Initialize the state machine
        stateMachine.Initialize((int)Relationship.Stranger);
    }

    // All physics calculations should always be done in FixedUpdate
    private void FixedUpdate()
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime* rotationalSpeed);
        rb.MovePosition(Position2D + (Right2D * currentSpeed * Time.deltaTime)); 

        if (Time.time > nextDirectionChangeTime)
        {
            targetRotation = GetRandom2DRotation();
            nextDirectionChangeTime = GetNextDirectionChangeTime();
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
                
                // Debug
                spriteRenderer.color = Color.black;
            }
        }
    }

    #endregion

    #region Helpers

    private float GetNextDirectionChangeTime()
    {
        return Time.time + Random.Range(minTimeBeforeDirectionChange, maxTimeBeforeDirectionChange);
    }

    public Quaternion GetRandom2DRotation()
    {
        // TODO: Don't completely randomize it, factor in the edges of the screen and player position too
        return Quaternion.Euler(new Vector3(0f, 0f, Random.Range(0f, 360f)));
    }

    #endregion
}
