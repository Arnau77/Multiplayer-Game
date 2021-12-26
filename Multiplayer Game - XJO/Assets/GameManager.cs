using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int gameTime;
    public static Action onPauseGame;

    public List<CharacterScript> prefabs;

    public List<CharacterScript> playersList;

    private NewClient client;

    private void Start()
    {
       
        StartCoroutine(TimeDown());
        client = FindObjectOfType<NewClient>();
        if (client == null)
            return;

        for (int i = 0; i < client.positionsDic.Count; i++)
        {
            SpawnPlayer(i,client.positionsDic[i]);

        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            onPauseGame?.Invoke();
        }
        if (playersList.Count <= 0)
            return;

        if(playersList[0].transform.position.x > playersList[1].transform.position.x)
        {
            playersList[0].transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            playersList[1].transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        }
        else
        {
            playersList[0].transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            playersList[1].transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        }

    }

    IEnumerator TimeDown()
    {
        int time = 99;
        while (time > 0)
        {
            yield return new WaitForSeconds(1);
            time -= 1;
            UIManager.onUpdateTimer?.Invoke(time);
        }
    }

    public void SpawnPlayer(int i, Vector3 pos)
    {

        CharacterScript character = Instantiate(prefabs[i], pos, Quaternion.identity);
        playersList.Add(character);
        character.client = client;
        character.ID = i;
        client.characterScripts.Add(character);

    }

}
