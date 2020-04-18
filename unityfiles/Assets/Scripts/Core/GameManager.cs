using System;
using UnityEngine;

/// <summary>
/// The event data for the game state changed events 
/// </summary>
public class GameStateChangeEventArgs : EventArgs {
    public GAME_STATE NewState { get; set; }
    public int NextSceneIndex { get; set; }
}

/// <summary>
/// The manager for the whole game main task is to start and stop scenes and huntingGameSceneNames
/// </summary>
public class GameManager : Singleton<GameManager> {

    // Time value for pause: 0 = time stops
    private static readonly int PAUSE = 0;

    // Time value for play/normal time: 1 = time acts as normal
    private static readonly int PLAY = 1;

    private GAME_STATE CurrentState { get; set; }

    private GameDataManager gameDataManager;

    public GameDataManager GameDataManager {
        get => gameDataManager;
    }

    /// <summary>
    /// This event tells the listeners the game state has changed
    /// </summary>
    public static event EventHandler<GameStateChangeEventArgs> GameStateChangeEvent;
    /// <summary>
    /// Subscribes to the relevant events for this class
    /// </summary>
    private void SubscribeToEvents() {
        // todo subscribe to OnPlayerDead, OnTimeOut, OnAllEnemiesDead
        LevelManager.OnLevelStateChangeEvent += CallbackLevelStateChangeEvent;
        LetterGameManager.OnLetterGameEndedEvent += CallbackLetterGameEnded;
    }

    /// <summary>
    /// Subscribes to the relevant events for this class
    /// </summary>
    private void UnsubscribeFromEvents() {
        // todo unsubscribe from OnPlayerDead, OnTimeOut, OnAllEnemiesDead
        // maybe that this also should be done on disable
        LevelManager.OnLevelStateChangeEvent -= CallbackLevelStateChangeEvent;
        LetterGameManager.OnLetterGameEndedEvent -= CallbackLetterGameEnded;
    }

    /// <summary>
    /// This function is fired when the OnLevelStateChangeEvent is invoked
    /// </summary>
    /// <param name="o">the object calling (this should always be the level manager)</param>
    /// <param name="args">the event args containing the new state</param>
    private void CallbackLevelStateChangeEvent(object o, LevelStateChangeEventArgs args) {

    }

    /// <summary>
    /// This function is fired when OnLetterGameEndedEvent is invoked
    /// This function will trigger on:
    ///     LetterGameManager OnDestroy()
    ///         the letter game is done and the total score from the letter
    ///         game level is transmitted with the event 
    /// </summary>
    /// <param name="args">the event args containing the total score from letter level</param>
    private void CallbackLetterGameEnded(object _, LetterGameEndedArgs args) {
        if (args.Score > 0) {
            gameDataManager.AddGameScore(args.Score);
        }
    }

    // -- private -- //

    /// <summary>
    /// Changes the game state 
    /// </summary>
    /// <param name="NewState">The new game state</param>
    public void GameStateChange(GAME_STATE NewState) {
        this.CurrentState = NewState;
        GameStateChangeEventArgs args = new GameStateChangeEventArgs();
        args.NewState = NewState;

        switch (NewState) {
            case GAME_STATE.PLAY:
                this.StartTime();
                break;
            case GAME_STATE.PAUSE:
                this.PauseTime();
                break;

            case GAME_STATE.EXIT:
                Application.Quit();
                break;
            default:
                Debug.LogError("🌮🌮🌮🌮  UNKNOWN GAME STATE  🌮🌮🌮🌮");
                break;
        }

        GameStateChangeEvent?.Invoke(this, args);
    }

    private void PauseTime() {
        Time.timeScale = PAUSE;
    }

    private void StartTime() {
        Time.timeScale = PLAY;
    }

    private void Awake() {
        this.gameDataManager = new GameDataManager();
        SubscribeToEvents();
    }

    private void OnDestroy() {
        UnsubscribeFromEvents();
        this.gameDataManager.SaveData();
    }
}