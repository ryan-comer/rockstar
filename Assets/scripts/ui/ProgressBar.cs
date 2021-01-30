using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{

    public Image background;
    public Image forground;

    public float moveSpeed = 5.0f;

    private float maxWidth;
    private float goalWidth;

    // Start is called before the first frame update
    void Start()
    {
        maxWidth = forground.rectTransform.rect.width; 
    }

    // Update is called once per frame
    void Update()
    {
        goTowardsGoal(); 
    }

    public void SetPercentage(float percentage)
    {
        percentage = Mathf.Min(percentage, 1.0f);
        goalWidth = percentage * maxWidth;
    }

    private void goTowardsGoal()
    {
        float newWidth = Mathf.Lerp(forground.rectTransform.rect.width, goalWidth, moveSpeed * Time.deltaTime);
        forground.rectTransform.sizeDelta = new Vector2(newWidth, forground.rectTransform.rect.height);
    }
}
