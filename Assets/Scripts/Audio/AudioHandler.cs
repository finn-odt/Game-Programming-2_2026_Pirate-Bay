using GameEvents;
using UnityEngine;

public class AudioHandler : MonoBehaviour
{
    void OnEnable()
    {
        GameEventManager.AddListener<OneShotAudioEvent>(OnOneShotAudioRequest);

    }

    void OnDisable()
    {
        GameEventManager.RemoveListener<OneShotAudioEvent>(OnOneShotAudioRequest);
    }

    private void OnOneShotAudioRequest(OneShotAudioEvent e)
    {
        AudioSource.PlayClipAtPoint(e.clip, e.position, e.volume);
    }
}
