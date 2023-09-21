using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform RefTarget { get; set; } = null;
    public float LerpTime = 1.0f;
    public float WheelSpeed = 10.0f;

    private Vector3 _currentCamOffset;
    private List<Vector3> _camOffsets = new();
    private Vector3 _cameraOffsetFirst = new Vector3(-3f, 4.5f, -3f);
    private Vector3 _cameraOffsetSecond = new Vector3(-1.5f, 2f, -1.5f);
    private Vector3 _cameraOffsetThird = new Vector3(-0.75f, 1.5f, -0.75f);

    private Vector3 _currentCamAngleOffset;
    private List<Vector3> _camAngleOffsets = new();
    private Vector3 _cameraOffsetAnglesFirst = new Vector3(45f, 45f, 0f);
    private Vector3 _cameraOffsetAnglesSecond = new Vector3(15f, 45f, 0f);
    private Vector3 _cameraOffsetAnglesThird = new Vector3(10f, 45f, 0f);
    [SerializeField] private float _smoothTime = 0.3f;
    private Vector3 _targetPos;
    private Vector3 _velocity = Vector3.zero;

    private bool _isCamOnAction = false;
    private int _index = 0;
    
    void Awake()
    {
        _camOffsets.Add(_cameraOffsetFirst);
        _camOffsets.Add(_cameraOffsetSecond);
        _camOffsets.Add(_cameraOffsetThird);

        _camAngleOffsets.Add(_cameraOffsetAnglesFirst);
        _camAngleOffsets.Add(_cameraOffsetAnglesSecond);
        _camAngleOffsets.Add(_cameraOffsetAnglesThird);

        _currentCamOffset = _camOffsets[0];
        _currentCamAngleOffset = _camAngleOffsets[0];
        RefTarget = GameObject.FindGameObjectWithTag("Player").gameObject.transform;
        RefTarget.GetComponent<PlayerController>()._myCamera = this.GetComponent<Camera>();
        this.transform.position = RefTarget.transform.position + _currentCamOffset;
        _targetPos = RefTarget.transform.position + _currentCamOffset;
        this.transform.rotation = Quaternion.Euler(_currentCamAngleOffset);
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (_isCamOnAction) return;
        if (scroll > 0)
        {
            //카메라 위치, 오프셋 변화 (가까이)
            StartCoroutine(CamOffsetIncrease());
        }
        else if (scroll < 0)
        {
            //카메라 위치, 오프셋 변화 (멀리)
            StartCoroutine(CamOffsetDecrease());
        }
    }

    void LateUpdate()
    {
        if(this.transform.position != RefTarget.transform.position + _currentCamOffset)
        {
            _targetPos = RefTarget.transform.position + _currentCamOffset;
            this.transform.position = Vector3.SmoothDamp(transform.position, _targetPos, ref _velocity, _smoothTime);
        }
    }

    public Vector3 GetCameraOffsetAngles()
    {
        return _cameraOffsetAnglesFirst;
    }

    private IEnumerator CamOffsetIncrease() //Far Away
    {
        if (_currentCamOffset == _camOffsets[2] || _currentCamAngleOffset == _camAngleOffsets[2]) yield break;
        _isCamOnAction = true;
        _index++;
        float currentTime = 0;
        while(currentTime < LerpTime)
        {
            currentTime += Time.deltaTime;
            _currentCamOffset = Vector3.Lerp(_camOffsets[_index - 1], _camOffsets[_index], currentTime / LerpTime);
            _currentCamAngleOffset = Vector3.Lerp(_camAngleOffsets[_index - 1], _camAngleOffsets[_index], currentTime / LerpTime);
            this.transform.rotation = Quaternion.Euler(_currentCamAngleOffset);
            yield return null;
        }
        _isCamOnAction = false;
    }

    private IEnumerator CamOffsetDecrease() //Close
    {
        if (_currentCamOffset == _camOffsets[0] || _currentCamAngleOffset == _camAngleOffsets[0]) yield break;
        _isCamOnAction = true;
        _index--;
        float currentTime = 0;
        while (currentTime < LerpTime)
        {
            currentTime += Time.deltaTime;
            _currentCamOffset = Vector3.Lerp(_camOffsets[_index + 1], _camOffsets[_index], currentTime / LerpTime);
            _currentCamAngleOffset = Vector3.Lerp(_camAngleOffsets[_index + 1], _camAngleOffsets[_index], currentTime / LerpTime);
            this.transform.rotation = Quaternion.Euler(_currentCamAngleOffset);
            yield return null;
        }
        _isCamOnAction = false;
    }
}
