using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class TPSCamController : MonoBehaviour
{
    [SerializeField] private GameObject _aimPointUI;
    [SerializeField] private CinemachineVirtualCamera _aimCam;
    [SerializeField] private float _mouseRotateSpeed = 1.0f;

    [Header("카메라를 바라볼 각도 범위")]
    [SerializeField] private float _viewAngleY = 160;
    [SerializeField] private float _viewAngleX = 90;

    [Header("카메라를 바라보는 캐릭터의 Y축 회전 오프셋")]
    [SerializeField] private float _viewAngleYOffset = 45;

    private Camera _myCamera;
    public Transform CamTarget { get; private set; }
    public bool IsCamInSight { get; private set; } = false;

    private float _yAngleRotationEuler = 0;
    private float _xAngleRotationEuler = 0;

    private float _yRotation = 0;
    private float _xRotation = 0;

    private void Awake()
    {
        _myCamera = Camera.main;
        CamTarget = GameObject.FindWithTag("CamTarget").transform;
    }

    private void FixedUpdate()
    {
        IsCamInSight = IsCamInCharacterSight();
    }

    private void LateUpdate()
    {
        CamTarget.position = this.transform.position + Vector3.up * 1.4f;

        _yAngleRotationEuler = Input.GetAxis("Mouse X") * Time.deltaTime * _mouseRotateSpeed;
        _xAngleRotationEuler = -Input.GetAxis("Mouse Y") * Time.deltaTime * _mouseRotateSpeed;

        _yRotation = CamTarget.transform.eulerAngles.y + _yAngleRotationEuler;
        _xRotation = _xRotation + _xAngleRotationEuler;
        _xRotation = Mathf.Clamp(_xRotation, -60, 60);

        CamTarget.transform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0); 
    }

    public void ActivateAimModeCam()
    {
        _aimCam.gameObject.SetActive(true);
        _aimPointUI.SetActive(true);
    }

    public void DeactivateAimModeCam()
    {
        _aimCam.gameObject.SetActive(false);
        _aimPointUI.SetActive(false);
    }

    private bool IsCamInCharacterSight()
    {
        if (Vector3.Distance(_myCamera.transform.position, transform.position) < 1.0f) return false;

        Vector3 camDirection = _myCamera.transform.position - CamTarget.position;
        Vector3 myForward = Quaternion.Euler(0, _viewAngleYOffset, 0) * transform.forward;

        float xAxisGap = Mathf.Abs(Vector3.SignedAngle(camDirection, myForward, transform.right)) * 2;
        float yAxisGap = Mathf.Abs(Vector3.SignedAngle(camDirection, myForward, Vector3.up)) * 2;

        if (xAxisGap <= _viewAngleX && yAxisGap <= _viewAngleY)
        {
            return true;
        }
        return false;
    }
}