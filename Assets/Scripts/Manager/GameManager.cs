using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType(typeof(GameManager)) as GameManager;

                if (_instance == null)
                {
                    Debug.Log("No Singleton");
                }
            }
            return _instance;
        }
    }

    public GameObject MyPlayer { get; set; }

    public List<Weapon> WeaponList = new(); //For Debug

    public bool IsWeaponListLoaded = false;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            WeaponDataLoad();
            
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void WeaponDataLoad()
    {
        string jsonData = File.ReadAllText(Application.dataPath + "/Resources/Json/Items/WeaponList.json");
        Dictionary<string, List<Weapon>> weaponList = JsonConvert.DeserializeObject<Dictionary<string, List<Weapon>>>(jsonData);
        foreach (var item in weaponList[Constants.WeaponList])
        {
            this.WeaponList.Add(item);
        }
        IsWeaponListLoaded = true;
    }
}
