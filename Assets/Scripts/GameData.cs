using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    public Dictionary<int, Vector3> car_positions = new Dictionary<int, Vector3>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
    }

    
    void Update()
    {
        
    }
}
