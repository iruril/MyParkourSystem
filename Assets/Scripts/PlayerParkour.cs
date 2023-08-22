using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerParkour : MonoBehaviour
{
    [SerializeField] private Transform _jumpBottomRay;
    [SerializeField] private Transform _jumpMiddleRay;
    [SerializeField] private Transform _jumpTopRay;
    [SerializeField] private Transform _MaxHeightRay;

    [SerializeField] private Vector3 _boxScale = new Vector3(0.6f, 0.2f, 0.1f);
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private float _rayDistance = 1.5f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_jumpBottomRay.position + transform.forward * _rayDistance, _boxScale.x / 2);
        Gizmos.DrawWireSphere(_jumpMiddleRay.position + transform.forward * _rayDistance, _boxScale.x / 2);
        Gizmos.DrawWireSphere(_jumpTopRay.position + transform.forward * _rayDistance, _boxScale.x / 2);
        Gizmos.DrawWireSphere(_MaxHeightRay.position + transform.forward * _rayDistance, _boxScale.x / 2);
    }

    void FixedUpdate()
    {
        Debug.Log(CheckRay());
    }

    private int CheckRay()
    {
        int rayCount = 0;

        if(Physics.BoxCast(_jumpBottomRay.position, _boxScale, this.transform.forward, this.transform.rotation, _rayDistance, _layerMask))
        {
            rayCount++;
        }
        if (Physics.BoxCast(_jumpMiddleRay.position, _boxScale, this.transform.forward, this.transform.rotation, _rayDistance, _layerMask))
        {
            rayCount++;
        }
        if (Physics.BoxCast(_jumpTopRay.position, _boxScale, this.transform.forward, this.transform.rotation, _rayDistance, _layerMask))
        {
            rayCount++;
        }
        if (Physics.BoxCast(_MaxHeightRay.position, _boxScale, this.transform.forward, this.transform.rotation, _rayDistance, _layerMask))
        {
            rayCount++;
        }
        return rayCount;
    }
}
