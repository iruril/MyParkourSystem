using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    [SerializeField] private Vector3 _boxScale;
    [SerializeField] private float _detectionMaxDist;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private bool drawGizmo;

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;

        Gizmos.color = Color.red;
        Gizmos.DrawCube(transform.position, _boxScale);
    }

    public bool IsGrounded()
    {
        return Physics.BoxCast(transform.position, _boxScale, -transform.up, transform.rotation,
            _detectionMaxDist, groundLayer);
    }
}
