using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ChainReaction : MonoBehaviour {

    [SerializeField]
    private TextMeshPro hitCountText;
    [SerializeField]
    private TextMeshPro encouragedCountText;
    [SerializeField]
    private TextMeshPro energyAddedText;
    
    [SerializeField]
    private Animator animator;
    

    private int energyAdded = 0;
    private float endTime = 0;

    public int HitCount { get; private set; }
    public int EncouragedCount { get; private set; }

    private readonly int _ANIMATION_ID = Animator.StringToHash("");
    
    public void Initialize(float endTime)
    {
        HitCount = 1;
        EncouragedCount = 1;
        energyAdded = 1;
        this.endTime = endTime;

        UpdateText();

        StartCoroutine(RunWaitForChainReactionEnd());
    }

    public void AddHit(bool isEncouragement, int newEnergy, float newEndTime)
    {
        HitCount++;
        // Animate

        if (isEncouragement)
        {
            EncouragedCount++;

            // Animate
        }

        energyAdded = newEnergy;
        // Animate

        endTime = newEndTime;
        UpdateText();
    }

    private void UpdateText()
    {
        hitCountText.text = string.Format("{0} hits!", HitCount);
        encouragedCountText.text = string.Format("Encouraged x {0}", EncouragedCount);
        energyAddedText.text = string.Format("+ {0}", energyAdded);
    }

    private IEnumerator RunWaitForChainReactionEnd()
    {
        Debug.LogError("endtime: " + endTime);
        while(Time.time < endTime)
        {
            Debug.Log(Time.time);
            yield return null;
        }
        Debug.LogError("destroyed: " + Time.time);

        // Animate out

        Destroy(gameObject);
    }
}
