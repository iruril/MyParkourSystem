using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    public float Speed = 5f;
    public float MyCurrentSpeed { get; set; } = 0;
    public float JumpPower = 5f;
    public float GravityForce = 9.8f;
    public float RotateSpeed = 30.0f;

    void Start()
    {
        MyCurrentSpeed = Speed;
    }
}
