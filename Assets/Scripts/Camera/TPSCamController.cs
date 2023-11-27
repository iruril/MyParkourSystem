using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class TPSCamController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera _aimCam;
    [SerializeField] private float _normalSensitivity;
    [SerializeField] private float _aimSensitivity;
    [SerializeField] private float _mouseRotateSpeed = 1.0f;

    public Transform CamTarget;
    private void Update()
    {
        CamTarget.position = this.transform.position + Vector3.up * 1.4f;
        CamTarget.Rotate(0f, Input.GetAxis("Mouse X") * _mouseRotateSpeed, 0f, Space.World);
        CamTarget.Rotate(-Input.GetAxis("Mouse Y") * _mouseRotateSpeed, 0f, 0f);
    }

    public void ActivateAimModeCam()
    {
        _aimCam.gameObject.SetActive(true);
    }
    public void DeactivateAimModeCam()
    {
        _aimCam.gameObject.SetActive(false);
    }
}