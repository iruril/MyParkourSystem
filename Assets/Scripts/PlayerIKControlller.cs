using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIKControlller : MonoBehaviour
{
    [SerializeField] private float _kneeHeight = 0.5f;
    [SerializeField] private float _rayCheckDistance = 1.0f;
    [Range(0, 1)] public float HipUpDownSpeed = 0.28f;
    [Range(0, 1)] public float FeetIKPositioningSpeed = 0.5f;

    private Animator _animator;
    [SerializeField] private GroundChecker _myGroundChecker;
    [SerializeField] private PlayerController _player;
    [SerializeField] private PlayerParkour _playerParkour;
    private LayerMask _layerMask;

    private float _animIKWeight = 0;
    private float _animIKWeightLerpTime = 0.35f;
    private bool _IkWeightSetOnDynamicMove = false;

    [SerializeField] private float _hipOffset = 0;

    private Vector3 _leftFootPosition = Vector3.zero;
    private Vector3 _rightFootPosition = Vector3.zero;
    private Vector3 _leftFootIKPosition = Vector3.zero;
    private Vector3 _rightFootIKPosition = Vector3.zero;

    private float _lastHipPositionY;
    private float _lastLeftFootPositionY;
    private float _lastRightFootPositionY;

    private Vector3 _lefttFootUpDirection;
    private Vector3 _rightFootUpDirection;

    private Quaternion _leftFootIKRotation;
    private Quaternion _rightFootIKRotation;

    private const string LeftFootIKWeight = "LeftFootIKWeight";
    private const string RightFootIKWeight = "RightFootIKWeight";

    private void Awake()
    {
        _animator = this.GetComponent<Animator>();
        _layerMask = _myGroundChecker.GroundLayer;

        _lefttFootUpDirection = -_animator.GetBoneTransform(HumanBodyBones.LeftFoot).forward;
        _rightFootUpDirection = -_animator.GetBoneTransform(HumanBodyBones.RightFoot).forward;
    }

    private void FixedUpdate()
    {
        CalculateFeetTarget(ref _leftFootPosition, HumanBodyBones.LeftFoot);
        CalculateFeetTarget(ref _rightFootPosition, HumanBodyBones.RightFoot);

        FeetPositionSelecter(_leftFootPosition, HumanBodyBones.LeftLowerLeg, _lefttFootUpDirection, ref _leftFootIKPosition, ref _leftFootIKRotation);
        FeetPositionSelecter(_rightFootPosition, HumanBodyBones.RightLowerLeg, _rightFootUpDirection, ref _rightFootIKPosition, ref _rightFootIKRotation);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (_player.IsOnDynamicMove)
        {
            if (!_IkWeightSetOnDynamicMove)
            {
                StartCoroutine(WeightLerp());
            }

            if (_player.JumpMode == PlayerParkour.JumpState.Vault)
            {
                _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _animIKWeight);

                _animator.SetIKPosition(AvatarIKGoal.LeftHand, _playerParkour.LeftStepPoint);
            }
            else if (_player.JumpMode == PlayerParkour.JumpState.JumpClimb)
            {
                if (_playerParkour.StepMode == PlayerParkour.StepState.Middle || _playerParkour.StepMode == PlayerParkour.StepState.Highest)
                {
                    _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _animIKWeight);
                    _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, _animIKWeight);

                    _animator.SetIKPosition(AvatarIKGoal.LeftHand, _playerParkour.LeftStepPoint);
                    _animator.SetIKPosition(AvatarIKGoal.RightHand, _playerParkour.RightStepPoint);
                }
            }
        }
        else
        {
            _IkWeightSetOnDynamicMove = false;
            FootIK();
        }
    }

    private void FootIK()
    {
        CalculateHipHeight();

        _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, _animator.GetFloat(LeftFootIKWeight));
        FeetIKPositioning(AvatarIKGoal.LeftFoot, _leftFootIKPosition, _leftFootIKRotation, ref _lastLeftFootPositionY);

        _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, _animator.GetFloat(RightFootIKWeight));
        FeetIKPositioning(AvatarIKGoal.RightFoot, _rightFootIKPosition, _rightFootIKRotation, ref _lastRightFootPositionY);
    }

    private void FeetIKPositioning(AvatarIKGoal foot, Vector3 positionIK, Quaternion rotationIK, ref float lastFootPositionY)
    {
        Vector3 targetIKPosition = _animator.GetIKPosition(foot);

        if(positionIK != Vector3.zero)
        {
            targetIKPosition = transform.InverseTransformPoint(targetIKPosition);
            positionIK = transform.InverseTransformPoint(positionIK);

            float yLerpValue = Mathf.Lerp(lastFootPositionY, positionIK.y, FeetIKPositioningSpeed);
            targetIKPosition.y += yLerpValue;

            lastFootPositionY = yLerpValue;
            targetIKPosition = transform.TransformPoint(targetIKPosition);

            _animator.SetIKRotation(foot, rotationIK);
        }
        _animator.SetIKPosition(foot, targetIKPosition);
    }

    private void CalculateHipHeight()
    {
        if (_leftFootIKPosition == Vector3.zero || _rightFootIKPosition == Vector3.zero || _lastHipPositionY == 0)
        {
            _lastHipPositionY = _animator.bodyPosition.y;
            return;
        }

        float leftOffsetPos = _leftFootIKPosition.y - this.transform.position.y;
        float rightOffsetPos = _rightFootIKPosition.y - this.transform.position.y;

        float totalHipOffset = (leftOffsetPos < rightOffsetPos) ? leftOffsetPos : rightOffsetPos;

        Vector3 newHipPosition = _animator.bodyPosition + Vector3.up * totalHipOffset;
        newHipPosition.y = Mathf.Lerp(_lastHipPositionY, newHipPosition.y, HipUpDownSpeed);

        _animator.bodyPosition = newHipPosition;
        _lastHipPositionY = _animator.bodyPosition.y;
    }

    private void FeetPositionSelecter(Vector3 rayOriginPosition, HumanBodyBones knee, Vector3 footUpVector, ref Vector3 feetIKPosition, ref Quaternion feetIKRotation)
    {
        RaycastHit hitInfo;

        Debug.DrawLine(rayOriginPosition, rayOriginPosition + Vector3.down * (_rayCheckDistance + _kneeHeight), Color.magenta);
        if(Physics.Raycast(rayOriginPosition, Vector3.down, out hitInfo, _rayCheckDistance + _kneeHeight, _layerMask))
        {
            feetIKPosition = rayOriginPosition;
            feetIKPosition.y = hitInfo.point.y + _hipOffset;
            feetIKRotation = Quaternion.FromToRotation(footUpVector, hitInfo.normal) * CalcutateFootRotation(knee);

            return;
        }
        feetIKPosition = Vector3.zero;
    }

    private void CalculateFeetTarget(ref Vector3 feetPosition, HumanBodyBones foot)
    {
        feetPosition = _animator.GetBoneTransform(foot).position;
        feetPosition.y = transform.position.y + _rayCheckDistance;
    }

    private IEnumerator WeightLerp()
    {
        _IkWeightSetOnDynamicMove = true;
        float time = 0;
        while (time <= _animIKWeightLerpTime)
        {
            _animIKWeight = Mathf.Lerp(0, 1.0f, time / _animIKWeightLerpTime);
            time += Time.deltaTime;
            yield return null;
        }
        time = 0;
        while (time <= _animIKWeightLerpTime)
        {
            _animIKWeight = Mathf.Lerp(1.0f, 0.3f, time / _animIKWeightLerpTime);
            time += Time.deltaTime;
            yield return null;
        }
        yield break;
    }

    private Quaternion CalcutateFootRotation(HumanBodyBones kneeBone)
    {
        Vector3 kneeForward = -_animator.GetBoneTransform(kneeBone).forward;
        kneeForward.y = 0;
        return this.transform.rotation * Quaternion.FromToRotation(this.transform.forward, kneeForward.normalized);
    }
}
