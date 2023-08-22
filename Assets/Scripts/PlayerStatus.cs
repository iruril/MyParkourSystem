using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    public float Speed = 5f;
    public float MyCurrentSpeed { get; set; } = 0;
    public float JumpPower = 5f;
    public float _gravityForce = 9.8f;

    void Start()
    {
        MyCurrentSpeed = Speed;
    }
}
