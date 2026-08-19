using System;
using System.Collections.Generic;
using GameEvents;
using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
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
    private Ability[] abilities;
    private readonly Dictionary<Ability, bool> lastPossibleByAbility = new();

    private float nextCheckTime;

    private void Awake()
    {
        if (character == null)
            character = gameObject;

        characterLocomotion = character.GetComponent<UltimateCharacterLocomotion>();

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
        CheckAbilities(true);
    }

    private void Update()
    {
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
            
            // could skip those that are not in bindings
            if (!IsAbilityInBindingsList(ability))
                return;

            if (ability == null)
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

        if (hideWhileAbilityIsActive && ability.IsActive)
            return false;

        return ability.CanStartAbility();
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