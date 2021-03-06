﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class AvailableShip : MonoBehaviour
{

    public Text ExperienceText;
    public Text ShopExperienceText;
    public Image ShipPreview;
    public Transform ShipContainer;
    public Button BuyButton;
    public GameObject ShipBuyPrefab;

    int currentShip;

    void Start()
    {
        currentShip = UserData.GetShipId();
        BuyButton.onClick.AddListener(OnBuyShip);
        ShipPreview.sprite = ShipProperties.GetShip(currentShip).ShipSprite;
        BuyButton.gameObject.SetActive(false);
    }

    void Update()
    {
        if (ShipContainer.childCount == 0 && ExperienceText.IsActive())
        {
            for (int i = 0; i < Constants.ShipsCount; i++)
            {
                if (ShipProperties.GetShip(i).ShipName != "Error" && ShipProperties.GetClass(i) < Constants.ClassCount)
                {
                    var s = Instantiate(ShipBuyPrefab) as GameObject;
                    s.transform.SetParent(ShipContainer);
                    s.transform.localScale = Vector3.one;
                    s.transform.localRotation = Quaternion.identity;
                    s.transform.localPosition = Vector3.zero;
                    s.transform.SetAsLastSibling();
                    s.GetComponentInChildren<Text>().text = ShipProperties.GetShip(i).ShipName;
                    s.name = ShipProperties.GetShip(i).ShipName;
                    int x = i;
                    s.GetComponent<Button>().onClick.AddListener(() => OnShipId(x));
                    s.GetComponent<Button>().interactable = true;
                }
            }
            UpdateShips();
            UpdateCredits();
        }
    }


    void UpdateShips()
    {
        for (int i = 0; i < Constants.ShipsCount; i++)
        {
            if (ShipProperties.GetShip(i).ShipName != "Error" && ShipProperties.GetClass(i) < Constants.ClassCount)
            {
                SetShipBought(i, UserData.HasBoughtShip(i));
                SetShipLocked(i, !(UserData.CanBuyShip(i) || UserData.HasBoughtShip(i)));
            }
        }
    }

    public void UpdateCredits()
    {
        if (ShopExperienceText.IsActive())
        {
            ShopExperienceText.text = "Credits : " + UserData.GetCredits().ToString();
        }
        if (ExperienceText.IsActive())
        {
            ExperienceText.text = "Credits : " + UserData.GetCredits().ToString();
        }
    }

    public void AddCredits(int credits)
    {
        UserData.AddCredits(credits);
        UpdateCredits();
    }

    void SetShipBought(int shipId, bool active)
    {
        GameObject ship = GameObject.Find(ShipProperties.GetShip(shipId).ShipName);
        ship.GetComponentInChildren<Text>().text = active ? ShipProperties.GetShip(shipId).ShipName : ShipProperties.GetShip(shipId).Price.ToString();
        ship.GetComponentInChildren<Text>().color = active ? Color.white : Color.gray;
    }

    void SetShipLocked(int shipId, bool locked)
    {
        GameObject ship = GameObject.Find(ShipProperties.GetShip(shipId).ShipName);
        ship.GetComponentInChildren<Text>().color = locked ? Color.gray : Color.white;
        //ship.transform.Find("Locked").gameObject.SetActive(locked);
    }

    public void OnShipId(int id)
    {
        BuyButton.gameObject.SetActive(false);
        if (!UserData.HasBoughtShip(id))
        {
            BuyButton.gameObject.SetActive(true);
            currentShip = id;
            ShipPreview.sprite = ShipProperties.GetShip(id).ShipSprite;
        }
        else
        {
            currentShip = id;
            ShipPreview.sprite = ShipProperties.GetShip(id).ShipSprite;
            UserData.SetShipId(id);
        }
        UpdateShips();
        UpdateCredits();
    }

    public void OnBuyShip()
    {
        if (UserData.CanBuyShip(currentShip))
        {
            UserData.BuyShip(currentShip);
            UserData.SetShipId(currentShip);
            BuyButton.gameObject.SetActive(false);
        }
        else
        {
            GameObject.Find("BuyXPButton").GetComponent<Button>().onClick.Invoke();
        }
        UpdateShips();
        UpdateCredits();
    }
}
