using System;
using Systems.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

using GameConfiguration = Configurations.GameConfiguration;
using SaveDataState = Configurations.SaveDataState;

public class TitleScreenManager : MonoBehaviour
{
    [SerializeField] private Button _deleteGameStateButton;
    
    private void Awake()
    {
        LockCursor(false);
        if (_deleteGameStateButton == null)
            return;
        
        switch (GameConfiguration.GetSaveDataState())
        {
            case SaveDataState.NoSaveFile:
                // No save
                _deleteGameStateButton.gameObject.SetActive(false);
                break;
            case SaveDataState.Invalid:
                // Save is corrupted
                _deleteGameStateButton.gameObject.SetActive(false);
                break;
            case SaveDataState.Default:
                // Save exists but nothing has changed
                _deleteGameStateButton.gameObject.SetActive(false);
                break;
            case SaveDataState.Modified:
                // Player has saved progress
                _deleteGameStateButton.gameObject.SetActive(true);
                break;
        }
        
        switch (GameConfiguration.GetSaveDataState())
        {
            case SaveDataState.NoSaveFile:
                Debug.LogWarning("No save.");
                break;

            case SaveDataState.Invalid:
                Debug.LogWarning("Save is corrupted.");
                break;

            case SaveDataState.Default:
                Debug.LogWarning("Save exists but nothing has changed.");
                break;

            case SaveDataState.Modified:
                Debug.LogWarning("Player has saved progress.");
                break;
        }
    }

    public void StartGame()
    {
        SceneLoader.Instance.LoadSceneGroup(1);  // title screen has ID=0
    }
    
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void DeleteSavedGameState()
    {
        if (_deleteGameStateButton != null && _deleteGameStateButton.gameObject.activeSelf)
        {
            GameConfiguration.ResetToFactory();
            _deleteGameStateButton.gameObject.SetActive(false);
        }
    }
    
    public void LockCursor(bool isLocked)
    {
        Cursor.visible = !isLocked;
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
