using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

//**********************************************************************************************
//  Now We're Talking - Peter Martin Caddock - Chatterware 
//   
// - v1.08 released with the addition of using '++' in a voice clip filename to ensure the next voice clip in the list follows on immediately, so follow on voice clips. Last clip should have no '++'.
// - v1.07 released with some small ammends for the Dialogue System (Pixel Crushers) to enable use of NWT Phases.
// - v1.061 changed Adventure Creator bool into "externalAudioSourceUsed" for a more correct explanation of it.
// - V1.06 new features...   Added compatibility with Adventure Creator and external AudioSources - (use Adventure Creator boolean) and check to see if it's set and if not grab this prefabs AudioSource instead
// - V1.05 new features...   Beta version - testing microphone input + fix for mouth 3
// - v1.04 new features...   Pixel Crushers Dialogue System integrated - many thanks to Tony from Pixel Crushers! for integration of NWt into Dialogue System
//
// Many settings in this script depend on the quality of the voice clip being used.
// It is important for voice clips to be normalised, clean, crisp and clear and each clip
// should only contain one voice. (see Chatterware voice packs below).
// 
// Chatterware will be creating voice clip packs with preset data which work out of the box
// for a number of generic 'settings and scenarios' 
// Please join our Discord to make suggestions for voice pack content.
// https://discord.gg/TaRAvGn3HK
// 
//********************************************************************************************** 
//
// Don't forget to uncheck the 'Allow Mouth Data Presets' check box before you start or the script 
// will pull embedded data from the script each time you run the editor.
//
// Don't forget to copy your 'Now We're Talking!' script component settings so you can re-paste them into
// the 'Now We're Talking!' script component once you have stopped the Unity Editor.
//
// To tune a voice clip so the mouth moves giving the impression of speaking...
// From scratch this is a matter of slowly tuning settings until the fluidity and responsiveness of the
// mouth shapes matches the voice clips absoluetly so each nuance is visible in the mouth movement.
//
// The best way to begin is to use one of our presets, just to get you started, but if you are brave and want to 'go it alone'...
// set the audio source component to loop and run the unity editor
// Start tuning with the top six sliders on the left side. 
// Now set Mouth Responsiveness to the middle and move lipSpeed to the right a little - this slider changes the mouth
// shape responsiveness quickly (works in large steps).  Now try to slide the lipSpeedFine slider (works in small steps) towards the right
// - see the mouth change more slowly, it is a finer control.
//
// Now adjust the sizeFactor and the Min and Max sliders.  Try to get a 'feel' for how the
// sliders affect the mouth response.  The Min Size slider should normally be lower to the left of the Max Size slider.
//
// Getting a great looking response is a balance between all these sliders - your mouth shape should look like 
// it is fluidly responding to the many variations in the voice to give the right 'feel'.
//
// To ensure consistent voice clip quality, Chatterware will be releasing voice packs with lots of useful phrases and sentences.
// Within each voice pack - will be a data set which means the voices will work immediately out of the box, with Now We're Talking!
// The data presets will provide the very best and the quickest way to get set up to have your low polygon characters talking.
// 
//********************************************************************************************** 

namespace NWT
{   
    public class nowWereTalking : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Drag and drop the lipsPivot gameObject into this slot.")]
        GameObject lipsPivot;      // gameobject lipsPivot - this is the parent of the lips objects (see above) - move it into positon and rotate it
                                   // so the cross is on the surface of the face of the character in approxomately the correct position and rotation for a mouth

        [SerializeField]
        [Tooltip("Drag and drop the lipsCapsule from your NowWe'reTalking component into this slot.")]
        GameObject lipsCapsule;   // gameobject used for capsule mouth - no longer auto found

        [SerializeField]
        [Tooltip("Drag and drop the lipsCube from your NowWe'reTalking component into this slot.")]
        GameObject lipsCube;      // gameobject used for cube mouth - no longer auto found
        
        [SerializeField]
        [Tooltip("Drag and drop the lipsSphere from your NowWe'reTalking component into this slot.")]
        GameObject lipsSphere;      // gameobject used for cube mouth - no longer auto found

  
        bool toggleOnOff;  // toggle between centring and rotating to new positions

        [SerializeField]
        [Tooltip("Drag and drop the Eyes object (mesh) from your character into this slot.")]
        GameObject myEyes; // the eyes object on Synty or other low poly faces - blinking eyes and dynamic eyebrow movements are both optional.

        [SerializeField]
        [Tooltip("Drag and drop the EyesBrows object (mesh) from your character into this slot.")]
        GameObject myEyeBrows;// included for those characters which have expressive eyebrows!

        float eyesScaleY; // how open are the eyes?
        float eyesBrowsScaleY;// how eyebrows will be scaled
        int eyeCount;  // timer for opening and closing eyes

        Vector3 eyesMaxScale = new Vector3(1.0f, 1.0f, 1.0f);

        [SerializeField]
        [Tooltip("Drag your character's Audio Source here (It should be just above this component).  Make sure the audio clip you use is a clean, normalised speech audio clip. 'Noisy' silence will affect the quailty of the responsiveness.")]
        AudioSource audioSource;  // Drag and drop the character's audio source reference here. (It will normally be just above the 'Now We're Talking!' script component

        int eyeRandom;

       // lips surround each mouth shapes
       [SerializeField]
        [Tooltip("This checkbox is for if you wish to have lips on mouth shape 1.")]
        bool mouth001Lips;  // checked = yes unchecked = no

        [SerializeField]
        [Tooltip("This checkbox is for if you wish to have lips on mouth shape 2.")]
        bool mouth002Lips;  // checked = yes unchecked = no

        [SerializeField]
        [Tooltip("This checkbox is for if you wish to have lips on mouth shape 3.")]
        bool mouth003Lips;  // checked = yes unchecked = no

        [SerializeField]
        [Tooltip("Drag the mouth001Lips object into this slot.")]
        GameObject lipsmouth001Lips;

