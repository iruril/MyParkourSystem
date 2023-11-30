using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class PlayerParkour : MonoBehaviour
{
    [SerializeField] private Transform _jumpBottomRay;
    [SerializeField] private Transform _jumpMiddleRay;
    [SerializeField] private Transform _jumpTopRay;
    [SerializeField] private Transform _maxHeightRay;

    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private float _rayDistance = 3.0f;
    [SerializeField] private float _vaultLimit = 4.0f;
    [SerializeField] private float _vaultMaxLength = 1.5f;

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
    public JumpState JumpMode { get; set; } = JumpState.None;

    public enum StepState
    {
        None,
        Lowest,
        Middle,
        Highest
    }
    public StepState StepMode { get; set; } = StepState.None;

    private void OnDrawGizmos()
    {
        Debug.DrawRay(_jumpBottomRay.position, this.transform.forward * _rayDistance, Color.cyan);
        Debug.DrawRay(_jumpMiddleRay.position, this.transform.forward * _rayDistance, Color.cyan);
        Debug.DrawRay(_jumpTopRay.position, this.transform.forward * _rayDistance, Color.cyan);
        Debug.DrawRay(_maxHeightRay.position, this.transform.forward * _rayDistance, Color.cyan);

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
            float surfaceAngle = Vector3.Angle(hit.normal, transform.up);
            if (surfaceAngle > 45f)
            {
                float distance = hit.distance * 100;
                distance = Mathf.Floor(distance);
                return distance / 100;
            }
        }
        return 0;
    }

    private JumpState ShouldClimbAfterJumpOver(Transform ray)// Check if there's sufficient space for 'Vaulting-Run'
    {
        float step = RayCasting(ray, _vaultLimit);
        if (step < _vaultLimit && step != 0)
        {
            return JumpState.JumpClimb; // if it's not, returns JumpState.JumpClimb
        }

        RaycastHit hit;
        Vector3 rayPos = new Vector3(StepPoint.x, _jumpTopRay.position.y, StepPoint.z) + transform.forward * _vaultMaxLength;
        if (Physics.Raycast(rayPos, Vector3.down, out hit, 100f, _layerMask))
        {
            if(hit.point.y >= StepPoint.y)
            {
                return JumpState.JumpClimb; // if it's not, returns JumpState.JumpClimb
            }
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

        dist = RayCasting(_maxHeightRay, _rayDistance);
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
        if (Physics.Raycast(_maxHeightRay.position + _maxHeightRay.forward * (rayDist + 0.01f),
            Vector3.down, out hit, 7.5f, _layerMask))
        {
            StepPoint = hit.point;
            StepHeight = StepPoint.y - this.transform.position.y;
            RightStepPoint = StepPoint + this.transform.right * 0.1f;
            LeftStepPoint = StepPoint - this.transform.right * 0.1f;
        }

        if(StepHeight <= 1.2f)
        {
            StepMode = StepState.Lowest;
        }
        else if(StepHeight is >1.2f and <= 2.0f)
        {
            StepMode = StepState.Middle;
        }
        else
        {
            StepMode = StepState.Highest;
        }
    }
}
