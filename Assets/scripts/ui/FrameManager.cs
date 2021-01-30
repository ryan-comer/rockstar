using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameManager : MonoBehaviour
{
    public static FrameManager Instance;

    public RectTransform[] frames;  // The frames that you can show
    public int startingFrame;   // Frame to start with
    public float sizeChangeRate;    // The rate of change for the size
    public float sizeChangeOffset = 0.01f;

    private int activeFrameNumber;
    private bool isAnimating;

    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        hideAllFrames();
        showStartingFrame();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowFrame(int frameNumber)
    {
        if(frameNumber > frames.Length)
        {
            return;
        }

        // Already animating
        if (isAnimating)
        {
            return;
        }

        // Hide the current frame
        StartCoroutine(sizingCoroutine(frames[activeFrameNumber], frames[activeFrameNumber].localScale, 0));

        // Show the next frame
        StartCoroutine(sizingCoroutine(frames[frameNumber], frames[frameNumber].localScale, 1));

        activeFrameNumber = frameNumber;
    }

    // Coroutine to make the frames bigger/smaller
    // 0 - shrink, 1 - grow
    private IEnumerator sizingCoroutine(RectTransform frame, Vector3 realScale, int direction)
    {
        isAnimating = true;

        switch (direction)
        {
            // Shrink
            case 0:
                while (frame.localScale.magnitude > sizeChangeOffset)
                {
                    frame.localScale = Vector3.Lerp(frame.localScale, Vector3.zero, sizeChangeRate * Time.deltaTime);
                    yield return new WaitForEndOfFrame();
                }
                frame.gameObject.SetActive(false);
                frame.localScale = realScale;
                break;
            // Grow
            case 1:
                frame.localScale = Vector3.zero;
                frame.gameObject.SetActive(true);
                while(frame.localScale.magnitude < realScale.magnitude - sizeChangeOffset)
                {
                    frame.localScale = Vector3.Lerp(frame.localScale, realScale, sizeChangeRate * Time.deltaTime);
                    yield return new WaitForEndOfFrame();
                }
                frame.localScale = realScale;
                break;
        }

        isAnimating = false;
    }

    private void showStartingFrame()
    {
        // No frames to show
        if(frames.Length == 0)
        {
            return;
        }

        frames[startingFrame].gameObject.SetActive(true);
        activeFrameNumber = startingFrame;
    }

    private void hideAllFrames()
    {
        foreach(var frame in frames)
        {
            frame.gameObject.SetActive(false);
        }
    }

}
