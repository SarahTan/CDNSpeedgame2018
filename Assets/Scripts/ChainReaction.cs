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
    private TextMeshPro bonusEnergyText;
    [SerializeField]
    private GameObject bonusEnergySprite;
    
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private float visabilityDuration;

    private float endTime = 0;

    public int HitCount { get; private set; }
    public int EncouragedCount { get; private set; }
    public float BonusEnergy { get; private set; }

    private static readonly int _ANIMATION_ID = Animator.StringToHash("");

    public void Initialize()
    {
        HitCount = 1;
        EncouragedCount = 1;
        BonusEnergy = 0;
        endTime = Time.time + visabilityDuration;

        UpdateText();
        AudioManager.Instance.PlayChainReactionPopSFX();

        StartCoroutine(RunWaitForChainReactionEnd());
    }

    public void AddHit(bool isEncouraged, float newEnergy)
    {
        AudioManager.Instance.PlayChainReactionPopSFX();

        HitCount++;

        if (isEncouraged)
        {
            EncouragedCount++;
            BonusEnergy = newEnergy;
            endTime = Time.time + visabilityDuration;
        }

        UpdateText();
    }

    private void UpdateText()
    {
        hitCountText.text = string.Format("{0} hits!", HitCount);
        encouragedCountText.text = string.Format("Encouraged x {0}", EncouragedCount);

        if (Mathf.RoundToInt(BonusEnergy) > 0)
        {
            bonusEnergySprite.SetActive(true);
            bonusEnergyText.gameObject.SetActive(true);
            bonusEnergyText.text = string.Format("+ Bonus {0}", Mathf.RoundToInt(BonusEnergy));
        }
        else
        {
            bonusEnergySprite.SetActive(false);
            bonusEnergyText.gameObject.SetActive(false);
        }
    }

    private IEnumerator RunWaitForChainReactionEnd()
    {
        while(Time.time < endTime - 0.3f)
        {
            yield return null;
        }

        if (BonusEnergy > 0)
        {
            AudioManager.Instance.PlayChainReactionEndSFX();
        }

        while (Time.time < endTime)
        {
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
