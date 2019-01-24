using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.Analytics;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class GameController : MonoBehaviour
{
    public static GameController instance;
    public GameObject splashButtonPanel;
    public int currentScene = 0;
    public float timeSinceLastAd = 0f;

    void Awake()
    {
        // This ensures that the game controller persists from scene to scene instead of being destroyed and re-created for each scene.
        if (instance == null)
        {
            DontDestroyOnLoad(gameObject);
            instance = this;
        }
        else if (instance != this)
            Destroy(gameObject);
    }

    void Start()
    {
        splashButtonPanel = GameObject.FindGameObjectWithTag("Menu");
        splashButtonPanel.SetActive(true);
    }

    public void ShowAd()
    {
        if (Advertisement.IsReady())
        {
            Advertisement.Show();
            timeSinceLastAd = Time.time;
        }
    }

    public void NewGame()
    {
        // Send a message to the Unity Analytics service that a new game has been started.
        AnalyticsEvent.GameStart();

        // Scene 0 is the title screen, so increment it to start the first level.
        currentScene++;
        LoadLevel();
    }

    public void LoadLevel()
    {
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(currentScene);
    }

    public void SaveAndQuit()
    {
        Save();
        Quit();
    }

    public void Quit()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

    public void Save()
    {
        PlayerData data = new PlayerData();
        data.currentScene = currentScene;

        FileStream file = File.Create(Application.persistentDataPath + "/playerInfo.dat");
        BinaryFormatter binaryformatter = new BinaryFormatter();
        binaryformatter.Serialize(file, data);
        file.Close();
    }

    public void Load()
    {
        if (File.Exists(Application.persistentDataPath + "/playerInfo.dat"))
        {
            FileStream file = File.Open(Application.persistentDataPath + "/playerInfo.dat", FileMode.Open);
            BinaryFormatter binaryformatter = new BinaryFormatter();
            PlayerData data = (PlayerData)binaryformatter.Deserialize(file);
            file.Close();
            currentScene = data.currentScene;

            // After completing the game, the player is returned to the title screen (Scene 0) AND
            // this playerInfo.dat file will exist. Hitting "Load" here will strand you on the title
            // screen. Therefore, if the currentScene is 0, start a new game.
            if (currentScene != 0)
                LoadLevel();
            else
                NewGame();
        }
        else
        {
            // If a player chooses "Load Game" instead of "New Game" on a fresh installation
            // (i.e. no playerInfo.dat has been created), this will load the first level.
            NewGame();
        }
    }
}

[Serializable]
class PlayerData
{
    public int currentScene;
}