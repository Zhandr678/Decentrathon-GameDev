using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Environment : MonoBehaviour
{
    public GameObject finish_line;

    void Start()
    {
        finish_line.GetComponent<Renderer>().enabled = false;
    }

    void Update()
    {
        
    }
}
