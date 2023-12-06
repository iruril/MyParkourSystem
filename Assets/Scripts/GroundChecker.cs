using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    [SerializeField] private float _detectionMaxDist;
    public LayerMask GroundLayer;

    public bool IsGround { get; private set; } = false;

    [SerializeField] private bool drawGizmo;
    [SerializeField] private Vector3 boxSize = new Vector3(0.5f, 0.3f, 0.5f);

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;

        Gizmos.color = Color.green;
        Gizmos.DrawCube(transform.position + transform.up - transform.up * _detectionMaxDist, boxSize);
    }

    public bool IsGrounded()
    {
        return IsGround = (Physics.BoxCast(transform.position + transform.up, boxSize, -transform.up, transform.rotation, _detectionMaxDist, GroundLayer));
    }
}
