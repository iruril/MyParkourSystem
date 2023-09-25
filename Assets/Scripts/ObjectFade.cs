using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectFade : MonoBehaviour
{
    public enum SurfaceType
    {
        Opaque,
        Transparent
    }

    public enum BlendMode
    {
        Alpha,
        Premultiply,
        Additive,
        Multiply
    }

    private MeshRenderer _myRenderer = null;
    private Material[] _myMaterials;
    private List<Coroutine> _fadeOutSequence = new();

    void Start()
    {
        _myRenderer = this.GetComponent<MeshRenderer>();
        _myMaterials = _myRenderer.materials;
    }

    public void FadeOut()
    {
        StopAllFadeCoroutines();

        for(int i = 0; i < _myMaterials.Length; i++)
        {
            _fadeOutSequence.Add(StartCoroutine(DoFadeOut(i)));
        }
    }

    public void FadeIn()
    {
        StopAllFadeCoroutines();

        for (int i = 0; i < _myMaterials.Length; i++)
        {
            _fadeOutSequence.Add(StartCoroutine(DoFadeIn(i)));
        }
    }

    private void StopAllFadeCoroutines()
    {
        foreach (Coroutine coroutine in _fadeOutSequence)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        _fadeOutSequence.Clear();
    }

    private IEnumerator DoFadeOut(int materialIndex)
    {
        if (_myRenderer == null) yield break;

        Material material = _myMaterials[materialIndex];
        material.SetFloat("_Surface", 1.0f);
        Color baseColor = material.GetColor("_BaseColor");
        float materialAlpha = baseColor.a;

        while(materialAlpha > 0.3f)
        {
            materialAlpha -= Time.deltaTime;
            baseColor.a = materialAlpha;
            material.SetColor("_BaseColor", baseColor);
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    private IEnumerator DoFadeIn(int materialIndex)
    {
        if (_myRenderer == null) yield break;

        Material material = _myMaterials[materialIndex];
        SetupMaterialBlendMode(material);
        Color baseColor = material.GetColor("_BaseColor");
        float materialAlpha = baseColor.a;

        while (materialAlpha <= 1.0f)
        {
            materialAlpha += Time.deltaTime;
            baseColor.a = materialAlpha;
            material.SetColor("_BaseColor", baseColor);
            yield return new WaitForSeconds(Time.deltaTime);
        }
        material.SetFloat("_Surface", 0f);
        material.SetInt("_ZWrite", 1);
    }

    void SetupMaterialBlendMode(Material material)
    {
        if (material == null)
            throw new ArgumentNullException("material");

        if (!material.HasProperty("_AlphaClip"))
            throw new ArgumentException("Material does not have '_AlphaClip' property.");

        bool alphaClip = material.GetFloat("_AlphaClip") == 1;

        if (alphaClip)
            material.EnableKeyword("_ALPHATEST_ON");
        else
            material.DisableKeyword("_ALPHATEST_ON");

        SurfaceType surfaceType = (SurfaceType)material.GetFloat("_Surface");

        if (!material.HasProperty("_Surface"))
            throw new ArgumentException("Material does not have '_Surface' property.");

        if (surfaceType == 0)
        {
            material.SetOverrideTag("RenderType", "");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
            material.SetShaderPassEnabled("ShadowCaster", true);
        }
        else
        {
            if (!material.HasProperty("_Blend"))
                throw new ArgumentException("Material does not have '_Blend' property.");

            BlendMode blendMode = (BlendMode)material.GetFloat("_Blend");

            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            //material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);
        }
    }
}
