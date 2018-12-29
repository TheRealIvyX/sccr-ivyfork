﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfoText : MonoBehaviour
{
    public Text text;

    private void Start()
    {
        text.color = Color.clear;
    }

    public void showMessage(string message)
    {
        text.text = message;
        text.color = Color.white;
        StartCoroutine(fade());
    }

    IEnumerator fade()
    {
        yield return new WaitForSeconds(3f);
        while(text.color.a > 0f)
        {
            text.color = new Color(1f, 1f, 1f, text.color.a - Time.deltaTime);
            yield return new WaitForFixedUpdate();
        }
    }
}