using GameEvents;
using System;
using System.Collections;
using Configurations;
using ObjectFactory;
using Player;
using SLTypes;
using Systems.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class GameManager : StatefulMonoBehaviour<GameManager>
{
    public static GameManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Set up FSM
        fsm = new FSM<GameManager>();
        fsm.Configure(this, new GameStateIntro());
    }

    private void OnDestroy()
    {
        if (Instance == this) {
            Instance = null;
        }
    }

    [HideInInspector] private IPlayer player;
    private Vector3 lastKnownPlayerPosition;
    private int lastKnownCollectedCoins = 0, lastKnownPlayerHealth = 100;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool useSavedPlayerPosition = false;
    
    private void Start()
    {
        GameConfiguration.Load();
        GameConfiguration.Save();
        
        ServiceLocator.ForSceneOf(this).Get(out player);
        
        InitializeData();
    }

    protected override void Updated()
    {
        if (player != null)
        {
            lastKnownPlayerPosition = player.Position;
            lastKnownCollectedCoins = player.CollectedCoins;
            lastKnownPlayerHealth = player.Health;
        }
    }

    //private int coinAmount = 0;
    [HideInInspector] public bool isGamePaused = false, isInventoryOpen = false;
    [HideInInspector] public bool gameOver = false, reachedGoal = false;

    public GameDifficulty gameDifficulty { get; private set; } = GameDifficulty.Easy;

    void OnEnable()
    {
        GameEventManager.AddListener<GameDifficultyChangedEvent>(OnDifficultyChange);
        GameEventManager.AddListener<GameOverEvent>(OnGameOver);
        GameEventManager.AddListener<ReachedGoalEvent>(OnReachingGoal);
        GameEventManager.AddListener<InventoryVisibilityChangeEvent>(OnInventoryToggle);
        GameEventManager.AddListener<PlayerReachedGoalEvent>(OnPlayerReachedGoal);
    }

    void OnDisable()
    {
        GameEventManager.RemoveListener<GameDifficultyChangedEvent>(OnDifficultyChange);
        GameEventManager.RemoveListener<GameOverEvent>(OnGameOver);
        GameEventManager.RemoveListener<ReachedGoalEvent>(OnReachingGoal);
        GameEventManager.RemoveListener<InventoryVisibilityChangeEvent>(OnInventoryToggle);
        GameEventManager.RemoveListener<PlayerReachedGoalEvent>(OnPlayerReachedGoal);
        
        // no saving of data, if GameState = GameOver
        if (fsm.CurrentState.GetType() == typeof(GameStateLost))
            return;
        
        // Save Configuration
        GameConfiguration.SaveCoinAmount(lastKnownCollectedCoins);
        GameConfiguration.SaveHealthPoints(lastKnownPlayerHealth);
        GameConfiguration.SavePlayer(lastKnownPlayerPosition);
        GameConfiguration.Save();  // save inventory on game closed
    }

    private void OnPlayerReachedGoal(PlayerReachedGoalEvent e)
    {
        reachedGoal = true;
    }

    private void OnInventoryToggle(InventoryVisibilityChangeEvent e)
    {
        isInventoryOpen = e.isOpen;
    }

    private void OnGameOver(GameOverEvent e)
    {
        Debug.Log($"Player died at {e.deathPosition} by {e.killer.ToString()}");
        gameOver = true;
        // stop time to freece physics [after x sec, so death animations can play]
        switch (e.killer)
        {
            case GameOverEvent.Killer.Drowned:
                StartCoroutine(StopTimeAfterDeath(5.6f));
                break;
            default:
                StartCoroutine(StopTimeAfterDeath(1.75f));
                break;
        }
    }

    private IEnumerator StopTimeAfterDeath(float delay)
    {
        yield return new WaitForSeconds(delay);
        Time.timeScale = 0;
    }

    private void OnReachingGoal(ReachedGoalEvent e)
    {
        if (gameOver)
            return;
        
        GameEventManager.Raise(new GameWonEvent());
        reachedGoal = true;
    }

    /*private void OnCoinCollection(CollectedCoinEvent e)
    {
        coinAmount += e.amount;
        GameEventManager.Raise(new UpdatedCoinsEvent(coinAmount));  // for UI update
    }*/
    
    public Type GetPreviousStateType()
    {
        IFSMState<GameManager> prevState = fsm.PreviousState;

        if (prevState == null)
            return null;

        return prevState.GetType();
    }

    public void LockCursor(bool isLocked)
    {
        Cursor.visible = !isLocked;
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
    }

    public void OnDifficultyChange(GameDifficultyChangedEvent e)
    {
        if (gameDifficulty == e.newDifficulty)
            return;
        
        gameDifficulty = e.newDifficulty;
        GameConfiguration.SaveDifficulty(gameDifficulty);
    }

    public IFSMState<GameManager> GetPreviousState()
    {
        return fsm.PreviousState;
    }

    public void InitializeData()
    {
        // Restore from Configuration Save File
        gameDifficulty = GameConfiguration.Data.gameDifficulty;  // difficulty from Configuration-File
        GameEventManager.Raise(new GameDifficultyChangedEvent(gameDifficulty));
        
        SaveDataState state = GameConfiguration.GetSaveDataState();
        Debug.Log($"Save state: {state}");
        Debug.Log($"Config player pos: {GameConfiguration.Data.playerPos}");
        Debug.Log($"Spawn point pos: {spawnPoint.position}");
        Debug.Log($"Player pos BEFORE: {player.Position}");
        switch (state)
        {
            case SaveDataState.Modified:
                if (useSavedPlayerPosition)
                {
                    Debug.Log("SPAWNING AT SAVED POSITION");
                    player.SetInitialPosition(GameConfiguration.Data.playerPos, Quaternion.identity);
                }
                else
                {
                    Debug.Log("Saved data exists, but saved position disabled.");
                    player.SetInitialPosition(spawnPoint.position, Quaternion.LookRotation(spawnPoint.forward));
                }
                break;

            default:
                Debug.Log("SPAWNING AT SPAWN POINT");
                player.SetInitialPosition(spawnPoint.position, Quaternion.LookRotation(spawnPoint.forward));
                break;
        }
        Debug.Log($"Player pos AFTER SetInitialPosition: {player.Position}");
            
        player.SetInitialCoins(GameConfiguration.Data.coinAmount);  // collected coins from Configuration-File
        Inventory.Instance.InitializeInventory();  // load Inventory from Configurations-File (if possible)
        
        isGamePaused = false;
        gameOver = false;
        reachedGoal = false;
        
        Time.timeScale = 1;
        
        // Set up FSM
        fsm?.Clear();
        fsm = new FSM<GameManager>();
        fsm.Configure(this, new GameStateIntro());
    }

    public void ResetGame(bool firstLoad = false)
    {
        if (!gameOver)
            return;
        
        Time.timeScale = 1;
        
        // clear all forgotten event listeners
        GameEventManager.Clear();
        
        // reload scene
        SceneLoader.Instance.LoadSceneGroup(SceneLoader.Instance.ActiveSceneGroupIndex);
    }

    public void RequestRestart()
    {
        Debug.Log("Restart Requested GameManager");
        if (gameOver)
        {
            ResetGame();
        }
    }
    
    public void TogglePause()
    {
        isGamePaused = !isGamePaused;
        if (isGamePaused)
            Time.timeScale = 0;
        else
            Time.timeScale = 1;
    }
}
