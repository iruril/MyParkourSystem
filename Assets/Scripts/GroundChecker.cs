using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    private CharacterController _myPlayer;
    private PlayerController _playerControl;
    [SerializeField] private float _detectionMaxDist;
    public LayerMask GroundLayer;

    [SerializeField] private bool drawGizmo;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 0.3f,0f);

    private void Awake()
    {
        _myPlayer = this.GetComponent<CharacterController>();
        _playerControl = this.GetComponent<PlayerController>();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmo) return;

        Gizmos.color = Color.green;
        Gizmos.DrawRay((transform.position + _offset), Vector3.down * _detectionMaxDist);
    }

    public bool IsGrounded()
    {
        if (_myPlayer.isGrounded) return true;
        else
        {
            RaycastHit hit;
            if(Physics.Raycast(transform.position + _offset, Vector3.down, out hit, _detectionMaxDist, GroundLayer))
            {
                return true;
            }
            return false;
        }
    }
}
