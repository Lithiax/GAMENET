using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class GameManager : MonoBehaviourPunCallbacks
{
    public GameObject playerPrefab;

    [Header("Kill Feed")]
    public GameObject killFeedPrefab;
    public GameObject killFeedParent;

    [Header("WinScreen")]
    public GameObject winScreenPanel;
    public Text winnerName;
    public Text returnToLobbyTimerText;

    [Header("SpawnAreas")]
    public List<Transform> spawnAreas;

    public static GameManager instance;
    private bool gameEnd = false;
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }
    void Start()
    {
        if(PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.Instantiate(playerPrefab.name, GetRandomSpawnPoint(), Quaternion.identity);
        }
        winScreenPanel.SetActive(false); 
    }

    public Vector3 GetRandomSpawnPoint()
    {
        int randomIndex = Random.Range(0, spawnAreas.Count);
        return spawnAreas[randomIndex].position;
    }

    public void LogKillFeed(string killer, string victim)
    {
        GameObject killFeed = Instantiate(killFeedPrefab);
        killFeed.transform.SetParent(killFeedParent.transform);
        killFeed.transform.localScale = Vector3.one;

        killFeed.transform.Find("KillerName").GetComponent<Text>().text = killer;
        killFeed.transform.Find("KilledName").GetComponent<Text>().text = victim;
        Destroy(killFeed, 5.0f);
    }
    [PunRPC]
    public void WinGame(string winner)
    {
        if (gameEnd)
            return;
        gameEnd = true;
        winScreenPanel.SetActive(true);
        winnerName.text = "WINNER: " + winner;
        StartCoroutine(EndGame());  
    }
    IEnumerator EndGame()
    {
        winScreenPanel.SetActive(true);
        Time.timeScale = 0.2f;


        float respawnTime = 5.0f;
        while (respawnTime > 0)
        {
            yield return new WaitForSecondsRealtime(1.0f);
            respawnTime--;

            returnToLobbyTimerText.text = "Returning to Lobby in " + respawnTime.ToString(".00");
        }
        Time.timeScale = 1.0f;

        PhotonNetwork.LoadLevel("LobbyScene");
    }
}
