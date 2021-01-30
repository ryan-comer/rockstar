using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SongOption : MonoBehaviour, IPointerClickHandler
{

    public Image thumbnailImage;
    public Text titleText;
    public Image background;

    [HideInInspector]
    public string SongId { get; set; }

    public Color unselectedColor;
    public Color selectedColor;

    public string Title
    {
        get
        {
            return titleText.text;
        }
        set
        {
            titleText.text = value;
        }
    }

    void Start()
    {
        background.color = unselectedColor;
    }

    public void SetImage(string imagePath)
    {
        Texture2D spriteTexture = loadTexture(imagePath);
        Sprite sprite = Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height), new Vector2(0.5f, 0.5f));

        thumbnailImage.sprite = sprite;
        thumbnailImage.preserveAspect = true;
        thumbnailImage.rectTransform.localScale = thumbnailImage.rectTransform.localScale * .9f;
    }

    public void SelectOption()
    {
        background.color = selectedColor; 
    }

    public void UnSelectOption()
    {
        background.color = unselectedColor;
    }

    private Texture2D loadTexture(string imagePath)
    {
        Texture2D tex2D;
        byte[] fileData;

        fileData = System.IO.File.ReadAllBytes(imagePath);
        tex2D = new Texture2D(1, 1);
        if (tex2D.LoadImage(fileData))
        {
            return tex2D;
        }

        return null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SelectOption();
        SongController.Instance.UpdateSelectedSongOption(this);
    }
}
