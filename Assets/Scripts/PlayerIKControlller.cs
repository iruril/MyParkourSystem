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
    private bool _ikWeightSet = false;

    [SerializeField] private float _hipOffset = 0;
    private float _lastHipPositionY = 0;

    private Vector3 _leftFootIKPosition = Vector3.zero;
    private Vector3 _rightFootIKPosition = Vector3.zero;
    private Vector3 _lefttFootUpDirection;
    private Vector3 _rightFootUpDirection;

    private const string LeftFootIKWeight = "LeftFootIKWeight";
    private const string RightFootIKWeight = "RightFootIKWeight";

    private void Awake()
    {
        _animator = this.GetComponent<Animator>();
        _layerMask = _myGroundChecker.GroundLayer;

        _lefttFootUpDirection = -_animator.GetBoneTransform(HumanBodyBones.LeftFoot).forward;
        _rightFootUpDirection = -_animator.GetBoneTransform(HumanBodyBones.RightFoot).forward;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(_leftFootIKPosition, 0.05f);
        Gizmos.DrawWireSphere(_rightFootIKPosition, 0.05f);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (_player.IsOnDynamicMove)
        {
            if (!_ikWeightSet)
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
            FootIK();
            _ikWeightSet = false;
        }
    }

    private void FootIK()
    {
        CalculateIKState(AvatarIKGoal.LeftFoot, AvatarIKHint.LeftKnee, ref _leftFootIKPosition);
        CalculateIKState(AvatarIKGoal.RightFoot, AvatarIKHint.RightKnee, ref _rightFootIKPosition);
        //CalculateHipHeight();
    }

    private void CalculateIKState(AvatarIKGoal goal, AvatarIKHint hint , ref Vector3 IKPosition)
    {
        float weight;
        Vector3 footUpVector;
        if (goal == AvatarIKGoal.LeftFoot)
        {
            weight = _animator.GetFloat(LeftFootIKWeight);
            footUpVector = _lefttFootUpDirection;
        }
        else
        {
            weight = _animator.GetFloat(RightFootIKWeight);
            footUpVector = _rightFootUpDirection;
        }
        _animator.SetIKPositionWeight(goal, weight);
        _animator.SetIKRotationWeight(goal, weight);
        _animator.SetIKHintPositionWeight(hint, weight);

        Ray Ray = new Ray(_animator.GetIKPosition(goal) + Vector3.up * _kneeHeight, Vector3.down);
        Debug.DrawRay(_animator.GetIKPosition(goal) + Vector3.up * _kneeHeight, Vector3.down * _rayCheckDistance, Color.magenta);
        if (Physics.Raycast(Ray, out RaycastHit hitInfo, _rayCheckDistance, _layerMask))
        {
            IKPosition = hitInfo.point;

            Quaternion footIKRotation;
            if (goal == AvatarIKGoal.LeftFoot)
            {
                footIKRotation = Quaternion.FromToRotation(footUpVector, hitInfo.normal) * CalcutateFootRotation(HumanBodyBones.LeftLowerLeg);
            }
            else
            {
                footIKRotation = Quaternion.FromToRotation(footUpVector, hitInfo.normal) * CalcutateFootRotation(HumanBodyBones.RightLowerLeg);
            }
            _animator.SetIKPosition(goal, IKPosition);
            _animator.SetIKRotation(goal, footIKRotation);
        }
        else
        {
            if (goal == AvatarIKGoal.LeftFoot)
            {
                IKPosition = _animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                IKPosition.y = this.transform.position.y - _kneeHeight;
                _animator.SetIKPosition(goal, IKPosition);
            }
            else
            {
                IKPosition = _animator.GetBoneTransform(HumanBodyBones.RightFoot).position;
                IKPosition.y = this.transform.position.y - _kneeHeight;
                _animator.SetIKPosition(goal, IKPosition);
            }
        }
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

        float totalHipOffset = leftOffsetPos > rightOffsetPos ? rightOffsetPos : leftOffsetPos;

        Vector3 newHipPosition = _animator.bodyPosition + Vector3.up * totalHipOffset;
        newHipPosition.y = Mathf.Lerp(_lastHipPositionY, newHipPosition.y, HipUpDownSpeed);

        _animator.bodyPosition = newHipPosition;
        _lastHipPositionY = _animator.bodyPosition.y;
    }

    private IEnumerator WeightLerp()
    {
        _ikWeightSet = true;
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