        [SerializeField]
        [Tooltip("Drag the mouth002Lips object into this slot.")]
        GameObject lipsmouth002Lips;

        [SerializeField]
        [Tooltip("Drag the mouth003Lips object into this slot.")]
        GameObject lipsmouth003Lips;

        [SerializeField]
        [Tooltip("Adjust this value to balance with the two responsiveness values above. Try starting with low values, try to get a flickering effect then use FINE above to reduce the flicker to your liking.")]
        [Range(2, 512)]
        int mouthResponsiveness = 512;  // works well at 512 or lower but can be set lower in the inspector.  Start with it high and then lower it (slider to the left side) as needed

        [SerializeField]
        [Tooltip("Carefully adjust this value to increase and decrease the coarse responsiveness of the mouth shape to volume.")]
        [Range(0.001F, 1.50F)]
        float lipSpeed = 0.095f;  // lipSeed has a bigger effect in adjusting and reducing lip movement - use sparingly - resolution wise it is chunky

        [SerializeField]
        [Tooltip("Adjust this FINE lipspeed value to carefully increase and decrease responsiveness to LIPSPEED.")]
        [Range(0.001F, 0.09F)]
        float lipSpeedFine = 0.001f;  // lipSpeedFine has a finer adjustment - this will 'tune' the lip movement at a finer resolution

        float currentUpdateTime = 0.0f;

        float clipLoudness;  
        float[] clipSampleData;

        GameObject lipsObject;  // the 'temporary' gameobject which we switch dynamically in the Inspector (mouthshape 1,2,3).

        [SerializeField]
        [Tooltip("Adjust size factor.  Use this to balance reaction to audio clip scale.")]
        [Range(0.0001f, 2.0f)]
        float sizeFactor = 0.20f;

        [SerializeField]
        [Tooltip("Set to minimum size required, (varies with volume and clarity of speech file).")]
        [Range(0.00001f, 0.200f)]
        float minSize = 0.001f;

        [SerializeField]
        [Tooltip("Set to maximum size required, (varies with volume and clarity of speech file).")]
        [Range(0.0001f, 5.0f)]
        float maxSize = 0.001f;

        int randomScale;  // adds variety into the mix.

        // Below are controls to set the mouth X,Y,Z scales for silences and when the character is not speaking.
        // The 'mouthRestingThreshold' is the volume below which no movement will occur.
        [SerializeField]
        [Tooltip("Set the minimum volume below for the mouth to go to the 'resting' shape. At this lower volume, the system switches to your PRESET mouth shape for a closed, none speaking mouth, or a mouth pose 'resting' between words (or in the silences)")]
        [Range(0.05f, 0.00001f)]
        float mouthRestingThreshold;

        [SerializeField]
        [Tooltip("Set mouth minimum size for resting pose. This helps set your PRESET mouth position for moments of silence.")]
        [Range(0.040f, 0.00001f)]
        float xPoseScale = 0.0534f;

        [SerializeField]
        [Tooltip("Set mouth minimum size for resting pose. This helps set your PRESET mouth position for moments of silence.")]
        [Range(0.040f, 0.00001f)]
        float yPoseScale = 0.0534f;

        [SerializeField]
        [Tooltip("Set mouth minimum size for resting pose. This helps set your PRESET mouth position for moments of silence.")]
        [Range(0.00040f, 0.10001f)]
        float zPoseScale = 0.0534f;

        [SerializeField]
        [Tooltip("Set mouth shape to either capsule shape (1) or cube shape (2) or sphere (3).")]
        [Range(1, 3)]
        int mouthShape = 1;  // default shape is shape 1 the capsule

        [SerializeField]
        [Tooltip("Set this checkbox to true if you want to synchronise the 'Mouth Shape' with data presets for that mouth shape.")]
        bool allowMouthDataPresets = true;  // check this boolean to true if you want to start this character with the mouthshape
                                     // and default settings data - otherwise leave false.

        [SerializeField]
        [Tooltip("Setup initial mouth Data based on mouthShape. Defaults to 3rd mouth shape. Drag to change default data loaded for each mouth shape between 1,2,3")]
        [Range(1, 3)]
        int mouthDataPresets = 3;

        [SerializeField]
        [Tooltip("Adjusting this reduces the overall strength of the mouth shapes, if the lips are moving too much, you can reduce this here.")]
        [Range(0.0025F, 25.0F)]
        float lipReducer = 0.0f;  // lipSpeedFine has a finer adjustment - this will 'tune' the lip movement at a finer resolution

        [SerializeField]
        [Tooltip("Do you want the eyes to show emotion, true or false. False by default, they will not show emotion until this boolean is set to TRUE.  Emotive Blink On (checked box = TRUE) takes priority over standard Blinks.  Try switching Blinks on for normal use and add emotive blinks when speech clips require extra emphasis.")]
        bool emotiveBlinkOn = false; // do you wish to affect the blinking eyes? Set this to TRUE by checking the box, or FALSE by unchecking the box

        [SerializeField]  // was blinks
        [Tooltip("Do you want the eyes to blink on and off only, true or false. True by default, they will blink.  This boolean is overridden by the EmotiveBlinkOn bool.")]
        bool justBlinkOn = true; // do you wish to affect the blinking eyes? Set this to TRUE by checking the box, or FALSE by unchecking the box

        [SerializeField]
        [Tooltip("EyeBrows move on true or false. True by default.")]
        bool eyeBrowsOn = true; // do you wish to affect the blinking eyes? Set this to TRUE by checking the box, or FALSE by unchecking the box

        [SerializeField]
        [Tooltip("Setup blink speed.  This is the maximum 'end of the range of time' between blinks.")]
        [Range(1, 150)]
        int maxBlinkTimer =35;

        public bool randomSelectionFromListYes = false;
        
