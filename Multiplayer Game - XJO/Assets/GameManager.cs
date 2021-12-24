using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public int gameTime;
    public static Action onPauseGame;


    private void Start()
    {
        StartCoroutine(TimeDown());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            onPauseGame?.Invoke();
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
}
