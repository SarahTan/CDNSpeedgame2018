using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCManager : Singleton<NPCManager> {

    [SerializeField]
    private int maxNumberOfNPCs;
    [SerializeField]
    private NPC NPCPrefab;

    private List<NPC> allNPCs = new List<NPC>();

    private void Start()
    {
        while(allNPCs.Count < maxNumberOfNPCs)
        {
            var npc = Instantiate(NPCPrefab);
            npc.transform.parent = transform;
            allNPCs.Add(npc);
        }
    }
}
