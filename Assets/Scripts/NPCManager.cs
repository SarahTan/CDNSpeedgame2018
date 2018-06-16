using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCManager : Singleton<NPCManager> {

    #region Fields

    [SerializeField]
    private int maxNumberOfNPCs;
    [SerializeField]
    private NPC NPCPrefab;

    private List<NPC> allNPCs = new List<NPC>();

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

    private void Start()
    {
        // Instantiate all the NPCs
        while(allNPCs.Count < maxNumberOfNPCs)
        {
            var npc = Instantiate(NPCPrefab);
            npc.transform.position = GetRandomPositionOnScreen();
            npc.transform.parent = transform;
            allNPCs.Add(npc);
        }
    }

    #endregion

    #region Helpers

    private Vector2 GetRandomPositionOnScreen()
    {
        var viewPortPos = new Vector2(Random.value, Random.value);
        return MainCamera.ViewportToWorldPoint(viewPortPos);
    }

    #endregion
}
