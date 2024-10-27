using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// ---- For Only Host
public class DebugText : MonoBehaviour
{
    List<string> _text_list = new List<string>();
    public TMP_Text _debug_text;
    float timer = 0f;
    int tik = 0;

    public void PushDebugText(string text)
    {
        if (_text_list.Count > 2) PopDebugText();

        _text_list.Add(text);

        RenderDebugText();

        timer = 0f;
        tik = 0;
    }

    public string PopDebugText()
    {
        if (_text_list.Count <= 0) return "";

        string text = _text_list[0];
        _text_list.RemoveAt(0);

        RenderDebugText();

        return text;
    }

    public void FlushDebugText()
    {
        _text_list.Clear();
        _debug_text.text = "Debug Text";
    }

    void RenderDebugText()
    {
        _debug_text.text = "";
        foreach (string text in _text_list)
        {
            _debug_text.text += text + "\n";
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if ((int)timer > tik + 1)
        {
            tik = (int)timer;

            PopDebugText();
        }
    }
}
