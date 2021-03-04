using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShaderCurve
{
    [CreateAssetMenu(menuName = "ShaderCurve Asset", fileName = "ShaderCurveAsset")]
    public class ShaderCurveAsset : ScriptableObject
    {
        [SerializeField] private AnimationCurve[] curves = default;
        [SerializeField, HideInInspector] private Texture2D bakedTexture = default;
        [SerializeField] private BakeSettings bakeSettings = default;
        
        public BakeSettings BakeSettings => bakeSettings;
    }
}