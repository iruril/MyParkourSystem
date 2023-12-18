using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    [SerializeField] private float _detectionMaxDist;
    public LayerMask GroundLayer;

    public bool IsGround { get; private set; } = false;
    public bool IsSnapGround { get; private set; } = false;

    private float _stepMinDepth = 0.3f;
    private float _stepMaxHeight = 0.3f;
    private const float _stepHeightErrorRange = 0.2f;

    private Vector3 _rayOrigin;
    private Vector3 _rayEndPos;

    [SerializeField] private bool drawGizmo;
    [SerializeField] private Vector3 boxSize = new Vector3(0.5f, 0.3f, 0.5f);

    private void Start()
    {
        CharacterController characterController = GetComponent<CharacterController>();
        _stepMaxHeight = characterController.stepOffset + _stepHeightErrorRange;
        _stepMinDepth = characterController.stepOffset;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;

        Gizmos.color = Color.green;
        Gizmos.DrawCube(transform.position + transform.up - transform.up * _detectionMaxDist, boxSize);

        Gizmos.color = Color.red;

        _rayOrigin = transform.position + (transform.forward * _stepMinDepth) + (Vector3.up * _stepMaxHeight);
        _rayEndPos = _rayOrigin + Vector3.down * _stepMaxHeight * 2;
        Gizmos.DrawLine(_rayOrigin, _rayEndPos);
        if (Physics.Linecast(_rayOrigin, _rayEndPos, out RaycastHit hitInfo ,GroundLayer))
        {
            Gizmos.DrawWireSphere(hitInfo.point, 0.1f);
        }
    }

    private void Update()
    {
        IsSnapGrounded();
    }

    private void FixedUpdate()
    {
        IsGrounded();
    }

    private void IsGrounded()
    {
       IsGround = (Physics.BoxCast(transform.position + transform.up, boxSize, -transform.up, transform.rotation, _detectionMaxDist, GroundLayer));
    }

    private void IsSnapGrounded()
    {
        _rayOrigin = transform.position + (transform.forward * _stepMinDepth) + (Vector3.up * _stepMaxHeight);
        _rayEndPos = _rayOrigin + Vector3.down * _stepMaxHeight * 2;
        if (Physics.Linecast(_rayOrigin, _rayEndPos, GroundLayer))
        {
            IsSnapGround = true;
        }
        else
        {
            IsSnapGround = false;
        }
    }
}
