﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCManager : Singleton<NPCManager>
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

    #endregion

    #region Fields
    
    [SerializeField]
    private int maxNumberOfActiveNPCs;
    [SerializeField]
    private NPC NPCPrefab;

    [SerializeField]
    private float NPCSpawnFrequency;

    [Header("Spawning")]
    [SerializeField]
    private Transform spawningRectMin;
    [SerializeField]
    private Transform spawningRectMax;

    private Rect spawningRect;

    private List<NPC> inactiveNPCs = new List<NPC>();    
    private List<NPC> activeNPCs = new List<NPC>();

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

        // Listen for NPC deactivation events
        NPC.EnterInactiveStateEvent += OnNPCEnterInactiveState;

        // Start the spawning corouting
        StartCoroutine(RunSpawnNPC());
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        NPC.EnterInactiveStateEvent -= OnNPCEnterInactiveState;
    }

    #endregion

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

    private IEnumerator RunSpawnNPC()
    {
        while (true)
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
