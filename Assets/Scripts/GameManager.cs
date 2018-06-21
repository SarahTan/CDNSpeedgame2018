using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{

    #region Enums

    public enum Relationship
    {
        Stranger = 0,
        Acquaintance = 1,
        Friend = 2,
        ReceivingEncouragement = 3,
        CloseFriend = 4,
        Inactive = 5
    }

    public enum GameState
    {
        PreGame = 0,
        GameRunning = 1,
        PostGame = 2
    }

    #endregion

    #region Fields

    #region References

    [Header("References")]
    [SerializeField]
    private NPC NPCPrefab;

    [SerializeField]
    private Slider energyBar;

    [SerializeField]
    private GameObject gameOverScreen;
    [SerializeField]
    private Text gameOverText;

    #endregion

    #region Tunables

    [SerializeField]
    private int maxNumberOfActiveNPCs;
    
    [Header("Spawning")]
    [SerializeField]
    private float NPCSpawnFrequency;
    [SerializeField]
    private Transform spawningRectMin;
    [SerializeField]
    private Transform spawningRectMax;

    [Header("Energy")]
    [SerializeField]
    private int closeFriendEnergy;
    [SerializeField]
    private int wrongActionEnergy;
    [SerializeField]
    private int energyToWin;
    [SerializeField]
    private float energyDrainRate;
    [SerializeField]
    private float energyGainRate;
    [SerializeField]
    private float startingEnergy;

    #endregion

    private Rect spawningRect;

    private List<NPC> inactiveNPCs = new List<NPC>();    
    private List<NPC> activeNPCs = new List<NPC>();

    private StateMachine stateMachine = new StateMachine();

    #endregion

    #region Properties

    private float _currentEnergy = -1;
    private float CurrentEnergy
    {
        get { return _currentEnergy; }
        set
        {
            if(value != CurrentEnergy)
            {
                _currentEnergy = value;

                if (value > energyToWin || value < 1)
                {
                    stateMachine.EnterState((int)GameState.PostGame);
                }
                else
                {
                    energyBar.normalizedValue = value / energyToWin;
                }
            }
        }
    }


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
        stateMachine.AddState((int)GameState.PreGame, null, null, null);
        stateMachine.AddState((int)GameState.GameRunning, OnEnterGameRunningState, null, null);
        stateMachine.AddState((int)GameState.PostGame, OnEnterPostGameState, null, null);

        // Set up the spawning rect
        if (spawningRectMin != null && spawningRectMin != null)
        {
            spawningRect = new Rect(spawningRectMin.position, spawningRectMax.position - spawningRectMin.position);
        }

        // Instantiate all the NPCs
        while (inactiveNPCs.Count < maxNumberOfActiveNPCs)
        {
            var npc = Instantiate(NPCPrefab);
            npc.transform.parent = transform;
            inactiveNPCs.Add(npc);
#if UNITY_EDITOR
            npc.gameObject.name += inactiveNPCs.Count;
#endif
        }

        // Listen for NPC events
        NPC.EnterInactiveStateEvent += OnNPCEnterInactiveState;
        NPC.EnterCloseFriendStateEvent += OnNPCEnterCloseFriendState;
        NPC.WrongActionEvent += OnNPCWrongAction;

        stateMachine.EnterState((int)GameState.GameRunning);
    }

    private void OnEnterGameRunningState(int previousState)
    {
        CurrentEnergy = startingEnergy;

        gameOverScreen.SetActive(false);

        // Start the spawning corouting
        StartCoroutine(RunSpawnNPC());

        // Start draining the energy
        StartCoroutine(RunCalculateEnergy());
    }

    private void OnEnterPostGameState(int previousState)
    {
        while(activeNPCs.Last() != null)
        {
            activeNPCs.Last().Deactivate();
        }

        gameOverScreen.SetActive(true);

        if (CurrentEnergy >= energyToWin)
        {
            gameOverText.text = "You Win!";
        }
        else
        {
            gameOverText.text = "You Lose :(";
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        NPC.EnterInactiveStateEvent -= OnNPCEnterInactiveState;
        NPC.EnterCloseFriendStateEvent -= OnNPCEnterCloseFriendState;
        NPC.WrongActionEvent += OnNPCWrongAction;
    }

    #endregion

    private void OnNPCWrongAction(NPC npc)
    {
        if (activeNPCs.Contains(npc))
        {
            CurrentEnergy -= Mathf.Abs(wrongActionEnergy);
        }
        else
        {
            Debug.LogError(string.Format("{0} not found in activeNPC list!", npc.name));
        }
    }

    private void OnNPCEnterCloseFriendState(NPC npc)
    {
        if (activeNPCs.Contains(npc))
        {
            CurrentEnergy += closeFriendEnergy;
        }
        else
        {
            Debug.LogError(string.Format("{0} not found in activeNPC list!", npc.name));
        }
    }

    private void OnNPCEnterInactiveState(NPC npc)
    {
        // Try to move the NPC from the active to inactive list
        if (activeNPCs.Remove(npc))
        {
            inactiveNPCs.Add(npc);
        }
        else
        {
            Debug.LogError(string.Format("{0} not found in activeNPC list!", npc.name));
        }
    }

    private IEnumerator RunCalculateEnergy()
    {
        while (stateMachine.CurrentStateId == (int)GameState.GameRunning)
        {
            if (!Player.Instance.IsInChurch || CurrentEnergy >= startingEnergy)
            {
                CurrentEnergy -= (energyDrainRate / 10f);
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator RunSpawnNPC()
    {
        while (stateMachine.CurrentStateId == (int)GameState.GameRunning)
        {
            if (activeNPCs.Count < maxNumberOfActiveNPCs && inactiveNPCs.Count > 0)
            {
                var npc = inactiveNPCs.RemoveAndGetItem(0);
                activeNPCs.Add(npc);
                npc.transform.position = GetRandomSpawnPoint();
                npc.Activate();
            }
            yield return new WaitForSeconds(NPCSpawnFrequency);
        }
    }

    #region Buttons

    public void Button_PlayAgain()
    {
        stateMachine.EnterState((int)GameState.GameRunning);
    }

    #endregion

    #region Helpers

    private Vector2 GetRandomSpawnPoint()
    {
        var value = Random.value;
        if(value < 0.25f)
        {
            return new Vector2(spawningRect.xMin, Random.Range(spawningRect.yMin, spawningRect.yMax));
        }
        else if(value < 0.5f)
        {
            return new Vector2(spawningRect.xMax, Random.Range(spawningRect.yMin, spawningRect.yMax));
        }
        else if (value < 0.75f)
        {
            return new Vector2(Random.Range(spawningRect.xMin, spawningRect.xMax), spawningRect.yMin);
        }
        else
        {
            return new Vector2(Random.Range(spawningRect.xMin, spawningRect.xMax), spawningRect.yMax);
        }
    }
    
    #endregion
}
