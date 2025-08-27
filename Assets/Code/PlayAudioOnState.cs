using UnityEngine;
using System.Collections.Generic;

public class PlayAudioOnState : StateMachineBehaviour
{
    private AudioSource audioSource;

    // Mapping nama state -> audio clip
    public AudioClip idleClip;
    public AudioClip greetingClip;
    public AudioClip angryClip;
    public AudioClip thankfulClip;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (audioSource == null)
            audioSource = animator.GetComponent<AudioSource>();

        AudioClip clip = null;

        // Cek nama state
        if (stateInfo.IsName("Idle")) clip = idleClip;
        else if (stateInfo.IsName("Greeting")) clip = greetingClip;
        else if (stateInfo.IsName("Angry")) clip = angryClip;
        else if (stateInfo.IsName("Thankful")) clip = thankfulClip;

        if (clip != null && audioSource != null)
        {
            audioSource.Stop(); // hentikan audio sebelumnya
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