        [SerializeField]
        [Tooltip("When checked (True) this will allow you to play a randomised voice clip from the list of clips on this character.")]
        int randomSelectionFromList = 0;  // this variable is used to select which voice clip to play, randomly.

        [SerializeField]
        [Tooltip("Set this to true if the character has a 'welcome' phrase.")]
        bool welcomeYes = false;

        [SerializeField]
        [Tooltip("Drag and drop the 'Welcome' voice clip here.")]
        AudioClip welcomeClip;

        List<AudioClip> NWT_ClipsTEMPORARY = new List<AudioClip>();  // this is your list of audioclips for this character, in top to bottom (first to last) order, which they will speak during this conversation.
                                                                     //  Activation is achieved using a timeline activation clip for the 'NWT_PlayAudio' GameObject on the 'Now We're Talking' prefab.
                                                                     //  Drag the 'NWT_PlayAudio' gameobject to the timeline and set up an activation track for each piece of speech

        //[SerializeField]
        [Tooltip("This is List of audio clips this character will speak.  Drag and drop audio clips this character will speak into this list, in order ")]
        List<AudioClip> NWT_Clips = new List<AudioClip>();  // this is your list of audioclips for this character, in top to bottom (first to last) order, which they will speak during this conversation.
                                                            //  Activation is achieved using a timeline activation clip for the 'NWT_PlayAudio' GameObject on the 'Now We're Talking' prefab.
                                                            //  Drag the 'NWT_PlayAudio' gameobject to the timeline and set up an activation track for each piece of speech


        [SerializeField]
        [Tooltip("Setting 'D Bug_Results' to true (by checking this box) will print some details to the console for you to monitor. Make sure you un-check this box for your final build or it could cause slow down in your build!")]
        bool dBug_Results;


        [System.Serializable]
        public class Phase_NPC_Clips
        {
            public string phaseName;  // developer given name for this phase
            public List<AudioClip> sampleList;  // do not change
            public bool playPhase;  // bool, used to control which phase should play
          
        }
 
        [SerializeField]
        [Tooltip("NPC_Phases is the MASTER list of lists.  It is a list of lists, of audio clips.  You need at least one list to operate NWT.  Please 'Add' an NPC_Phase list using the '+' below to the right.  Then name the list and populate the list with audio clips.  Add as many NPC_Phase lists as you need.  Then set the correct list to be 'ready' to play, by checking the 'Play Phase' box.")]
        List<Phase_NPC_Clips> NPC_Phases = new List<Phase_NPC_Clips>();

        int lastClipIndex = 0; // the last voice clip played so we can avoid it.

        [SerializeField]
        [Tooltip("This is the index of the audio clip about to play.  You may need to alter this if you play the timeline from later than the start.")]
        int audioClipIndex = 0;

        AudioClip audioClip;  // this is the audioclip to play - fed by the index of the list which can also be randomised if randomSelectionFromListYes = true.

        [SerializeField]
        [Tooltip("This is the gameObject which when activated starts the next line of speech playing.")]
        GameObject NWT_PlayAudio;  // The 'NWT_PlayAudio' gameObject which when activated (in code or on a timeline) will start the next element of a conversation in the list. i.e. when your character needs to speak their next line, add an AudioArray gameobject activation on the timeline and they will begin speaking the next indexed voice clip from the array
      
        bool justOnce01;  // manages switching once only each time
        bool justOnce02;  

        bool mouth1Selected = false;  // user can select a mouthshape to test with their voice clip using the mouthShape slider
        bool mouth2Selected = false;  // the data provided here is a guide, a starting point for the voices provided by 
        bool mouth3Selected = false;  // Now We're Talking and can be adjusted using the sliders in the inspector. REMEMBER to copy any changes to settings you make and paste them back into the correct component unpon stopping the Unity Editor from running.

        [SerializeField]
        [Tooltip("This material will swap with material 2, as a speech clip plays.")]
        Material material1;  // material on a gameobject renderer chosen to swap

        [SerializeField]
        [Tooltip("This materials will swap with material 1, as a speech clip plays.")]
        Material material2;  // material on a gameobject renderer chosen to swap

        [SerializeField]
        [Tooltip("This is the switch - check the box to change materials with voices.")]
        bool flashYes;

        [SerializeField]
        [Tooltip("The materials will swap when this threshold is met.")]
        [Range(0.00000f, 0.50f)]
        float flashRange;

        [SerializeField]
        [Tooltip("This is a list of object renderers whose material needs to be swapped / changed when a character speaks. Used carefully this can give the impression of flashing lights")]
        List<Renderer> NWT_MaterialSwapObjects = new List<Renderer>();
 
        [SerializeField]
        [Tooltip("This checkbox is for if you wish to have objects rotate on a robot or mechanical device.")] 
        bool rotateBool;  // checked = yes unchecked = no

        [SerializeField]
        [Tooltip("This checkbox allows the developer to have toggling rotational movement or not.")]
        bool toggleRotation;  // checked = yes unchecked = no - if yes the rotational direction will toggle.
        bool toggleWayOne;  // used to invert rotation direction alternately

        [SerializeField]
        [Tooltip("Use this slider to set the threshold for rotation of gameobject/s in list below.  If the gameobject/s does not stop spinning when not voice clip is playing, then increase this threshold.")]
        [Range(0.00000f, 0.1f)]
        float rotateThresholdRange;

        [SerializeField]
        [Tooltip("This checkbox is for if you wish to have objects rotate on a robot or mechanical device.")]
        bool rotateXBool;  // checked = yes unchecked = no

        [SerializeField]
        [Tooltip("This checkbox is for if you wish to have objects rotate on a robot or mechanical device.")]
        bool rotateYBool;  // checked = yes unchecked = no

        [SerializeField]
        [Tooltip("This checkbox is for if you wish to have objects rotate on a robot or mechanical device.")]
        bool rotateZBool;  // checked = yes unchecked = no

       
        [SerializeField]
        [Tooltip("This is a list of game objects which should rotate in sync with voice clips.")]
        List<GameObject> NWT_MechanicalRotationList = new List<GameObject>();  // a user filled list of objects to rotate on this characters voice clip threshold

