using System.Collections;
using GameEvents;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.VFX;

public class CanonBehaviour : IInteractable
{

    [SerializeField] private ParticleSystem smokeEffect;
    [SerializeField] private VisualEffect destroyedEffect, explosionEffect;
    [SerializeField] private AudioClip canonSound, explosionSound;

    private IEnumerator DestroyCanon()
    {
        // wait for canon to be finished with shooting
        while(smokeEffect.isPlaying) {
            yield return null;
        }
        
        // trigger explosion
        explosionEffect.Play();
        GameEventManager.Raise(new OneShotAudioEvent(explosionSound, transform.position, 1f));
        yield return null;
        
        // wait for explosion
        while(explosionEffect.HasAnySystemAwake()) {
            yield return null;
        }

        // trigger smoke
        destroyedEffect.Play();
    }

    protected override void OnPlayerInteraction(PlayerInteractionRequestEvent e)
    {
        if(playerInTrigger) {
            canBeInteractedWith = false;
            
            // start interaction
            smokeEffect.Play();  // play one time
            GameEventManager.Raise(new OneShotAudioEvent(canonSound, transform.position, 1f));
            StartCoroutine(DestroyCanon());

            // deactivate interaction indicator
            GameEventManager.Raise(new InteractionPossibleEvent(false, gameObject));
        }
    }
}
