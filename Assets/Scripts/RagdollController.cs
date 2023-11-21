using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    public GameObject MySkeleton;
    private List<Rigidbody> _myRigids = new();
    private Rigidbody _myHead;

    void Start()
    {
        Rigidbody[] tempRigids = MySkeleton.GetComponentsInChildren<Rigidbody>();
        foreach(Rigidbody item in tempRigids)
        {
            if(item.transform.name != "SMG")
            {
                _myRigids.Add(item);
            }
            if (item.transform.name.Contains("Head") || item.transform.name.Contains("head"))
            {
                _myHead = item;
            }
        }
        SetMyRagdollState(true);
        SetMyRagdollCollisionState(false);
    }

    public void SetMyRagdollState(bool kinematicState)
    {
        foreach (Rigidbody rigid in _myRigids)
        {
            rigid.isKinematic = kinematicState;
        }
    }

    public void SetMyRagdollCollisionState(bool collisionRecieveState)
    {
        foreach (Rigidbody rigid in _myRigids)
        {
            rigid.transform.GetComponent<Collider>().enabled = collisionRecieveState;
        }
    }

    public void DeadEffect(Vector3 forceDir)
    {
        _myHead.AddForce(forceDir * 30.0f, ForceMode.Impulse);
    }
}
