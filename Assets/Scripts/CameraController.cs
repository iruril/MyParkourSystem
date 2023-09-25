using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform RefTarget { get; set; } = null;
    public float LerpTime = 1.0f;
    public float WheelSpeed = 10.0f;

    #region Dynamic Cam-Offset Variables
    private Vector3 _currentCamOffset;
    private List<Vector3> _camOffsets = new();
    [SerializeField] private Vector3 _cameraOffsetFirst = new Vector3(-3f, 4.5f, -3f);
    [SerializeField] private Vector3 _cameraOffsetSecond = new Vector3(-1.5f, 2f, -1.5f);
    [SerializeField] private Vector3 _cameraOffsetThird = new Vector3(-0.75f, 1.5f, -0.75f);

    private List<Quaternion> _camAngleOffsets = new();
    [SerializeField] private Vector3 _cameraOffsetAnglesFirst = new Vector3(45f, 45f, 0f);
    [SerializeField] private Vector3 _cameraOffsetAnglesSecond = new Vector3(15f, 45f, 0f);
    [SerializeField] private Vector3 _cameraOffsetAnglesThird = new Vector3(10f, 45f, 0f);

    [SerializeField] private float _smoothTime = 0.3f;
    private Vector3 _targetPos;
    private Vector3 _velocity = Vector3.zero;

    private bool _isCamOnAction = false;
    private int _currentCamOffsetIndex = 0;
    #endregion

    public List<GameObject> detectedObjects = new List<GameObject>();
    private Vector3 _screenCenter = Vector3.zero;
    public LayerMask _fadeObjectLayerMask;

    void Awake()
    {
        _camOffsets.Add(_cameraOffsetFirst);
        _camOffsets.Add(_cameraOffsetSecond);
        _camOffsets.Add(_cameraOffsetThird);

        _camAngleOffsets.Add(Quaternion.Euler(_cameraOffsetAnglesFirst));
        _camAngleOffsets.Add(Quaternion.Euler(_cameraOffsetAnglesSecond));
        _camAngleOffsets.Add(Quaternion.Euler(_cameraOffsetAnglesThird));

        _currentCamOffset = _camOffsets[0];
        RefTarget = GameObject.FindGameObjectWithTag("Player").gameObject.transform;
        RefTarget.GetComponent<PlayerController>()._myCamera = this.GetComponent<Camera>();
        this.transform.position = RefTarget.transform.position + _currentCamOffset;
        _targetPos = RefTarget.transform.position + _currentCamOffset;
        this.transform.rotation = _camAngleOffsets[0];
        _screenCenter = new Vector3(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2);
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (_isCamOnAction) return;
        if (scroll > 0)
        {
            StartCoroutine(CamOffsetIncrease()); //카메라 위치, 오프셋 변화 (가까이)
        }
        else if (scroll < 0)
        {
            StartCoroutine(CamOffsetDecrease()); //카메라 위치, 오프셋 변화 (멀리)
        }
    }

    private void FixedUpdate()
    {
        ObjectFadeOut();
        RemoveNotDetectedObject();
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

    #region Camera Zoom-In and Out Fields
    private IEnumerator CamOffsetIncrease() //Close 
    {
        if (_currentCamOffset == _camOffsets[2]) yield break;
        _isCamOnAction = true;
        _currentCamOffsetIndex++;
        float currentTime = 0;
        while(currentTime < LerpTime)
        {
            currentTime += Time.deltaTime;
            _currentCamOffset = Vector3.Lerp(_camOffsets[_currentCamOffsetIndex - 1], _camOffsets[_currentCamOffsetIndex], currentTime / LerpTime);
            this.transform.rotation = Quaternion.Lerp(_camAngleOffsets[_currentCamOffsetIndex - 1], _camAngleOffsets[_currentCamOffsetIndex], currentTime / LerpTime);
            yield return null;
        }
        _isCamOnAction = false;
    }

    private IEnumerator CamOffsetDecrease() //Far Away
    {
        if (_currentCamOffset == _camOffsets[0]) yield break;
        _isCamOnAction = true;
        _currentCamOffsetIndex--;
        float currentTime = 0;
        while (currentTime < LerpTime)
        {
            currentTime += Time.deltaTime;
            _currentCamOffset = Vector3.Lerp(_camOffsets[_currentCamOffsetIndex + 1], _camOffsets[_currentCamOffsetIndex], currentTime / LerpTime);
            this.transform.rotation = Quaternion.Lerp(_camAngleOffsets[_currentCamOffsetIndex + 1], _camAngleOffsets[_currentCamOffsetIndex], currentTime / LerpTime);
            yield return null;
        }
        _isCamOnAction = false;
    }
    #endregion

    #region Object Fade-Out Fields
    private void ObjectFadeOut()
    {
        Ray ray = Camera.main.ScreenPointToRay(_screenCenter);
        RaycastHit[] hits;
        hits = Physics.RaycastAll(ray, Vector3.Distance(RefTarget.position, this.transform.position), _fadeObjectLayerMask);

        foreach (RaycastHit hit in hits)
        {
            GameObject detectedObject = hit.collider.gameObject;
            if (!detectedObjects.Contains(detectedObject))
            {
                ObjectFade objFade = detectedObject.GetComponent<ObjectFade>();
                if (objFade != null) objFade.FadeOut();
                detectedObjects.Add(detectedObject);
            }
        }
    }

    private void RemoveNotDetectedObject()
    {
        for (int i = detectedObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = detectedObjects[i];
            if (!IsDetectedObject(obj))
            {
                ObjectFade objFade = obj.GetComponent<ObjectFade>();
                if (objFade != null) objFade.FadeIn();
                detectedObjects.RemoveAt(i);
            }
        }
    }

    private bool IsDetectedObject(GameObject obj)
    {
        Ray ray = Camera.main.ScreenPointToRay(_screenCenter);
        RaycastHit[] hits;
        hits = Physics.RaycastAll(ray, Vector3.Distance(RefTarget.position, this.transform.position), _fadeObjectLayerMask);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == obj)
            {
                return true;
            }
        }
        return false;
    }
    #endregion
}
