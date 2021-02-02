using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeTextOnSliderChange : MonoBehaviour
{

    public Slider slider;
    public Text text;
    

    // Start is called before the first frame update
    void Start()
    {
        slider.onValueChanged.AddListener(onValueChanged);
        text.text = slider.value.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void onValueChanged(float newValue)
    {
        text.text = ((int)newValue).ToString();
    }
}