        AudioClip initialClip;
        int howManyPhases = 0;


        // ****** Microphone Input Diversion **********
        [SerializeField]
        [Tooltip("***Warning*** BETA version - this turns on and leaves on the microphone input. You can not use microphone and voice clips in same NWT_Prefab and once microphone is set it may prevent voice clips working.")]
        bool useMicrophone = false;

        //    Boolean to use Adventure Creator or any other 'external' AudioSource to feed NWT to get animated mouth;
        [FormerlySerializedAs("useAdventureCreatorBool")]
        [Tooltip("Set this checkbox to TRUE (by ticking it) if you wish to use an asset like Adventure Creator's audio (or a different, external AudioSource i.e. not NWT) to drive an NWT mouth.  Also NOTE audio files need to be set to 'Decompress on load'.")]
        public bool externalAudioSourceUsed = false;  // checked = yes unchecked = no  // was         //bool useAdventureCreatorBool;  


        // Added March 17th 2022 ***  Thanks to Tony@PixelCrushers - extending functionality with the 'Dialogue System' - v1.07 - now uses NWT_Phases

        public List<Phase_NPC_Clips> PhaseClips
        {
            get { return NPC_Phases; }
        }

        public bool IsExternalAudioSourceUsed
        {
            get { return externalAudioSourceUsed; }
            set { externalAudioSourceUsed = value; }
        }

      
            // Added March 17th 2022 ***

        private void Awake()
        {
            if (useMicrophone) { checkAllMicrophonesConnected(); }

            NWT_Clips.Add( initialClip );  // empty clip needed to initialise the NWT_Clips list itself. Do Not Remove.
            
            if (dBug_Results) print("There are " + NPC_Phases.Count + " NPC_Phases currently.");  // How many NPC_Phases are there?
           
            for (int x = 0; x < NPC_Phases.Count; x++)  //loop through all the NPC_Phases so we can work out which phase is currently active is any
            {
              
                //  if (dBug_Results) { print(NPC_Phases[x].phaseName); }
                if ( NPC_Phases[x].phaseName == "") 
                { 
                   if(dBug_Results) print("This NPC_Phase voice clip list needs a name. Please use the Unity Inspector to enter a unique name."); 
                }
                
                if (dBug_Results) print(NPC_Phases[x].phaseName + " has " + NPC_Phases[x].sampleList.Count + " voice clips.");
                 
                if (NPC_Phases[x].playPhase)
                {
                    if(dBug_Results) print(NPC_Phases[x].phaseName + " is set ready to play.");  // prepare this list for playback.
                    NWT_Clips = NPC_Phases[x].sampleList;
                }
                else
                {
                    if(dBug_Results) print(NPC_Phases[x].phaseName + " is not set ready to play at this time.");
                }

            }

            if (!externalAudioSourceUsed)
            {
                audioSource = GetComponent<AudioSource>();  // get reference to the Now We're Talking AudioSource which drives the mouths
            }

            //check to ensure the developer has populated the NWT_Clips list with at least 1 audio clip
            if (NWT_Clips.Count < 1) { if (dBug_Results) print("Please populate an NPC_Phases list with voice clips (audio files) OR make sure at least one NPC_Phase has the 'Play Phase' set to TRUE."); }  // was 'NWT_Clips'

            NWT_ClipsTEMPORARY = NWT_Clips;  // copy original list into a temporary list ready for when we need to reload it

            if (welcomeYes)
            {
                if (welcomeClip==null  && welcomeYes==true)
                {
                    if(dBug_Results) print("Please drag and drop in a 'welcome' voice clip in the inspector in the 'Welcome Clip' slot!");
                }
            }

            if (allowMouthDataPresets) // if true user default mouth settings based on mouthShape and factory reset mouth dataset
            {

                if (mouth1Selected == true)
                {
                    mouth1Settings();
                }

                if (mouth2Selected == true)
                {
                    mouth2Settings();
                }

                if (mouth3Selected == true)
                {
                    mouth3Settings();
                }
 
            }

            mouthRestingThreshold = 0.00999f;  // this is the lowest (user set) setting a voice file will activate animation at, after which it switches to a fixed shape and scale

            if (lipsCapsule == null)
            { if (dBug_Results) print("Please make sure the 'lipsCapsule' gameObject is dragged into the correctly named slot for this character!"); }


            if (lipsCube == null)
            { if (dBug_Results) print("Please make sure the 'lipsCube' gameObject is dragged into the correctly named slot for this character!"); }

            if (lipsSphere == null)
            { if (dBug_Results) print("Please make sure the 'lipsSphere' gameObject is dragged into the correctly named slot for this character!"); }

            clipSampleData = new float[mouthResponsiveness];  // the samples used
            eyeCount = maxBlinkTimer;  // timer between blinks
            toggleOnOff = false;


            audioClipIndex = -1; // march 25th 2022
        }

        private void Start()
        {
            getParentName();    // set the Identify of each 'NWT_PlayAudio' gameobject in the timeline whilst "running" in the editor, to the root parent object's name ;
                                // so the name of the character appears in the activation track in the timeline - easy for debugging purposes.

            if (useMicrophone)
            {
                AudioSource audioSource = GetComponent<AudioSource>();
                audioSource.clip = Microphone.Start("Microphone (High Definition Audio Device)", true, 10, 44100);  // default name of my mic set up you may need to change this to the device plugged into your PC - here

                audioSource.loop = true;
                while (!(Microphone.GetPosition(null) > 0)) { }
                audioSource.Play();
            }

        }

