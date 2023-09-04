using UnityEngine;

public class JiggleBonePhysics : MonoBehaviour
{
    Transform parentTransform;
    Rigidbody boneRigidbody;
    Vector3 prevFrameParentPosition = Vector3.zero;
    public float power = 0f;
    public float clampDist = 0.03f;

    void Start()
    {
        parentTransform = transform.parent;
        prevFrameParentPosition = parentTransform.position;

        boneRigidbody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 delta = (prevFrameParentPosition - parentTransform.position);
        boneRigidbody.AddForce(Vector3.ClampMagnitude(delta, clampDist) * power);

        prevFrameParentPosition = parentTransform.position;
    }
}
