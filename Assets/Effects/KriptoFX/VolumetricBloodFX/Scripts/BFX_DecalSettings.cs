
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

//[ExecuteAlways]
public class BFX_DecalSettings : MonoBehaviour
{
    public BFX_BloodSettings BloodSettings;
    public Transform parent;
    public float TimeHeightMax = 3.1f;
    public float TimeHeightMin = -0.1f;
    [Space]
    public Vector3 TimeScaleMax = Vector3.one;
    public Vector3 TimeScaleMin = Vector3.one;
    [Space]
    public Vector3 TimeOffsetMax = Vector3.zero;
    public Vector3 TimeOffsetMin = Vector3.zero;
    [Space]
    public AnimationCurve TimeByHeight = AnimationCurve.Linear(0, 0, 1, 1);

    private Vector3 startOffset;
    private Vector3 startScale;
    private float timeDelay;

    Transform t, tParent;
    BFX_ShaderProperies shaderProperies;

    Vector3 averageRay;
    bool isPositionInitialized;
    private Vector3 initializedPosition;
    private DecalProjector decal;

    private void Awake()
    {
        decal = GetComponent<DecalProjector>();
        startOffset = transform.localPosition;
        startScale = transform.localScale;
        t = transform;
        tParent = parent.transform;
        shaderProperies = GetComponent<BFX_ShaderProperies>();
        shaderProperies.OnAnimationFinished += ShaderCurve_OnAnimationFinished;
    }

    private void ShaderCurve_OnAnimationFinished()
    {
        decal.enabled = false;
    }

    private void Update()
    {
        if (!isPositionInitialized) InitializePosition();
        if (shaderProperies.enabled && initializedPosition.x < float.PositiveInfinity) transform.position = initializedPosition;
    }

    void InitializePosition()
    {

        decal.enabled = false;

        var currentHeight = parent.position.y;
        float ground = currentHeight;
        if (BloodSettings.AutomaticGroundHeightDetection)
        {
            var raycasts = Physics.RaycastAll(parent.position, Vector3.down, 5);
            foreach (var raycastHit in raycasts)
            {
                if (raycastHit.point.y < ground) ground = raycastHit.point.y;
            }
        }
        else
        {
            ground = BloodSettings.GroundHeight;
        }

        var currentScale = parent.localScale;
        var scaledTimeHeightMax = TimeHeightMax * currentScale.y;
        var scaledTimeHeightMin = TimeHeightMin * currentScale.y;

        if (currentHeight - ground >= scaledTimeHeightMax || currentHeight - ground <= scaledTimeHeightMin)
        {
            decal.enabled = false;
        }
        else
        {
            decal.enabled = true;
        }

        float diff = (tParent.position.y - ground) / scaledTimeHeightMax;
        diff = Mathf.Abs(diff);

        var scaleMul = Vector3.Lerp(TimeScaleMin, TimeScaleMax, diff);
        scaleMul.x *= currentScale.x;
        scaleMul.z *= currentScale.z;
        decal.size = new Vector3(scaleMul.x * startScale.x, scaleMul.z * startScale.z, startScale.y);

        var lastOffset = Vector3.Lerp(TimeOffsetMin, TimeOffsetMax, diff);
        t.localPosition = startOffset + lastOffset;
        t.position = new Vector3(t.position.x, ground + 0.05f, t.position.z);


        timeDelay = TimeByHeight.Evaluate(diff);

        shaderProperies.enabled = false;
        Invoke("EnableDecalAnimation", Mathf.Max(0, timeDelay / BloodSettings.AnimationSpeed));

        if (BloodSettings.DecalRenderingMode == BFX_BloodSettings.DecalRenderingModeEnum.DiagonalSurfaces)
        {
            t.localRotation = Quaternion.Euler(120, -90, 90);

            var decalSize = decal.size;
            decalSize.z = 5;
            decal.size = decalSize;
        }

        //if (BloodSettings.ClampDecalSideSurface) Shader.EnableKeyword("CLAMP_SIDE_SURFACE");

        isPositionInitialized = true;
    }

    private void OnDisable()
    {
        //if (BloodSettings.ClampDecalSideSurface) Shader.DisableKeyword("CLAMP_SIDE_SURFACE");
        isPositionInitialized = false;
        initializedPosition = Vector3.positiveInfinity;
    }

    Vector3 GetAverageRay(Vector3 start, Vector3 forward)
    {
        if (Physics.Raycast(start, -forward, out RaycastHit bulletRay))
        {
            return (bulletRay.normal + Vector3.up).normalized;
        }

        return Vector3.up;
    }

    void EnableDecalAnimation()
    {
        shaderProperies.enabled = true;
        initializedPosition = transform.position;
    }

    private void OnDrawGizmos()
    {
        if (t == null) t = transform;
        Gizmos.color = new Color(49 / 255.0f, 136 / 255.0f, 1, 0.03f);
        Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, t.lossyScale);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);

        Gizmos.color = new Color(49 / 255.0f, 136 / 255.0f, 1, 0.85f);
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);


    }
}
