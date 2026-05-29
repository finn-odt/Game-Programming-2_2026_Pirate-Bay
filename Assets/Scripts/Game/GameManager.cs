using GameEvents;
using System;
using System.Collections;
using Configurations;
using Player;
using SLTypes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityServiceLocator;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using static GameObjectFactory;

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

    [HideInInspector] private IPlayer player;
    private Vector3 lastKnownPlayerPosition;
    private int lastKnownCollectedCoins = 0, lastKnownPlayerHealth = 100;
    [SerializeField] private bool useSavedPlayerPosition = false;
    
    private void Start()
    {
        GameConfiguration.Load();
        GameConfiguration.Save();
        
        ServiceLocator.Global.Get(out player);
        
        //if(useSavedPlayerPosition)
            //player.SetInitialPosition(GameConfiguration.Data.playerPos);
        
        ResetGame(true);
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
    [HideInInspector] public bool isGamePaused = false, restartRequested = false, isInventoryOpen = false;
    [HideInInspector] public bool gameOver = false, reachedGoal = false;

    public GameDifficulty gameDifficulty { get; private set; } = GameDifficulty.Easy;

    void OnEnable()
    {
        GameEventManager.AddListener<GameDifficultyChangedEvent>(OnDifficultyChange);
        GameEventManager.AddListener<GameOverEvent>(OnGameOver);
        GameEventManager.AddListener<ReachedGoalEvent>(OnReachingGoal);
        GameEventManager.AddListener<InventoryVisibilityChangeEvent>(OnInventoryToggle);
    }

    void OnDisable()
    {
        GameEventManager.RemoveListener<GameDifficultyChangedEvent>(OnDifficultyChange);
        GameEventManager.RemoveListener<GameOverEvent>(OnGameOver);
        GameEventManager.RemoveListener<ReachedGoalEvent>(OnReachingGoal);
        GameEventManager.RemoveListener<InventoryVisibilityChangeEvent>(OnInventoryToggle);
        
        // no saving of data, if application is already closed OR GameState = GameOver
        if (!Application.isPlaying || fsm.CurrentState.GetType() != typeof(GameStateLost))
            return;
        
        // Save Configuration
        GameConfiguration.SaveCoinAmount(lastKnownCollectedCoins);
        GameConfiguration.SaveHealthPoints(lastKnownPlayerHealth);
        GameConfiguration.SavePlayer(lastKnownPlayerPosition);
        GameConfiguration.Save();  // save inventory on game closed
    }

    private void OnInventoryToggle(InventoryVisibilityChangeEvent e)
    {
        isInventoryOpen = e.isOpen;
    }

    private void OnGameOver(GameOverEvent e)
    {
        Debug.Log($"Player died at {e.deathPosition} by {e.killer.ToString()}");
        gameOver = true;
    }

    private void OnReachingGoal(ReachedGoalEvent e)
    {
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

    public void ResetGame(bool firstLoad = false)
    {
        // Restore from Configuration Save File
        gameDifficulty = GameConfiguration.Data.gameDifficulty;  // difficulty from Configuration-File
        GameEventManager.Raise(new GameDifficultyChangedEvent(gameDifficulty));
        if(!firstLoad || (useSavedPlayerPosition && firstLoad))
            player.SetInitialPosition(GameConfiguration.Data.playerPos);  // player position from Configuration-File
        player.SetInitialCoins(GameConfiguration.Data.coinAmount);  // collected coins from Configuration-File
        Inventory.Instance.InitializeInventory();  // load Inventory from Configurations-File (if possible)
        
        restartRequested = false;
        isGamePaused = false;
        gameOver = false;
        reachedGoal = false;
        
        // Spawn new GameObjects
        GameObjectFactory.Instance.SpawnGameObjects(GameObjectType.Sharks | GameObjectType.Items);
        
        // Set up FSM
        fsm = new FSM<GameManager>();
        fsm.Configure(this, new GameStateIntro());

        if (!firstLoad)
        {
            // reload scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public IEnumerator RestartRequest(float delay)
    {
        restartRequested = true;
        yield return new WaitForSeconds(delay);
        restartRequested = false;
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
