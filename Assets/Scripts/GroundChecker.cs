using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    private CharacterController _myPlayer;
    [SerializeField] private Vector3 _boxScale;
    [SerializeField] private float _detectionMaxDist;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private bool drawGizmo;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 0.3f,0f);

    private void Awake()
    {
        _myPlayer = this.GetComponent<CharacterController>();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;

        Gizmos.color = Color.green;
        Gizmos.DrawCube((transform.position + _offset) - transform.up * _detectionMaxDist, _boxScale);
    }

    public bool IsGrounded()
    {
        if(_myPlayer.isGrounded) return true;
        return Physics.BoxCast(transform.position + _offset, _boxScale, -transform.up, transform.rotation,
            _detectionMaxDist, groundLayer);
    }
}
