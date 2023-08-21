using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform RefTarget { get; set; } = null;
    [SerializeField] private Vector3 _cameraOffset = new Vector3(-3.5f, 8f, -3.5f);
    [SerializeField] private Vector3 _cameraOffsetAngles = new Vector3(60f, 45f, 0f);
    [SerializeField] private float _smoothTime = 0.3f;

    private Vector3 _targetPos;
    private Vector3 _velocity = Vector3.zero;
    
    void Awake()
    {
        RefTarget = GameObject.FindGameObjectWithTag("Player").gameObject.transform;
        RefTarget.GetComponent<PlayerController>()._myCamera = this.GetComponent<Camera>();
        this.transform.position = RefTarget.transform.position + _cameraOffset;
        _targetPos = RefTarget.transform.position + _cameraOffset;
        this.transform.rotation = Quaternion.Euler(_cameraOffsetAngles);
    }

    void Update()
    {
        if(this.transform.position != RefTarget.transform.position + _cameraOffset)
        {
            _targetPos = RefTarget.transform.position + _cameraOffset;
            this.transform.position = Vector3.SmoothDamp(transform.position, _targetPos, ref _velocity, _smoothTime);
        }
    }

    public Vector3 GetCameraOffsetAngles()
    {
        return _cameraOffsetAngles;
    }
}
