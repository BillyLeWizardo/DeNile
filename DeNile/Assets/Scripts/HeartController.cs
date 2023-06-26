using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartController : MonoBehaviour
{
    PlayerController player;

    private GameObject[] heartContainers;
    private Image[] heartFills;
    public Transform heartsParent;
    public GameObject heartContainerPrefab;

    // Start is called before the first frame update
    void Start()
    {
        player = PlayerController.Instance; //Sets the player variable to the current instance of the player

        heartContainers = new GameObject[PlayerController.Instance.maxHealth]; //Makes heart containers for the players max health
        heartFills = new Image[PlayerController.Instance.maxHealth]; //Makes heart fills for the players max health

        PlayerController.Instance.onHealthChangedCallback += UpdateFilledHearts; //Updates the player's hearts that are filled, is a delegate due to it both increasing/decreasing

        InstantiateHeartContainers(); //Creates heart containers on the UI
        UpdateFilledHearts(); //Changes heart containers based on how much health the player has compared to their max health
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void setHeartContainers()
    {
        for(int i = 0; i < heartContainers.Length; i++) //For loop to create a heart container for as many points of health the player has
        {
            if(i < PlayerController.Instance.maxHealth)
            {
                heartContainers[i].SetActive(true);
            }
            else
            {
                heartContainers[i].SetActive(false);
            }
        }
    }

    void setFilledHearts()
    {
        for(int i = 0; i < heartFills.Length; i++) //Does the same as above but for each fill
        {
            if(i < PlayerController.Instance.health)
            {
                heartFills[i].fillAmount = 1;
            }
            else
            {
                heartFills[i].fillAmount = 0;
            }
        }
    }
    void InstantiateHeartContainers()
    {
        for( int i = 0;i < PlayerController.Instance.maxHealth; i++)
        {
            GameObject heartContainer = Instantiate(heartContainerPrefab); //Instantiates a heart container for each point of health the player has
            heartContainer.transform.SetParent(heartsParent, false); //Sets the heart containers transform to a certain grid in the heartsparent gameobject
            heartContainers[i] = heartContainer;
            heartFills[i] = heartContainer.transform.Find("HeartFill").GetComponent<Image>(); //Fills each heart
        }
    }

    void UpdateFilledHearts()
    {
        setHeartContainers();
        setFilledHearts();
    }

}
