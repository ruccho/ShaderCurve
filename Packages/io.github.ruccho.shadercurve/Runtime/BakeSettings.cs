using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShaderCurve
{
    [Serializable]
    public class BakeSettings
    {
        [SerializeField] public CurveTextureFormat format = default;
        [SerializeField] public int resolution = 1024;
    }

    public enum CurveTextureFormat
    {
        ARGB32,
        RGBAFloat
    }
}