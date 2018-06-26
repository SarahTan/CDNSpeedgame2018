using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : Singleton<AudioManager> {

    [SerializeField]
    private AudioSource bgmAudioSource;
    [SerializeField]
    private AudioSource audioSource1;
    [SerializeField]
    private AudioSource audioSource2;

    [SerializeField]
    private AudioClip chainReactionPopSFX;
    [SerializeField]
    private AudioClip chainReactionEndSFX;

    [SerializeField]
    private AudioClip winGameSFX;
    [SerializeField]
    private AudioClip loseGameSFX;

    public void StartBGM()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = 0.3f;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }
    }

    public void PlayChainReactionPopSFX()
    {
        if (audioSource1 != null && chainReactionPopSFX != null)
        {
            audioSource1.clip = chainReactionPopSFX;
            audioSource1.Play();
        }
    }

    public void PlayChainReactionEndSFX()
    {
        if (audioSource2 != null && chainReactionEndSFX != null)
        {
            audioSource2.clip = chainReactionEndSFX;
            audioSource2.Play();
        }
    }

    public void PlayGameEndSFX(bool win)
    {
        bgmAudioSource.volume /= 2;
        if (audioSource1 != null)
        {
            if(win && winGameSFX != null)
            {
                audioSource1.clip = winGameSFX;
                audioSource1.Play();
            }
            else if(!win && loseGameSFX != null)
            {
                audioSource1.clip = loseGameSFX;
                audioSource1.Play();
            }
        }
    }
}
