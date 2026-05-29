using System.Collections;
using System.Collections.Generic;
using UnityEngine;


 
    public class setCorrectPhaseFromHere : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Drag and drop the NWT_Prefab of the character you want to control the speaking of, onto this slot.")]
        GameObject nowWereTalkingPrefabOnCharacterToSpeak;  //reference to where this script is going to send messages to to set the correct phase



        void Update()
        {
            setAnExistingPhase();
        }



        void setAnExistingPhase()
        {

            if (Input.GetKeyDown("0"))  // first phase  - this input is just for a simple way to test changing the phases - you send the message from your own code to establish which phase is ready to play
                                        // also keep in mind only 1 phase can be set at a time, so all other phases will be set to false.
            {
                nowWereTalkingPrefabOnCharacterToSpeak.SendMessage("setTheCorrectPhase", "GivePlayerQuest001");  // insert your own NPC_Phase names here as stirngs in "" marks - as shown
                return;
            }

            if (Input.GetKeyDown("1"))  // second phase
            {
                nowWereTalkingPrefabOnCharacterToSpeak.SendMessage("setTheCorrectPhase", "PlayerFailsQuest001");
                return;
            }

            if (Input.GetKeyDown("2"))  // third phase
            {
                nowWereTalkingPrefabOnCharacterToSpeak.SendMessage("setTheCorrectPhase", "PlayerSucceedsQuest001");
                return;

            }

            if (Input.GetKeyDown("3"))  // fourth phase
            {
                nowWereTalkingPrefabOnCharacterToSpeak.SendMessage("setTheCorrectPhase", "PlayerIgnoresQuest001");
                return;
            }

        }
   }
