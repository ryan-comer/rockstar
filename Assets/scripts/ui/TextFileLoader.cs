using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextFileLoader : MonoBehaviour
{

    public string fileName;

    private Text textComponent;

    // Start is called before the first frame update
    void Start()
    {
        textComponent = GetComponent<Text>();
        // No text to set
        if(textComponent == null)
        {
            return;
        }

        loadFile();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void loadFile()
    {
        string fileText = File.ReadAllText(Application.streamingAssetsPath + "/" + fileName);
        textComponent.text = fileText;
    }
}