        void Update()
        { 


            phaseCheck();
            if(dBug_Results) 
            

            if (!audioSource.isPlaying) 
                clipLoudness=0.0f;

            // if (mouthShape == 1 || mouthShape == 2 || mouthShape == 3) lipsPivot.SetActive(true);

            if (mouthDataPresets == 1 && allowMouthDataPresets) // if true user default mouth settings based on mouthShape and factory reset mouth dataset
                mouth1Settings();

            if (mouthDataPresets == 2 && allowMouthDataPresets) // if true user default mouth settings based on mouthShape and factory reset mouth dataset
                mouth2Settings();

            if (mouthDataPresets == 3 && allowMouthDataPresets) // if true user default mouth settings based on mouthShape and factory reset mouth dataset
                mouth3Settings();

            if (mouthShape == 1)
                myCapsule();

            if (mouthShape == 2)
                myCube();

            if (mouthShape == 3)
                mySphere();

            checkWhichMouth();  // function to find out which 'mouth shape' the user has currently selected.

            currentUpdateTime += Time.deltaTime;
          
            if (currentUpdateTime >= lipSpeed + lipSpeedFine  )
            {
                currentUpdateTime = 0.0f;
               

                if (audioSource.isPlaying)
                {
                    audioSource.clip.GetData(clipSampleData, audioSource.timeSamples);
                    clipLoudness = 0.0f;

                    if (audioSource.timeSamples > 0)
                    {
                        foreach (var Sample in clipSampleData)
                        {
                            clipLoudness += Mathf.Abs(Sample);
                        }

                        clipLoudness /= mouthResponsiveness * lipReducer;
                        
                        if (flashYes) FlashInstead();  // if we are flashing lights on and off

                        if (mouthShape == 1) lipsPivot.SetActive(true);

                        if (mouthShape == 2)
                        {
                            lipsPivot.SetActive(true);
                            clipLoudness *= sizeFactor;
                            clipLoudness = Mathf.Clamp(clipLoudness, minSize, maxSize);
                        }

                        if (mouthShape == 3)
                        {
                            lipsPivot.SetActive(true);
                            clipLoudness *= sizeFactor;
                            clipLoudness = Mathf.Clamp(clipLoudness, minSize, maxSize);
                        }
                    }
                }


                // Emotive Blinks have priority over just blinks
                // for blinks just set 'blinks on' by checking the box in the Inspector and leave 'emotive blinks' unchecked
                randomScale = UnityEngine.Random.Range(1, 30);
                eyesScaleY = UnityEngine.Random.Range(0.750f, 1.10f);
                eyeCount -= 1;

                if (eyeCount < 0)
                {
                    eyeCount = UnityEngine.Random.Range(1, maxBlinkTimer);
                }

                closeMouth(); // check the Mouth Resting Threshold settings if the mouth needs to be closed during silences or between words!
                              // Move the slider to the right to reduce the threshold. Use the X,Y,Z Pose Scale to set up the resting mouth pose.

                if (eyeBrowsOn && myEyeBrows != null)
                {
                    eyeBrows(); // do eyebrow movements if true.
                }

                if (rotateBool)
                {
                    rotateMechanicalBits();  // if it is  set to operate then goto it 
                }


            }

           
            if (NWT_Clips.Count >= 1)  // only if there are some clips to speak.
            {
                voicesGo();
            }
        }

        void myCapsule()
        { // CAPSULE mouth shape

            // Now do mouth movements if the loudness threshold is met - else return
            if (clipLoudness <= mouthRestingThreshold)
            {
                return;
            }

            if (randomScale == 1)
            {
                clipLoudness *= sizeFactor;
                clipLoudness = Mathf.Clamp(clipLoudness, minSize, maxSize);

                lipsObject.transform.localScale = new Vector3(clipLoudness / 3.5F, clipLoudness / 8.3F, clipLoudness / maxSize/6);
                
            }
            else
            {
                clipLoudness *= sizeFactor;
                clipLoudness = Mathf.Clamp(clipLoudness, minSize, maxSize);
                lipsObject.transform.localScale = new Vector3(clipLoudness / 3.5F, clipLoudness / 3.5F, clipLoudness / (maxSize*16));
            }
        }

        void myCube()
        {
            // Now do mouth only if loudness threshold is met
            if (clipLoudness <= mouthRestingThreshold)
                return;

            // CUBE version
            if (randomScale == 1)
            {
                lipsObject.transform.localScale = new Vector3(clipLoudness / 4f, 0.005797093f / 2F, 0.02809257f);
            }
            else
            {
                lipsObject.transform.localScale = new Vector3(0.05993376F / 4, clipLoudness / 3F, 0.02809257f);
            }
        }


        void mySphere()
        {
            // Now do mouth only if the user set lower limit loudness threshold is met
            if (clipLoudness <= mouthRestingThreshold)
                return;
            
            // Sphere version
            if (randomScale == 1)
            {
                lipsObject.transform.localScale = new Vector3(clipLoudness / 12f, 0.005797093f / 2F, 0.02809257f);  // was new Vector3(clipLoudness / 4f
            }

            else
            {
                lipsObject.transform.localScale = new Vector3(0.00125993376F, clipLoudness / 16F, 0.02809257f);
            }
        }



        private void closeMouth()
        {
            // Now do mouth if loudness threshold is met
            if (clipLoudness <= mouthRestingThreshold)   // userSetLimit
            {
               lipsObject.transform.localScale = new Vector3(xPoseScale, yPoseScale, zPoseScale);   // close mouth if almost silent audio
            }
        }

        private void LateUpdate()
        {
            if (emotiveBlinkOn)
            {
                blinks(); // do eye blinks if true
            }
            else
            {
                noEmotiveBlinks();
            }
            
            toggleOnOff = !toggleOnOff;  // toggle the toggle
        }

