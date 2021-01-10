﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class WCBetterBarHandler : MonoBehaviour
{
    public GameObject betterBarButtonPrefab;
    public Transform betterBarContents;
    public ItemHandler itemHandler;
    public Sprite betterButtonActive;
    public Sprite betterButtonInactive;
    public int currentActiveButton;
    public Text itemName;
    public List<Image> images = new List<Image>();
    public int minButton = 0;
    public int maxButton = 7;
    public int padding = 1;
    public WorldCreatorCursor cursor;
    public GameObject optionButtonPrefab;
    public Transform optionGridTransform;
    private List<OptionButton> activeOptionButtons = new List<OptionButton>();
    public GameObject tooltipPrefab;
	private RectTransform tooltipTransform;
    public WCWorldIO WCWorldIO;
    public Sprite playButtonImage;

    /// <summary>
    /// Option buttons for the World Creator
    /// </summary>
    [System.Serializable]
    public class OptionButton
    {
        public string tooltip;
        public Sprite sprite;
        public Image imgRef;
        public Button.ButtonClickedEvent action;
    }

    public List<OptionButton> globalButtons;

    void AddOptionButton(int i, List<OptionButton> buttonList)
    {
        var gObj = Instantiate(optionButtonPrefab, optionGridTransform);
        var button = buttonList[i];
        button.imgRef = gObj.GetComponent<Image>();
        if(button.sprite) 
        {
            var headerImg = gObj.GetComponentsInChildren<Image>()[1];
            headerImg.sprite = button.sprite;
            headerImg.rectTransform.sizeDelta = button.sprite.bounds.size * 100;
        }
        else gObj.GetComponentsInChildren<Image>()[1].enabled = false;
        var buttonScript = button.imgRef.gameObject.AddComponent<Button>();
        buttonScript.onClick = button.action;
        activeOptionButtons.Add(button);
    }

    void Start()
    {
        for(int i = 0; i < itemHandler.itemPack.items.Count; i++) {
            var buttonObj = Instantiate(betterBarButtonPrefab, betterBarContents, false);
            var x = i;
            buttonObj.AddComponent<Button>().onClick.AddListener(new UnityAction(() => {
                currentActiveButton = x;
                cursor.SetCurrent(x);
            }));
            var img = buttonObj.GetComponent<Image>();
            images.Add(img);
            if(itemHandler.itemPack.items[i].assetID != "")
            {
                var print = ResourceManager.GetAsset<EntityBlueprint>(itemHandler.itemPack.items[i].assetID);
                if(print)
                {
                    buttonObj.transform.Find("MaskObj").Find("StandardImage").gameObject.SetActive(false);
                    var scaler = buttonObj.transform.Find("MaskObj").Find("EntityDisplay").transform;
                    
                    switch(print.intendedType)
                    {
                        case EntityBlueprint.IntendedType.AirCarrier:
                        case EntityBlueprint.IntendedType.GroundCarrier:
                        case EntityBlueprint.IntendedType.Yard:
                        case EntityBlueprint.IntendedType.Trader:
                        case EntityBlueprint.IntendedType.WeaponStation:
                        case EntityBlueprint.IntendedType.CoreUpgrader:
                        case EntityBlueprint.IntendedType.DroneWorkshop:
                            scaler.localScale = new Vector3(0.15f, 0.15f, 1);
                            break;
                        default:
                            scaler.localScale = new Vector3(0.3f, 0.3f, 1);
                            break;
                    }
                    
                    var sdh = buttonObj.GetComponentInChildren<SelectionDisplayHandler>();
                    sdh.AssignDisplay(print, print.intendedType == EntityBlueprint.IntendedType.Drone ? DroneUtilities.GetDefaultData(print.customDroneType) : null);
                }
                else
                {
                    var obj = itemHandler.itemPack.items[i].obj;
                    SetStandardImage(obj, buttonObj, 0.5f);
                }
            }
            else
            {
                
                var obj = itemHandler.itemPack.items[i].obj;
                SetStandardImage(obj, buttonObj, 0.5f);
            }
        }

        for(int i = 0; i < globalButtons.Count; i++)
        {
            AddOptionButton(i, globalButtons);
        }
    }

    void SetStandardImage(GameObject obj, GameObject buttonObj, float scale=1)
    {
        buttonObj.transform.Find("MaskObj").Find("EntityDisplay").gameObject.SetActive(false);
        var spriteList = obj.GetComponentsInChildren<SpriteRenderer>();
        var standardImage = buttonObj.transform.Find("MaskObj").Find("StandardImage");
        standardImage.localScale = new Vector3(scale,scale,1);
        var standardImageList = standardImage.GetComponentsInChildren<Image>();
        standardImageList[0].sprite = spriteList[0].sprite;
        standardImageList[0].color = spriteList[0].color;
        if(spriteList.Length > 1 && spriteList[1].sprite.name != "minimapsquare")
        {
            standardImageList[1].sprite = spriteList[1].sprite;
            standardImageList[0].color = spriteList[0].color;
        }
        else
            standardImageList[1].enabled = false;
    }

    void Update()
    {
        currentActiveButton = cursor.currentIndex;

        foreach(Image image in images)
        {
            image.sprite = betterButtonInactive;
        }
        var test = betterBarContents.GetChild(currentActiveButton).GetComponent<Image>();
        test.sprite = betterButtonActive;
        if((currentActiveButton > maxButton - padding) && currentActiveButton < images.Count - 1)
        {
            minButton++;
            maxButton++;
            (betterBarContents as RectTransform).anchoredPosition = new Vector2(-(maxButton - 7) * 125 ,0);
        }
        else if(currentActiveButton < minButton + padding && currentActiveButton > 0)
        {
            minButton--;
            maxButton--;
            (betterBarContents as RectTransform).anchoredPosition = new Vector2(-(maxButton - 7) * 125 ,0);
        }

        itemName.text = itemHandler.itemPack.items[currentActiveButton].name.ToUpper();

        // Instantiate tooltip. Destroy tooltip if mouse is not over a sector image.
		bool mouseOverSector = false;
		foreach(var optionButton in activeOptionButtons)
		{
			var pos = optionButton.imgRef.rectTransform.position;
			var sizeDelta = optionButton.imgRef.rectTransform.sizeDelta;
			var newRect = new Rect(pos.x - sizeDelta.x / 2, pos.y - sizeDelta.y / 2, sizeDelta.x, sizeDelta.y);
			// Mouse over sector. Instantiate tooltip if necessary, move tooltip and set text up
			if(newRect.Contains(Input.mousePosition))
			{
				if(!tooltipTransform) tooltipTransform = Instantiate(tooltipPrefab, transform.parent).GetComponent<RectTransform>();
				tooltipTransform.position = Input.mousePosition;
				mouseOverSector = true;
				var text = tooltipTransform.GetComponentInChildren<Text>();
				text.text = 
					$"{optionButton.tooltip}".ToUpper();
				tooltipTransform.GetComponent<RectTransform>().sizeDelta = new Vector2(text.preferredWidth + 16f, text.preferredHeight + 16);

			}
		}
		if(!mouseOverSector) 
		{
			if(tooltipTransform) Destroy(tooltipTransform.gameObject);
		}


    }
}