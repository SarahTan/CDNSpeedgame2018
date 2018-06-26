using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    private ChainReaction chainReactionPrefab;

    [SerializeField]
    private Slider energyBar;
    [SerializeField]
    private Animator energyBarAnimator;

    [SerializeField]
    private GameObject gameOverScreen;
    [SerializeField]
    private TextMeshProUGUI gameOverText;
    [SerializeField]
    private TextMeshProUGUI statsCount;

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
    private int energyPerNPC;
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
    [SerializeField]
    private float lowEnergyThreshold;

    [SerializeField]
    private float maxBonusCount;
    [SerializeField]
    private AnimationCurve energyBonusMultiplierCurve;

    #endregion

    private Rect spawningRect;

    private List<NPC> inactiveNPCs = new List<NPC>();    
    private List<NPC> activeNPCs = new List<NPC>();

    private StateMachine stateMachine = new StateMachine();

    // Each item represents the number of hits in that chain reaction
    private List<ChainReaction> chainReactions = new List<ChainReaction>();

    #region Stats

    private float gameStartTime;
    private int largestHitChain;
    private int largestEncouragedChain;
    private float timeSpentInChurch;

    #endregion

    #endregion

    #region Animation

    private static readonly int IN_CHURCH_ANIMATION_ID = Animator.StringToHash("InChurch");
    private static readonly int ENERGY_LEVEL_ANIMATION_ID = Animator.StringToHash("EnergyLevel");

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
                energyBarAnimator.SetInteger(ENERGY_LEVEL_ANIMATION_ID, (int)value);

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
        stateMachine.AddState((int)GameState.PreGame, null, null, null, null);
        stateMachine.AddState((int)GameState.GameRunning, OnEnterGameRunningState, null, null, null);
        stateMachine.AddState((int)GameState.PostGame, OnEnterPostGameState, null, null, null);

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
        NPC.WrongActionEvent += OnNPCWrongAction;

        stateMachine.EnterState((int)GameState.GameRunning);
    }

    private void OnEnterGameRunningState(int previousState)
    {
        AudioManager.Instance.StartBGM();

        gameStartTime = Time.time;

        CurrentEnergy = startingEnergy;

        gameOverScreen.SetActive(false);

        // Start the spawning corouting
        StartCoroutine(RunSpawnNPC());

        // Start draining the energy
        StartCoroutine(RunCalculateEnergy());
    }

    private void OnEnterPostGameState(int previousState)
    {

        while (activeNPCs.Last() != null)
        {
            activeNPCs.Last().Deactivate();
        }

        gameOverScreen.SetActive(true);

        var win = CurrentEnergy >= energyToWin;
        AudioManager.Instance.PlayGameEndSFX(win);
        gameOverText.text =  win ? "You Win!" : gameOverText.text = "You Lose :(";
        
        var timeTaken = Time.time - gameStartTime;
        statsCount.text = string.Format("{0}m {1}s\n{2}m {3}s\n{4} Hits\n{5} Encouraged",
                                        (int)timeTaken/60,
                                        (int)timeTaken%60,
                                        (int)timeSpentInChurch/60,
                                        (int)timeSpentInChurch%60, 
                                        largestHitChain,
                                        largestEncouragedChain);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        NPC.EnterInactiveStateEvent -= OnNPCEnterInactiveState;
        NPC.WrongActionEvent += OnNPCWrongAction;
    }

    #endregion

    #region Chain Reactions

    // Returns the index of the current chain reaction
    public int StartChainReaction(Vector2 position)
    {
        CurrentEnergy += energyPerNPC;
        oldEnergy = energyPerNPC;

        // Create the chain reaction
        var chainReaction = Instantiate(chainReactionPrefab);
        chainReaction.transform.position = new Vector3(Mathf.Clamp(position.x, -7f, 7f), Mathf.Clamp(position.y + 2f, -4f, 3.5f), -1f);

        // Initialize it and add it to the list
        chainReaction.Initialize();
        chainReactions.Add(chainReaction);

        return chainReactions.Count - 1;
    }

    private float oldEnergy = 0;
    public void AddToChainReaction(int index, bool isEncouraged)
    {
        if(index >= 0 && chainReactions.Count > index)
        {            
            if (isEncouraged)
            {
                var encouragedCount = chainReactions[index].EncouragedCount + 1;

                // Clamp and normalize the bonus count
                var bonusCount = Mathf.Clamp(encouragedCount, 0, maxBonusCount) / maxBonusCount;
                // Get the multiplier based on the count
                var bonusMultiplier = energyBonusMultiplierCurve.Evaluate(bonusCount);

                // Calculate the total bonus energy from this chain reaction so far
                var totalBonusEnergy = (encouragedCount * energyPerNPC * Mathf.Clamp(bonusMultiplier, 0, 1));

                // Calculate the total energy gained so far
                var newEnergy = totalBonusEnergy + (encouragedCount * energyPerNPC);

                // Add to CurrentEnergy
                CurrentEnergy += (newEnergy - oldEnergy);
                oldEnergy = newEnergy;

                // Update the chain reaction
                chainReactions[index].AddHit(true, totalBonusEnergy);

                if (chainReactions[index].EncouragedCount > largestEncouragedChain)
                {
                    largestEncouragedChain = chainReactions[index].EncouragedCount;
                }
            }
            else
            {
                chainReactions[index].AddHit(false, -1);
            }

            if(chainReactions[index].HitCount > largestHitChain)
            {
                largestHitChain = chainReactions[index].HitCount;
            }
        }
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
            energyBarAnimator.SetBool(IN_CHURCH_ANIMATION_ID, Player.Instance.IsInChurch);

            if (Player.Instance.IsInChurch)
            {
                timeSpentInChurch += 0.1f;
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
