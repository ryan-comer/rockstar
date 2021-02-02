using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum GameSpeed
{
    SLOW,
    MEDIUM,
    FAST
}

public class GameSpeedOption : MonoBehaviour, IPointerClickHandler
{

    public Image borderImage;

    public GameSpeed gameSpeed = GameSpeed.SLOW;

    public delegate void ClickedDelegate(GameSpeedOption option);
    public event ClickedDelegate OnClicked;

    void Start()
    {
        borderImage.GetComponent<Image>().enabled = false;
    }

    private void selectOption()
    {
        borderImage.GetComponent<Image>().enabled = true;
    }

    public void UnselectOption()
    {
        borderImage.GetComponent<Image>().enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        selectOption();
        OnClicked?.Invoke(this);
    }

}
