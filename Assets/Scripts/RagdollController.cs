using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    public GameObject MySkeleton;
    [SerializeField] private List<Rigidbody> _myRigids = new();
    [SerializeField] private Rigidbody _myHead;

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
        SetMyRagdoll(kinematicState : true);
    }

    public void SetMyRagdoll(bool kinematicState)
    {
        foreach (Rigidbody rigid in _myRigids)
        {
            rigid.isKinematic = kinematicState;
            EntityHitBounds tempEntity = rigid.transform.gameObject.AddComponent<EntityHitBounds>();
            tempEntity.MyDamagableEntity = this.GetComponent<IDamageable>();
        }
    }

    public void SetMyRagdollKinemeticState(bool kinematicState)
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
