using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    public GameObject MySkeleton;
    private Rigidbody[] _myRigids;

    void Start()
    {
        _myRigids = MySkeleton.GetComponentsInChildren<Rigidbody>();
        SetMyRagdollState(true);
        SetMyRagdollCollisionState(false);
    }

    public void SetMyRagdollState(bool state)
    {
        foreach (Rigidbody rigid in _myRigids)
        {
            rigid.isKinematic = state;
        }
    }

    public void SetMyRagdollCollisionState(bool state)
    {
        foreach (Rigidbody rigid in _myRigids)
        {
            rigid.transform.GetComponent<Collider>().enabled = state;
        }
    }
}