        private void checkWhichMouth()
        {
            if (mouthShape == 1)
            {
                lipsCapsule.SetActive(true);
                lipsObject = lipsCapsule;
                lipsCube.SetActive(false);
                lipsSphere.SetActive(false);

                if (mouth001Lips) 
                { 
                    lipsmouth001Lips.SetActive(true);
                }
                else 
                { 
                    lipsmouth001Lips.SetActive(false); 
                }
            }

            else if (mouthShape == 2)
            {
                lipsCube.SetActive(true);
                lipsObject = lipsCube;
                lipsCapsule.SetActive(false);
                lipsSphere.SetActive(false);
                if (mouth002Lips)
                {
                    lipsmouth002Lips.SetActive(true);
                }
                else
                {
                    lipsmouth002Lips.SetActive(false);
                }
            }
            else if (mouthShape == 3)
            {
                lipsSphere.SetActive(true);
                lipsObject = lipsSphere;
                lipsCapsule.SetActive(false);
                lipsCube.SetActive(false);

                if (mouth003Lips)
                {
                    lipsmouth003Lips.SetActive(true);
                }
                else
                {
                    lipsmouth003Lips.SetActive(false);
                }
            }
 
        }
        private void blinks()  // if 'blink on' is checked on in the inspector then the character will occasionally blink.
        {
           eyeRandom = UnityEngine.Random.Range(1, maxBlinkTimer);  // was 10  // dec 29th
           
            if (myEyes != null)  // only do this if there are eyes in the Drag and Drop slot
            {

                if (randomScale == 1)  // for the occasaionl time we want to vary it randomly
                {
                    if (eyeCount == 0)
                    {
                     //   myEyes.transform.localScale = Vector3.one;
                        myEyes.transform.localScale = Vector3.Lerp(myEyes.transform.localScale, Vector3.one, Time.deltaTime * 2 );  // was eyeRandom  Jan 1st

                    }
                }
                else
                {
                    if (eyeCount == 0)
                     //   myEyes.transform.localScale = new Vector3(1.0f, eyesScaleY, 1.0f);
                    myEyes.transform.localScale = Vector3.Lerp(myEyes.transform.localScale, new Vector3(1.0f, eyesScaleY, 1.0f)  , Time.deltaTime * eyeRandom);
                }
            }
        }
        private void eyeBrows()  // if eyebrows is checked on then this code will attempt to animate them slightly for you, to give them some 'feeling of life'
        {

             if (eyeCount > 0)
            {
              
                myEyeBrows.transform.localScale = Vector3.Lerp(myEyeBrows.transform.localScale, new Vector3(1.0f, eyesBrowsScaleY, 1.0f), Time.deltaTime *4);
               
             }
             else  //do this when eyeCount = 0
            {
                 eyesBrowsScaleY = UnityEngine.Random.Range(0.25f, 1.250f); // for your eyebrows only!
            }

        }



        private void noEmotiveBlinks()
        {
            if (justBlinkOn)
            {

                if (myEyes != null)  // only do this if there are eyes in the Drag and Drop slot
                {
                    if (eyeCount == 0)
                    {
                        myEyes.transform.localScale = new Vector3(1.0f, 0.20f, 1.0f);
                    }

                    else
                    {
                        myEyes.transform.localScale = Vector3.one;
                    }
                }
                
            }
        }



        // IMPORTANT - READ ME!
        // Run the Unity Editor and ensure the 'Allow Mouth Data Presets' is checked (true) to load data presets for each mouth shape,
        // based on the mouth shape slider. An initial set of data will be loaded to assist you. Slide the slider to the mouth shape
        // you wish to load a preset for.  Then UNCHECK 'Allow Mouth Data Presets' and begin to adjust the presets to better suit
        // your characters voice clip. When you are happy, pause the editor.  Copy the 'Now We're Talking' Component.
        // Stop the editor.  'Paste Component Values' back into your character's 'Now We're Talking' component and Save.
        // Your new settings are ready to make your character's mouth move as you require.

        // The following datasets represent great starting points for each included mouthshape - 1, 2, 3.
        public void mouth1Settings()  // mouth shape 1
        {
            mouthShape = 1;  //settings before Feb 3rd 2022
            mouthResponsiveness = 294; // 448; 
            lipSpeed = 0.001f;         //0.043f;
            lipSpeedFine = 0.001f;     //0.0285f;
            sizeFactor = 0.56f;         //0.988f;
            minSize = 0.003f;          // 0.0034f;
            maxSize = 0.79f;           // 2.81f;
            mouthRestingThreshold = 0.0099f; // 0.0112f;
            xPoseScale = 0.0013f;      // 0.0043f;
            yPoseScale = 0.0114f;      // 0.028f;
            zPoseScale = 0.0004f;
            lipReducer = 3.5f;
        }


        public void mouth2Settings()  // mouth shape 2
        {
            mouthShape = 2;
            mouthResponsiveness = 114;
            lipSpeed = 0.035f;
            lipSpeedFine = 0.0181f;
            sizeFactor = 0.109f;
            minSize = 0.0064f;
            maxSize = 0.17f;
            mouthRestingThreshold = 0.0102f;
            xPoseScale = 0.0028f;
            yPoseScale = 0.0019f;
            zPoseScale = 0.0223f;
            lipReducer = 1.70f;
        }


        public void mouth3Settings()  // mouth shape 3
        {
            mouthShape = 3;
            mouthResponsiveness = 320;
            lipSpeed = 0.034f;
            lipSpeedFine = 0.0094f;
            sizeFactor = 0.08f;
            minSize = 0.004f;
            maxSize = 0.67f;    
            mouthRestingThreshold = 0.0069f;
            xPoseScale = 0.0001f;
            yPoseScale = 0.0019f;
            zPoseScale = 0.03f;
            lipReducer = 0.065f;
        }

        // CHARACTERS in the hierarchy... either, change the name of the root parent gameobject, make it the character's name...  'SM_Character001' to 'Richard'
        // OR
        // Rename 'NWT_PlayAudio' with the character's name. ie 'Peter'

