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

    private const string _weaponDataPath = "WeaponDataList/WeaponList.json";

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        BetterStreamingAssets.Initialize();
    }

    private void Start()
    {
        WeaponDataLoad();
    }

    private void WeaponDataLoad()
    {
        string jsonData = FileUtility.LoadFile(_weaponDataPath);
        Dictionary<string, List<Weapon>> weaponList = JsonConvert.DeserializeObject<Dictionary<string, List<Weapon>>>(jsonData);
        foreach (var item in weaponList[Constants.WeaponList])
        {
            this.WeaponList.Add(item);
        }
        IsWeaponListLoaded = true;
    }

    
}
