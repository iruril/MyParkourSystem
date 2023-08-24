using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class PlayerParkour : MonoBehaviour
{
    [SerializeField] private Transform _jumpBottomRay;
    [SerializeField] private Transform _jumpMiddleRay;
    [SerializeField] private Transform _jumpTopRay;
    [SerializeField] private Transform _MaxHeightRay;

    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private float _rayDistance = 3.0f;
    [SerializeField] private float _jumpOverLimit = 5.0f;

    public float ParkourMoveSpeed= 5.0f;
    public float ParkourJumpTime= 0.3f;
    public float ParkourClimbTime = 0.4f;
    public float ParkourVaultTime= 1.0f;
    public float ParkourJumpClimbTime = 1.3f;
    public float StepHeight { get; set; } = 0.0f;
    public Vector3 StepPoint { get; set; }

    public enum JumpState
    {
        None,
        DefaultJump,
        JumpOver,
        JumpClimb,
        Climb
    }
    public JumpState JumpMode = JumpState.None;

    private void OnDrawGizmos()
    {
        Debug.DrawRay(_jumpBottomRay.position, this.transform.forward * _rayDistance, Color.cyan);
        Debug.DrawRay(_jumpMiddleRay.position, this.transform.forward * _rayDistance, Color.cyan);
        Debug.DrawRay(_jumpTopRay.position, this.transform.forward * _rayDistance, Color.cyan);
        Debug.DrawRay(_MaxHeightRay.position, this.transform.forward * _rayDistance, Color.cyan);

        if(StepPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(StepPoint, 0.1f);
        }
    }

    private float RayCasting(Transform ray, float range)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray.position, this.transform.forward, out hit, range, _layerMask))
        {
            float temp = hit.distance * 100;
            temp = Mathf.Floor(temp);
            return temp / 100;
        }
        return 0;
    }

    private JumpState ShouldClimbAfterJumpOver(Transform ray)
    {
        float step = RayCasting(ray, _jumpOverLimit);
        if (step < _jumpOverLimit && step != 0)
        {
            return JumpState.JumpClimb;
        }
        return JumpState.JumpOver;
    }

    public JumpState CheckRay()
    {
        Dictionary<JumpState, float> temp = new();
        float dist = 0;
        dist = RayCasting(_jumpBottomRay, _rayDistance);
        if (dist != 0) temp.Add(JumpState.DefaultJump, dist);

        dist = RayCasting(_jumpMiddleRay, _rayDistance);
        if (dist != 0) temp.Add(JumpState.JumpOver, dist);

        dist = RayCasting(_jumpTopRay, _rayDistance);
        if (dist != 0) temp.Add(JumpState.JumpClimb, dist);

        dist = RayCasting(_MaxHeightRay, _rayDistance);
        if (dist != 0) temp.Add(JumpState.Climb, dist);

        if (temp.Count > 0)
        {
            JumpMode = temp.Aggregate((x, y) => x.Value < y.Value ? x : y).Key;
            CalculateStepHeight(temp[JumpMode]);
            temp.Remove(JumpMode);
        }
        else
        {
            JumpMode = JumpState.DefaultJump;
        }

        if(JumpMode == JumpState.JumpOver)
        {
            JumpMode = ShouldClimbAfterJumpOver(_jumpTopRay);
        }
        return JumpMode;
    }

    private void CalculateStepHeight(float rayDist)
    {
        RaycastHit hit;
        if (Physics.Raycast(_MaxHeightRay.position + _MaxHeightRay.forward * (rayDist + 0.1f),
            Vector3.down, out hit, 7.5f, _layerMask))
        {
            StepPoint = hit.point;
            StepHeight = StepPoint.y - this.transform.position.y;
        }
    }
}
