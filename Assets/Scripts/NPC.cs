﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Relationship = GameManager.Relationship;

public class NPC : MonoBehaviour {

    #region Events

    public static System.Action<NPC> EnterInactiveStateEvent = null;
    public static System.Action<NPC> WrongActionEvent = null;

    #endregion

    #region Fields

    [Header("References")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;
    [SerializeField]
    private Rigidbody2D rb;
    [SerializeField]
    private GameObject keyVisual;
    [SerializeField]
    private TextMeshPro keyText;
    [SerializeField]
    private GameObject aoe;
    [SerializeField]
    private Animator animator;

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
    private KeyCode[] keys = new KeyCode[0];

    private Vector2 currentDirection = Vector2.zero;
    private float currentSpeed = 0f;
    private float nextDirectionChangeTime = 0f;

    private float acquaintanceStateExitTime = 0f;
    private float pauseMovementDuration = 0.5f;
    private float pauseMovementEndTime = 0.5f;

    private KeyCode associatedKey = KeyCode.None;

    private bool shouldFollowPlayer = false;

    private StateMachine stateMachine = new StateMachine();

    private Vector2 savedVelocity = Vector2.zero;

    private int currentChainReactionIndex = -1;

    #endregion

    #region Animation

    private static readonly int REALTIONSHIP_ANIMATION_ID = Animator.StringToHash("Relationship");
    private static readonly int REALTIONSHIP_CHANGE_ANIMATION_ID = Animator.StringToHash("RelationshipChange");

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
        stateMachine.AddState((int)Relationship.Stranger, OnEnterStrangerState, null, null, OnFixedUpdateStrangerState);
        stateMachine.AddState((int)Relationship.Acquaintance, OnEnterAcquaintanceState, null, OnUpdateAcquaintanceState, OnFixedUpdateAcquaintanceState);
        stateMachine.AddState((int)Relationship.Friend, OnEnterFriendState, OnExitFriendState, null, OnFixedUpdateFriendState);
        stateMachine.AddState((int)Relationship.ReceivingEncouragement, OnEnterReceivingEncourgaementState, OnExitReceivingEncourgaementState, null, null);
        stateMachine.AddState((int)Relationship.CloseFriend, null, null, null, OnFixedUpdateCloseFriendState);
        stateMachine.AddState((int)Relationship.Inactive, OnEnterInactiveState, null, null, null);

        // Listen for state changes so we can animate
        stateMachine.StateChangeEvent += StateMachine_StateChangeEvent;

        // Immediately deactivate it
        stateMachine.Initialize((int)Relationship.Inactive);
    }

    private void OnDestroy()
    {
        stateMachine.StateChangeEvent -= StateMachine_StateChangeEvent;
    }

    private void StateMachine_StateChangeEvent(int currentStateId, int previousStateId)
    {
        if (currentStateId != (int)Relationship.CloseFriend)
        {
            pauseMovementEndTime = Time.time + pauseMovementDuration;
            animator.SetTrigger(REALTIONSHIP_CHANGE_ANIMATION_ID);
        }
        animator.SetInteger(REALTIONSHIP_ANIMATION_ID, stateMachine.CurrentStateId);
    }

