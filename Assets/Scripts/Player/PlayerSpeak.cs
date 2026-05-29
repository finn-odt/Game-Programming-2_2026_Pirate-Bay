using System.Collections;
using UnityEngine;
using UnityEngine.Localization.SmartFormat.Utilities;
using UnityEngine.InputSystem;

public class PlayerSpeak : MonoBehaviour
{

    public GameObject prefabOnCharacterToSpeak;
    public bool talkTriggerFound = false;

    public GameObject trigger;

    
    private PlayerInput _playerInput;

    public void Go(GameObject trigger)
    {
        StartCoroutine(Wait(0.1f, trigger));
    }
    private IEnumerator Wait(float delay, GameObject trigger)
    {
        trigger.SetActive(true);
        yield return new WaitForSeconds(delay);
        trigger.SetActive(false);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject[] speakers = GameObject.FindGameObjectsWithTag("MouthSpeaker");
        foreach(GameObject speaker in speakers) {
            if(speaker.transform.IsChildOf(transform))
            {
                prefabOnCharacterToSpeak = speaker;
                talkTriggerFound = true;
                prefabOnCharacterToSpeak.SendMessage("setTheCorrectPhase", "GivePlayerQuest001");
                break;
            }
        }

        foreach (Transform child in prefabOnCharacterToSpeak.transform)
        {
            if(child.tag == "SpeakTrigger")
            {
                trigger = child.gameObject;
                break;
            }
        }

        _playerInput = GetComponent<PlayerInput>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!talkTriggerFound || prefabOnCharacterToSpeak == null || trigger == null)
            return;

    }

    public void OnSpeak()
    {
        Go(trigger);
    }
}
