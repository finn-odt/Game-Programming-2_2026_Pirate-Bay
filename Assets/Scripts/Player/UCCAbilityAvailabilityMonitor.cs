using System;
using System.Collections.Generic;
using GameEvents;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using Opsive.UltimateCharacterController.AddOns.Swimming;
using UnityEngine;

public class UCCAbilityAvailabilityMonitor : MonoBehaviour
{
    [Serializable]
    public class AbilityPossibleBinding
    {
        public string abilityTypeName = "";

        public GameEventTypeReference onBecamePossible = new GameEventTypeReference();
        public GameEventTypeReference onBecameImpossible = new GameEventTypeReference();

        public AbilityPossibleBinding()
        {
            abilityTypeName = "";
            onBecamePossible = new GameEventTypeReference();
            onBecameImpossible = new GameEventTypeReference();
        }

        public bool Matches(Ability ability)
        {
            if (ability == null || string.IsNullOrWhiteSpace(abilityTypeName))
                return false;

            Type type = ability.GetType();

            return type.Name == abilityTypeName ||
                   type.FullName == abilityTypeName ||
                   type.AssemblyQualifiedName == abilityTypeName;
        }

        public GameEventTypeReference GetEffect(bool isPossible)
        {
            return isPossible ? onBecamePossible : onBecameImpossible;
        }
    }

    [SerializeField] private GameObject character;

    [SerializeField, Min(0.02f)] private float checkInterval = 0.1f;
    [SerializeField] private bool onlyCheckEnabledAbilities = true;
    [SerializeField] private bool hideWhileAbilityIsActive = true;
    [SerializeField] private bool raiseInitialPossibleEvents = true;
    [SerializeField] private bool raiseInitialImpossibleEvents = false;

    [SerializeField] private List<AbilityPossibleBinding> bindings = new();
    
    private UltimateCharacterLocomotion characterLocomotion;
    private CharacterLayerManager characterLayerManager;

    private Ability[] abilities;
    private readonly Dictionary<Ability, bool> lastPossibleByAbility = new();

    private RaycastHit underwaterCheckHit;

    private float nextCheckTime;

    private void Awake()
    {
        if (character == null)
            character = gameObject;
        
        characterLocomotion = character.GetComponent<UltimateCharacterLocomotion>();
        characterLayerManager = character.GetComponent<CharacterLayerManager>();

        if (characterLocomotion == null)
        {
            Debug.LogError($"{nameof(UCCAbilityAvailabilityMonitor)} could not find UltimateCharacterLocomotion.", this);
            enabled = false;
            return;
        }

        abilities = characterLocomotion.Abilities;
    }

    private void OnEnable()
    {
        lastPossibleByAbility.Clear();
        nextCheckTime = Time.time;
    }

    private bool hasStarted = false;
    private void Start()
    {
        hasStarted = true;
        CheckAbilities(true);
        nextCheckTime = Time.time + checkInterval;
    }

    private void Update()
    {
        if (!hasStarted)
            return;
        
        if (Time.time < nextCheckTime)
            return;

        nextCheckTime = Time.time + checkInterval;
        CheckAbilities(false);
    }

    private void CheckAbilities(bool initialCheck)
    {
        if (abilities == null)
            return;

        for (int i = 0; i < abilities.Length; i++)
        {
            Ability ability = abilities[i];
            
            if (ability == null)
                continue;

            if (!IsAbilityInBindingsList(ability))
                continue;

            bool isPossible = IsAbilityPossible(ability);

            if (!lastPossibleByAbility.TryGetValue(ability, out bool wasPossible))
            {
                lastPossibleByAbility[ability] = isPossible;

                if (initialCheck)
                {
                    if (isPossible && raiseInitialPossibleEvents)
                        RaiseForAbility(ability, true);

                    if (!isPossible && raiseInitialImpossibleEvents)
                        RaiseForAbility(ability, false);
                }

                continue;
            }

            if (wasPossible == isPossible)
                continue;

            lastPossibleByAbility[ability] = isPossible;
            RaiseForAbility(ability, isPossible);
        }
    }

    private bool IsAbilityPossible(Ability ability)
    {
        if (onlyCheckEnabledAbilities && !ability.Enabled)
            return false;

        // Special case:
        // For Swim we want to know whether the player can dive
        // from surface swimming into underwater swimming.
        //      => water depth > minDepth & isSwimming
        if (ability is Swim swim)
            return IsUnderwaterSwimPossible(swim);

        if (hideWhileAbilityIsActive && ability.IsActive)
            return false;

        return ability.CanStartAbility();
    }
    
    private bool IsUnderwaterSwimPossible(Swim swim)
    {
        if (swim == null)
            return false;

        // The Swim ability must already be running.
        if (!swim.Enabled || !swim.IsActive)
            return false;

        // Underwater swimming must be enabled in Opsive.
        if (!swim.CanSwimUnderwater)
            return false;

        // Opsive Swim states:
        // 0 = EnterWaterFromAir
        // 1 = SurfaceSwim
        // 2 = UnderwaterSwim
        // 3 = ExitWaterMoving
        // 4 = ExitWaterIdle
        const int SurfaceSwimState = 1;

        if (swim.AbilityIntData != SurfaceSwimState)
            return false;

        if (characterLayerManager == null)
            return false;

        // Is there enough free space underneath the character
        // to transition into underwater swimming?
        bool blockedBelow = characterLocomotion.SingleCast(
            -characterLocomotion.Up,
            Vector3.zero,
            swim.MinUnderwaterSwimDepth,
            characterLayerManager.SolidObjectLayers,
            ref underwaterCheckHit);

        return !blockedBelow;
    }

    private void RaiseForAbility(Ability ability, bool isPossible)
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            AbilityPossibleBinding binding = bindings[i];

            if (binding == null || !binding.Matches(ability))
                continue;

            GameEventTypeReference specificEffect = binding.GetEffect(isPossible);

            // Only raise the default event when the binding actually has a specific effect.
            if (specificEffect == null || !specificEffect.HasValue)
                continue;

            // Event: depends on ability and possible/impossible state.
            specificEffect.TryRaise();
        }
    }

    private bool IsAbilityInBindingsList(Ability ability)
    {
        for (int i = 0; i < bindings.Count; i++)
        {
            AbilityPossibleBinding binding = bindings[i];

            if (binding != null && binding.Matches(ability))
                return true;
        }
        return false;
    }

    private void OnValidate()
    {
        bindings ??= new List<AbilityPossibleBinding>();

        for (int i = 0; i < bindings.Count; i++)
        {
            bindings[i] ??= new AbilityPossibleBinding();

            bindings[i].onBecamePossible ??= new GameEventTypeReference();
            bindings[i].onBecameImpossible ??= new GameEventTypeReference();
        }
    }
}