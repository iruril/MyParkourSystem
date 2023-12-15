using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIKControlller : MonoBehaviour
{
    public float DistanceGround = 0.5f;
    public float KneeHeight = 0.5f;
    private Animator _animator;
    [SerializeField] private GroundChecker _myGroundChecker;
    [SerializeField] private PlayerController _player;
    [SerializeField] private PlayerParkour _playerParkour;
    private LayerMask _layerMask;

    private float _animIKWeight = 0;
    private float _animIKWeightLerpTime = 0.35f;
    private bool _ikWeightSet = false;

    private float _hipHeight = 0;

    private Vector3 _leftFootIKPosition = Vector3.zero;
    private Vector3 _rightFootIKPosition = Vector3.zero;

    private const string LeftFootIKWeight = "LeftFootIKWeight";
    private const string RightFootIKWeight = "RightFootIKWeight";

    private void Awake()
    {
        _animator = this.GetComponent<Animator>();
        _layerMask = _myGroundChecker.GroundLayer;
        _hipHeight = _animator.GetBoneTransform(HumanBodyBones.Hips).position.y;
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
        CalculateIKState(AvatarIKGoal.LeftFoot, ref _leftFootIKPosition);
        CalculateIKState(AvatarIKGoal.RightFoot, ref _rightFootIKPosition);
        CalculateHipHeight();
    }

    private void CalculateIKState(AvatarIKGoal goal, ref Vector3 IKPosition)
    {
        if (goal == AvatarIKGoal.LeftFoot)
        {
            _animator.SetIKPositionWeight(goal, _animator.GetFloat(LeftFootIKWeight));
        }
        else
        {
            _animator.SetIKPositionWeight(goal, _animator.GetFloat(RightFootIKWeight));
        }
        _animator.SetIKRotationWeight(goal, 1);

        Ray leftRay = new Ray(_animator.GetIKPosition(goal) + Vector3.up * KneeHeight, Vector3.down);
        Debug.DrawRay(_animator.GetIKPosition(goal) + Vector3.up * KneeHeight, Vector3.down * (KneeHeight + DistanceGround), Color.red);
        if (Physics.Raycast(leftRay, out RaycastHit leftHitinfo, DistanceGround + KneeHeight, _layerMask))
        {
            if (leftHitinfo.transform.tag == "Walkable")
            {
                IKPosition = leftHitinfo.point;
                IKPosition.y += DistanceGround;

                Quaternion footIKRotation = Quaternion.FromToRotation(this.transform.up, leftHitinfo.normal) * this.transform.rotation;
                _animator.SetIKPosition(goal, IKPosition);
                _animator.SetIKRotation(goal, footIKRotation);
            }
        }
        else
        {
            IKPosition = Vector3.zero;
        }
    }

    private void CalculateHipHeight()
    {
        if (_leftFootIKPosition == Vector3.zero || _rightFootIKPosition == Vector3.zero)
        {
            this.transform.position = new Vector3(transform.position.x, _animator.bodyPosition.y, transform.position.z);
            return;
        }
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
}
