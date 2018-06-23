using System.Collections.Generic;

public class StateMachine
{
    #region Delegates and Events

    public delegate void OnStateChange(int currentStateId, int previousStateId);
    public delegate void StateTransition(int otherStateId);
    public delegate void StateUpdate();

    public event OnStateChange StateChangeEvent = null;

    #endregion
    
    #region Fields

    public const int INVALID_STATE_ID = -1;

    private Dictionary<int, State> states = new Dictionary<int, State>();

    private StateUpdate cachedStateOnUpdate;
    private StateUpdate cachedStateOnFixedUpdate;

    #endregion

    #region Properties

    public int CurrentStateId { get; private set; }
    public int PreviousStateId { get; private set; }

    #endregion       
    
    #region Public Methods and Constructor

    public StateMachine()
    {
        PreviousStateId = INVALID_STATE_ID;
        CurrentStateId = INVALID_STATE_ID;
    }

    public void Initialize(int defaultStateId)
    {
        RunStateTransition(defaultStateId);
    }

    public void AddState(int stateId, StateTransition onEnter, StateTransition onExit, StateUpdate onUpdate, StateUpdate onFixedUpdate)
    {
        if (!states.ContainsKey(stateId))
        {
            states.Add(stateId, new State()
            {
                OnEnter = onEnter,
                OnExit = onExit,
                OnUpdate = onUpdate,
                OnFixedUpdate = onFixedUpdate
            });
        }
    }

    public void EnterState(int nextStateId)
    {
        RunStateTransition(nextStateId);
    }

    public void Update()
    {
        if (cachedStateOnUpdate != null)
        {
            cachedStateOnUpdate();
        }
    }

    public void FixedUpdate()
    {
        if (cachedStateOnFixedUpdate != null)
        {
            cachedStateOnFixedUpdate();
        }
    }

    #endregion

    private State GetState(int stateId)
    {
        // Try to retrieve the state from the dictionary
        State state = null;
        if (stateId != INVALID_STATE_ID)
        {
            if(!states.TryGetValue(stateId, out state))
            {
                UnityEngine.Debug.LogError("Missing stateId " + stateId);
            }
        }
        return state;
    }

    private void RunStateTransition(int nextStateId)
    {
        // If it's not actually changing states, early out
        if(nextStateId == CurrentStateId)
        {
            return;
        }

        // Call OnExit of the current state
        State currentState = GetState(CurrentStateId);
        if (currentState != null && currentState.OnExit != null)
        {
            currentState.OnExit(nextStateId);
        }

        // Update the current and previous states
        PreviousStateId = CurrentStateId;
        CurrentStateId = nextStateId;

        // Enter the next state
        State nextState = GetState(CurrentStateId);
        if (nextState != null)
        {
            // Cache the OnUpdate and OnFixedUpdate method so we don't have to keep getting it
            cachedStateOnUpdate = nextState.OnUpdate;
            cachedStateOnFixedUpdate = nextState.OnFixedUpdate;

            // Call OnEnter
            var onEnter = nextState.OnEnter;
            if (onEnter != null)
            {
                onEnter(PreviousStateId);
            }
        }
        else
        {
            // Clear the cached OnUpdate  and OnFixedUpdate which might have been set for the previous state
            cachedStateOnUpdate = null;
            cachedStateOnFixedUpdate = null;
        }
                        
        // Broadcast the state change
        if (StateChangeEvent != null)
        {
            StateChangeEvent(CurrentStateId, PreviousStateId);
        }
    }
    
    #region Class

    private class State
    {
        public StateTransition OnEnter = null;
        public StateTransition OnExit = null;
        public StateUpdate OnUpdate = null;
        public StateUpdate OnFixedUpdate = null;
    }

    #endregion
}