    // All physics calculations should always be done in FixedUpdate
    private void FixedUpdate()
    {
        if (Time.time > pauseMovementEndTime)
        {
            // Resume movement
            if (rb.isKinematic)
            {
                rb.isKinematic = false;
                rb.velocity = savedVelocity;
            }

            // Get the new direction
            if (Time.time > nextDirectionChangeTime)
            {
                currentSpeed = Random.Range(minSpeed, maxSpeed);
                currentDirection = UnityExtensions.GetRandomUnitVector();
                nextDirectionChangeTime = GetNextDirectionChangeTime();
            }

            // Call each of the state's FixedUpdates
            stateMachine.FixedUpdate();
            
            // Clamp the velocity
            if(rb.velocity.sqrMagnitude > maxSpeed * maxSpeed)
            {
                rb.velocity = Vector2.ClampMagnitude(rb.velocity, maxSpeed);
            }
        }
        else
        {
            // Pause movement
            savedVelocity = rb.velocity;
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
    }

    private void Update()
    {
        stateMachine.Update();
    }

    private void OnTriggerEnter2D(Collider2D otherCollider)
    {
        var npc = otherCollider.GetComponent<NPC>();
        if (npc != null && currentChainReactionIndex != -1)
        {
            npc.ChainReaction(currentChainReactionIndex);
        }
    }

    #endregion

    public void HitByPlayer()
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
                    currentChainReactionIndex = GameManager.Instance.StartChainReaction(transform.position);
                }
                else
                {
                    WrongActionEvent.SafeRaise(this);
                    stateMachine.EnterState((int)Relationship.Acquaintance);
                }
            }
        }
    }

    #region States
    
    private void ChainReaction(int index)
    {
        // Stranger, Aquaintance, Friend
        if(stateMachine.CurrentStateId <= 2 && index != currentChainReactionIndex)
        {
            currentChainReactionIndex = index;
            var nextStateId = stateMachine.CurrentStateId + 1;
            GameManager.Instance.AddToChainReaction(currentChainReactionIndex, nextStateId == (int)Relationship.ReceivingEncouragement);
            stateMachine.EnterState(nextStateId);
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

    #region Stranger

    private void OnEnterStrangerState(int previousStateId)
    {
        if (previousStateId == (int)Relationship.Inactive)
        {
            gameObject.SetActive(true);

            // Give the NPC with random values so it behaves differently fromt other NPCs
            nextDirectionChangeTime = GetNextDirectionChangeTime();
            currentDirection = UnityExtensions.GetRandomUnitVector();
            currentSpeed = Random.Range(minSpeed, maxSpeed);
        }
    }

    private void OnFixedUpdateStrangerState()
    {
        rb.AddForce((currentDirection) * currentSpeed);
    }

    #endregion

    #region Acquaintance

    private void OnEnterAcquaintanceState(int previousStateId)
    {
        acquaintanceStateExitTime = Time.time + Random.Range(minTimeBeforeFriendTransition, maxTimeBeforeFriendTransition);
    }

    private void OnUpdateAcquaintanceState()
    {
        // Only transition the state when the player can see it
        if(Time.time > acquaintanceStateExitTime && MainCamera.IsObjectVisible(spriteRenderer.bounds))
        {
            stateMachine.EnterState((int)Relationship.Friend);
        }
    }
    
    private void OnFixedUpdateAcquaintanceState()
    {
        rb.AddForce((currentDirection) * currentSpeed);
    }

    #endregion

    #region Friend

    private void OnEnterFriendState(int previousStateId)
    {
        associatedKey = keys[Random.Range(0, keys.Length)];
        keyText.text = associatedKey.ToString();
        keyVisual.SetActive(true);
    }

    private void OnExitFriendState(int previousStateId)
    {
        keyText.text = string.Empty;
        keyVisual.SetActive(false);
    }

    private void OnFixedUpdateFriendState()
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

    #endregion

    #region Receiving Encouragement

    private void OnEnterReceivingEncourgaementState(int previousStateId)
    {
        // NPC pauses in place while receiving encouragement
        //savedVelocity = rb.velocity;
        //rb.velocity = Vector2.zero;
        //rb.isKinematic = true;
    }

    private void FinishedReceivingEncouragement()
    {
        stateMachine.EnterState((int)Relationship.CloseFriend);
    }

    private void OnExitReceivingEncourgaementState(int previousStateId)
    {
        rb.isKinematic = false;

        // Set to max speed because:
        // 1. Velocity magnitude might be close to 0 and will take awhile to increase back up
        // 2. We want the NPC to quickly exit the screen and get recycled
        rb.velocity = savedVelocity.normalized*maxSpeed;
    }

    #endregion

    #region Close Friend

    private void OnFixedUpdateCloseFriendState()
    {
        // Use max speed as we want it to quickly get out of the screen to be recycled
        rb.AddForce((currentDirection) * maxSpeed);

        // Deactivate the NPC once it's no longer visible
        if (!MainCamera.IsObjectVisible(spriteRenderer.bounds))
        {
            stateMachine.EnterState((int)Relationship.Inactive);
        }
    }

    #endregion

    #region Inactive

    private void OnEnterInactiveState(int previousStateId)
    {
        // Reset everything
        gameObject.SetActive(false);
        keyVisual.SetActive(false);
        aoe.SetActive(false);

        EnterInactiveStateEvent.SafeRaise(this);
    }

    #endregion

    #endregion

    #region Helpers

    private float GetNextDirectionChangeTime()
    {
        return Time.time + Random.Range(minTimeBeforeDirectionChange, maxTimeBeforeDirectionChange);
    }

    #endregion
}
