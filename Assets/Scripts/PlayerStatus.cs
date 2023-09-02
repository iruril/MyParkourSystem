using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    public float Speed { get; private set; } = 5f;
    public float MyCurrentSpeed { get; set; } = 0;
    public float JumpPower { get; private set; } = 5f;
    public float GravityForce { get; private set; } = 9.8f;
    public float RotateSpeed { get; private set; } = 30.0f;

    void Start()
    {
        MyCurrentSpeed = Speed;
    }
}
