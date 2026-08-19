using System;
using GameEvents;
using UnityEngine;
using Opsive.Shared.Input;
using Opsive.UltimateCharacterController.Character;

public class GameOverUCCLock : MonoBehaviour
{
    [SerializeField] private GameObject player;

    private PlayerInputProxy playerInputProxyForUCC;
    private UltimateCharacterLocomotion locomotion;

    private void Awake()
    {
        playerInputProxyForUCC = player.GetComponent<PlayerInputProxy>();
        locomotion = player.GetComponent<UltimateCharacterLocomotion>();
    }

    private void OnEnable()
    {
        GameEventManager.AddListener<GameOverEvent>(OnGameOver);
    }

    private void OnDisable()
    {
        GameEventManager.RemoveListener<GameOverEvent>(OnGameOver);
    }

    public void OnGameOver(GameOverEvent e)
    {
        // Stop active movement/abilities first.
        if (locomotion != null) {
            locomotion.StopAllAbilities(true);
        }

        // Prevent UCC from receiving more player input.
        if (playerInputProxyForUCC != null) {
            playerInputProxyForUCC.enabled = false;
        }
    }
}