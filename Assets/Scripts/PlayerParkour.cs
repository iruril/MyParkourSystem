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

    private Animator _anim;

    public float ParkourMoveSpeed= 5.0f;
    public float ParkourJumpTime= 0.3f;
    public float ParkourClimbTime = 0.4f;
    public float ParkourVaultTime= 1.0f;
    public float ParkourJumpClimbTime = 1.3f;
    public float StepHeight { get; set; } = 0.0f;
    public Vector3 StepPoint { get; set; } = Vector3.zero;
    public Vector3 LeftStepPoint { get; set; } = Vector3.zero;
    public Vector3 RightStepPoint { get; set; } = Vector3.zero;

    public enum JumpState
    {
        None,
        DefaultJump,
        Vault,
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
            Gizmos.DrawWireSphere(StepPoint, 0.1f);
        }
    }

    private float RayCasting(Transform ray, float range) //Return Distance between rayPoint to HitPoint. 
    {
        RaycastHit hit;
        if (Physics.Raycast(ray.position, this.transform.forward, out hit, range, _layerMask))
        {
            if(Vector3.Angle(hit.normal, -transform.forward) >= 45f)
            {
                return 0;
            }
            float distance = hit.distance * 100;
            distance = Mathf.Floor(distance);
            return distance / 100;
        }
        return 0;
    }

    private JumpState ShouldClimbAfterJumpOver(Transform ray)// Check if there's sufficient space for 'Vaulting-Run'
    {
        float step = RayCasting(ray, _jumpOverLimit);
        if (step < _jumpOverLimit && step != 0)
        {
            return JumpState.JumpClimb; // if it's not, returns JumpState.JumpClimb
        }
        return JumpState.Vault; // if it is, returns JumpState.Vault
    }

    public JumpState CheckRay() //Shoot's ray, and return current JumpMode
    {
        //Create Dictionary, and Save Data(RayName, RayHitDistance) in Dictionary
        Dictionary<JumpState, float> temp = new();
        float dist = 0;
        dist = RayCasting(_jumpBottomRay, _rayDistance);
        if (dist != 0) temp.Add(JumpState.DefaultJump, dist);

        dist = RayCasting(_jumpMiddleRay, _rayDistance);
        if (dist != 0) temp.Add(JumpState.Vault, dist);

        dist = RayCasting(_jumpTopRay, _rayDistance);
        if (dist != 0) temp.Add(JumpState.JumpClimb, dist);

        dist = RayCasting(_MaxHeightRay, _rayDistance);
        if (dist != 0) temp.Add(JumpState.Climb, dist);

        //if there's data in Dictionary, get HashKey(JumpState) which has 'Closeast Distance' Data
        if (temp.Count > 0)
        {
            JumpMode = temp.Aggregate((x, y) => x.Value < y.Value ? x : y).Key; //Get HashKey(JumpState) which has 'Closeast Distance' Data
            CalculateStepHeight(temp[JumpMode]); //Calculates StepHight, and StepPosition
            temp.Remove(JumpMode);
        }
        else // if there's no, then just set to JumpState.DefaultJump
        {
            JumpMode = JumpState.DefaultJump;
        }

        //When JumpMode is JumpState.Vault, check if there's sufficient space for 'Vaulting-Run'
        if (JumpMode == JumpState.Vault)
        {
            JumpMode = ShouldClimbAfterJumpOver(_jumpTopRay);
        }
        return JumpMode;
    }

    private void CalculateStepHeight(float rayDist) //Calculates StepHight, and StepPosition
    {
        RaycastHit hit;
        if (Physics.Raycast(_MaxHeightRay.position + _MaxHeightRay.forward * (rayDist + 0.01f),
            Vector3.down, out hit, 7.5f, _layerMask))
        {
            StepPoint = hit.point;
            StepHeight = StepPoint.y - this.transform.position.y;
            RightStepPoint = StepPoint + this.transform.right * 0.25f;
            LeftStepPoint = StepPoint - this.transform.right * 0.25f;
        }
    }
}