        // In the Unity Editor/Inspector, rename the 'NWT_PlayAudio' GameObject on the 'Now We're Talking' GameObject to the name of the
        // character whose mouth it is, or if you leave it blank this snippet will rename the track in the timeline for you, based on the root parent name. See above note.

        private void getParentName()
        {
            if (GameObject.Find("NWT_PlayAudio"))
            {
                GameObject PlayAudio = GameObject.Find("NWT_PlayAudio");  // Find the gameobject which when activated will activate the next line of speech
                PlayAudio.name = PlayAudio.transform.root.gameObject.name + " :";
            }
        }

        private void playWelcome()  // plays the welcome clip
        {
            audioSource.clip = welcomeClip;
            audioSource.Play();  // play the indexed audio clip.
        }

        private void FlashInstead()
        {
            for (int x = 0; x < NWT_MaterialSwapObjects.Count; x++)
            {
                if (clipLoudness > flashRange)
                {
                    NWT_MaterialSwapObjects[x].material = material1;
}
                else
                {
                    NWT_MaterialSwapObjects[x].material = material2;
             }
        }
    }


        private void toggleWay1()
        {
            if(toggleWayOne)
            { 
                toggleWayOne = false;
                return;
            }
            else
            {
                toggleWayOne = true;
            }
        }



        // Robotic or mechanical rotation of objects in NWT_MechanicalRotationList list, synchronised to voice clip
        private void rotateMechanicalBits()
        {
            if (NWT_MechanicalRotationList.Count == 0)  // check there is at least one gameobject in the NWT Mechanical Rotation list
            {
                if (dBug_Results) print("Please populate the NWT_Mechanical Rotation List with at least one gameobject, or reset the Mechanical Rotation List to empty.");
                return;
            }

            if (toggleRotation)
            {
                toggleWay1();
            }

           
                for (int r = 0; r < NWT_MechanicalRotationList.Count; r++)  // cycle through the list of gameobjects to rotate
            {
                if (clipLoudness > rotateThresholdRange)  // check the threshold of the clipLoudness
                {
                    float thisXrot = NWT_MechanicalRotationList[r].transform.rotation.x;
                    float thisYrot = NWT_MechanicalRotationList[r].transform.rotation.y;
                    float thisZrot = NWT_MechanicalRotationList[r].transform.rotation.z;

                    if (rotateXBool && toggleWayOne)
                    {
                        NWT_MechanicalRotationList[r].transform.Rotate(clipLoudness * 10000.0f, thisYrot, thisZrot, Space.Self);
                        
                    }

                    if (rotateXBool && !toggleWayOne)
                    {
                        NWT_MechanicalRotationList[r].transform.Rotate(-clipLoudness * 10000.0f, thisYrot, thisZrot, Space.Self);
                       
                    }

                    if (rotateYBool && toggleWayOne)
                    {
                        NWT_MechanicalRotationList[r].transform.Rotate(thisXrot, clipLoudness * 10000.0f, thisZrot, Space.Self);
                       
                    }

                    if (rotateYBool && !toggleWayOne)
                    {
                        NWT_MechanicalRotationList[r].transform.Rotate(thisXrot, -clipLoudness * 10000.0f, thisZrot, Space.Self);
                        
                    }

                    if (rotateZBool && toggleWayOne)
                        {
                            NWT_MechanicalRotationList[r].transform.Rotate(thisXrot, thisYrot, clipLoudness * 10000.0f, Space.Self);
                        
                    }
                    if (rotateZBool && !toggleWayOne)
                    {
                        NWT_MechanicalRotationList[r].transform.Rotate(thisXrot, thisYrot, -clipLoudness * 10000.0f, Space.Self);
                       
                    }
                }

            }
        }





            private void voicesGo()
        {
            if (NWT_Clips.Count == 0) 
            {
                if (dBug_Results) print("You must load some voice clips in this Phase to use it.");
                identifyPhase();  // find out which Phase is causing the issue
             }



            if (NWT_PlayAudio.activeSelf == true && justOnce01 == false)
            {
                if (welcomeYes)  // is the welcome flag set to true?
                {   
                    welcomeYes = false;  // turn off the welcome flag - this will allow further clips to be played once the welcome clip has finished playing. 
                    playWelcome();  // play the welcome speech
                    return;  // exit
                }

                else
                
                {
                    if(audioSource.isPlaying)  // if the welcome speech is still playing exit again
                    {
                       return;  // exit  Don't allow any other clips to play just yet...
                                // Once the Welcome clip stops playing the code will drop through to play the next clips from the list
                    }
                }

                justOnce01 = true;
                justOnce02 = false;

                audioClipIndex++;  // step through the list  of audio clips in list using this index.  maybe remove this later ****** PMC ******
                checkPhase();
                if(audioClipIndex >= NWT_Clips.Count) { audioClipIndex = 0; } // print("Finished playing the list of NWT_Clips");

                if (randomSelectionFromListYes)  // set this to true and the list will be randomised so the voice clips play in a random order.
                {
                    randomSelectionFromList = UnityEngine.Random.Range(0, NWT_Clips.Count );  
                    if (lastClipIndex != randomSelectionFromList)
                    {
                        audioClipIndex = randomSelectionFromList;
                        lastClipIndex = randomSelectionFromList;
                    }
                    else
                    {
                        randomSelectionFromList = UnityEngine.Random.Range(0, NWT_Clips.Count );
                        audioClipIndex = randomSelectionFromList;
                        lastClipIndex = randomSelectionFromList;
                     }
                }
                
                audioClip = NWT_Clips[audioClipIndex];
                audioSource.clip = audioClip;
                audioSource.Play();  // play the indexed audio clip.
 
                if (audioClipIndex >= NWT_Clips.Count) { audioClipIndex = -1; }  // reset the index when it exceeds the list count. -1 as the next time through
                                                                                 // we increment before operating on it
                // additional lines to allow follow on voice clips by adding '++' to the filename of each voice clip which has a follow on vocie clip after it.  March 28th 2022.
                if (audioClip.name.Contains("++"))  // if a filename has "&&" in it then the next voice clip will play automatically as soon as the current clip finishes.
                {
                    playCurrentPhase();  //  March 2022
                }

            }

            if (NWT_PlayAudio.activeSelf == false && justOnce02 == false)
            {
                justOnce02 = true;
                justOnce01 = false;
            }
        }

