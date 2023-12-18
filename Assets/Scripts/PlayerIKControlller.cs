using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIKControlller : MonoBehaviour
{
    [SerializeField] private float _kneeHeight = 0.5f;
    [SerializeField] private float _heelHeight = 0.02f;
    [SerializeField] private float _rayCheckDistance = 1.0f;
    public float HipUpDownSpeed = 5.0f;
    public float FeetIKPositioningSpeed = 5.0f;

    [SerializeField] private float _defaultBodyPositionY = 0.85f;
    [SerializeField] private float _aimBodyPositionY = 0.6f;

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
        CalculateHipHeight();
        CalculateIKState(AvatarIKGoal.LeftFoot, HumanBodyBones.LeftFoot, ref _leftFootIKPosition, LeftFootIKWeight, _lefttFootUpDirection, HumanBodyBones.LeftLowerLeg);
        CalculateIKState(AvatarIKGoal.RightFoot, HumanBodyBones.RightFoot, ref _rightFootIKPosition, RightFootIKWeight, _rightFootUpDirection, HumanBodyBones.RightLowerLeg);
    }

    private void CalculateIKState(AvatarIKGoal goal, HumanBodyBones foot, ref Vector3 footIKPosition, string weightName, Vector3 footUp, HumanBodyBones knee)
    {
        float weight;
        weight = _animator.GetFloat(weightName);

        _animator.SetIKPositionWeight(goal, weight);
        _animator.SetIKRotationWeight(goal, weight);

        Ray Ray = new Ray(_animator.GetIKPosition(goal) + Vector3.up * _kneeHeight, Vector3.down);
        Debug.DrawRay(_animator.GetIKPosition(goal) + Vector3.up * _kneeHeight, Vector3.down * (_rayCheckDistance + _kneeHeight), Color.magenta);
        if (Physics.Raycast(Ray, out RaycastHit hitInfo, _rayCheckDistance + _kneeHeight, _layerMask))
        {
            footIKPosition = hitInfo.point;
            footIKPosition.y += _heelHeight;

            Quaternion footIKRotation;

            footIKRotation = Quaternion.FromToRotation(footUp, hitInfo.normal) * CalcutateFootRotation(knee);
            _animator.SetIKPosition(goal, footIKPosition);
            _animator.SetIKRotation(goal, footIKRotation);
        }
        else
        {
            footIKPosition = _animator.GetBoneTransform(foot).position;
            footIKPosition.y = this.transform.position.y - _kneeHeight;
            _animator.SetIKPosition(goal, footIKPosition);
        }
    }

    private void CalculateHipHeight()
    {
        if (_leftFootIKPosition == Vector3.zero || _rightFootIKPosition == Vector3.zero || _lastHipPositionY == 0)
        {
            _lastHipPositionY = _animator.bodyPosition.y;
            return;
        }

        float Offset;
        if (_animator.GetFloat(LeftFootIKWeight) == 1 && _animator.GetFloat(RightFootIKWeight) == 1)
        {
            Offset = Mathf.Abs(_leftFootIKPosition.y - _rightFootIKPosition.y) * 0.5f;
        }
        else
        {
            Offset = 0;
        }

        Vector3 bodyPosition = _animator.bodyPosition;
        switch (_player.CurrentMode)
        {
            case PlayerController.MoveMode.Default:
                bodyPosition.y = _player.transform.position.y + _defaultBodyPositionY;
                break;
            case PlayerController.MoveMode.Aim:
                bodyPosition.y = _player.transform.position.y + _aimBodyPositionY;
                break;
        }

        Vector3 newHipPosition = bodyPosition + Vector3.down * Offset;
        newHipPosition.y = Mathf.Lerp(_lastHipPositionY, newHipPosition.y, HipUpDownSpeed * Time.fixedDeltaTime);

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