        private void checkPhase()  // check if a phase is ready or not, if bool = true it is ready - swap lists from ready phaselist into NWT_Clips list
        {
            for (int x = 0; x < NPC_Phases.Count; x++)  // loop through the PHASEs list by 'Count' times
            {
                if (NPC_Phases[x].playPhase) // if this is set to true and there are more than 0 clips in the list then we need to copy this list of audioclips into NWT_Clips
                { 

                 if(NPC_Phases[x].sampleList.Count>0)  // copy list if sampleList.Count >0
                    {
                        NWT_Clips = NPC_Phases[x].sampleList; return;  // do the copy
                    }
                }

                if (dBug_Results) print(NPC_Phases[x].phaseName+" is set to "+NPC_Phases[x].playPhase);

                if (NPC_Phases[x].phaseName == "") { if (dBug_Results) print("This NPC_Phase voice clip list needs a name. Please use the Unity Inspector to enter a unique name."); }
               

               identifyPhase();
            }
           
        }

        private void identifyPhase()
        {
            phaseCheck();

            if (howManyPhases==0 && dBug_Results) print("At least one NPC_Phase must have their 'Play Phase' set to TRUE.");
        }

        
        private void phaseCheck()  // which phase are we in currently and have we more than one selected?
        {
            howManyPhases = 0;
            for (int x = 0; x < NPC_Phases.Count; x++)  // loop through the PHASEs list by 'Count' times
            {
                if (NPC_Phases[x].playPhase) // if this is set to true and there are more than 0 clips in the list then we need to copy this list of audioclips into NWT_Clips
                {
                    howManyPhases++;
                    if(dBug_Results) print(" This phase name is "+NPC_Phases[x].phaseName);
                }
            }
            if (howManyPhases > 1 && dBug_Results)
            {
                print("You have " + howManyPhases + " phases selected!  Only ONE phase is allowed to be selected at a time! Check your NPC_Phases PlayPhase settings!");
            }
        }

        public void setTheCorrectPhase(string correctPhase)   // this method is waiting for the correct phase to set for the NWT_PlayAudio to know the correct
                                                              // list to pull voice clips from - number 1,2,3...?  use SendMessage to send the name to set that phase to active so it plays
        {
             
            for (int x = 0; x < NPC_Phases.Count; x++)  // loop through all the possible PHASEs using 'Count' as the index
            {
              
                if (NPC_Phases[x].phaseName == correctPhase)  // if the name sent by sendmessage == a name in a phase found here then
                                                              // activate that Phase via it's playPhase BOOL (all Phase names MUST be unique).
                {
                    NPC_Phases[x].playPhase = true;
                    if (dBug_Results) 
                    { 
                        print("This Phase = " + x); 
                    }
                }
                else
                {
                    NPC_Phases[x].playPhase = false;
                }
            }
        }

        // ************* Microphone Input *********************
        // This function checks and prints out any microphone connected to this PC
        // You may have to find the correct name and enter it into the Start functions microphone code - LINE 437 - see example below
        // 
        // audioSource.clip = Microphone.Start("Microphone (High Definition Audio Device)", true, 10, 44100);  // default name of my mic set up you may need to change this to the device plugged into your PC - here
        // 
        // If you don't want to hear the microphone output simple find the audio Source on the NWT_Prefab and set its volume to 0 
        void checkAllMicrophonesConnected()
        {
            foreach (var device in Microphone.devices)
            {
                Debug.Log("Name: " + device);
            }


        }


        public AudioSource CurrentAudioSource   // appended March 17th V1.07 - thanks to Tony @ Pixel Crushers
        {
            get { return audioSource; }
            set { audioSource = value; }
        }

        public bool IsPlaying
        {
            get { return audioSource != null && audioSource.isPlaying; }
        }

        /// <summary>
        /// Returns the name of the phase that is currently set. The phase may not be playing yet.
        /// Check the IsPlaying property to determine if the phase is playing. If the Welcome clip
        /// is playing, this property returns "Welcome".
        /// </summary>
        public string CurrentPhase
        {
            get
            {
                if (IsPlaying && audioSource.clip == welcomeClip)
                {
                    return "Welcome";
                }
                for (int x = 0; x < NPC_Phases.Count; x++)
                {
                    if (NPC_Phases[x].playPhase) return NPC_Phases[x].phaseName;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the audio clips assigned to a phase.
        /// </summary>
        public List<AudioClip> getPhaseAudioClips(string phaseName)
        {
            for (int x = 0; x < NPC_Phases.Count; x++)
            {
                if (NPC_Phases[x].phaseName == phaseName) return NPC_Phases[x].sampleList;
            }
            return null;
        }

        /// <summary>
        /// Sets the correct phase and plays it.
        /// </summary>
        /// <param name="correctPhase"></param>
        public void playPhase(string correctPhase)
        {
            setTheCorrectPhase(correctPhase);
            playCurrentPhase();
        }

        /// <summary>
        /// Plays the current phase, which has been previously set by setTheCorrectPhase.
        /// </summary>
        public void playCurrentPhase()
        {
            StartCoroutine(playCurrentPhaseCoroutine());
        }

        private IEnumerator playCurrentPhaseCoroutine()
        {
            NWT_PlayAudio.SetActive(false);
            yield return null;
            NWT_PlayAudio.SetActive(true);
        }

        /// <summary>
        /// Stops the currently-playing phase, if any.
        /// </summary>
        public void stopCurrentPhase()
        {
            if (audioSource != null) audioSource.Stop();
            audioClipIndex = -1;
        }


    }

}
 

 

 
