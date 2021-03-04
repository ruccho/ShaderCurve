# ShaderCurve

An utility for Unity to bake AnimationCurve into texture and evaluate them in shaders.

## Requirements

- Unity 2019.4+

## Installation

Add UPM git dependencies to your project.
| Package     | Git URL                                                                                           | Description                                           |
|-------------|---------------------------------------------------------------------------------------------------|-------------------------------------------------------|
| Core        | https://github.com/ruccho/ShaderCurve.git?path=/Packages/io.github.ruccho.shadercurve             | Core package of ShaderCurve.                          |
| ShaderGraph | https://github.com/ruccho/ShaderCurve.git?path=/Packages/io.github.ruccho.shadercurve.shadergraph | Optional package to use ShaderCurve with ShaderGraph. |

## Bake AnimationCurve into texture

### 1. Create ShaderCurve Asset
 - Right click on Project view, select `ShaderCurveAsset`.

### 2. Configure
 - Curves
    - Each ShaderCurve asset can contain multiple `AnimationCurve`.
 - Texture Format
    - Default texture format `ARGB32` only supports curves whose range fits in [0 .. 1]. Select `RGBAFloat` to use out-of-range values.
 - Resolution
    - The resolution in the time direction. Higher values make evaluations smooth. Default is `1024`.

### 3. Bake
- Click `Bake` to bake curves into a texture with specified settings.
- Baked textures will appear as children of each ShaderCurve asset.

## Use baked textures in shaders

### Cg/HLSL (Builtin Render Pipeline)

 - Include `"Packages/io.github.ruccho.shadercurve/Runtime/ShaderCurve.cginc"`
 - Declare baked texture and its `xxx_TexelSize` property (they are required to evaluation).
 - Use `ShaderCurve_Evaluate(curve, curve_ts, resolution, index, t)` to evaluate.
    - `curve` (sampler2D): Baked texture.
    - `curve_ts` (float4): TexelSize of `curve` texture.
    - `resolution` (uint): Use the value you specified in bake settings.
    - `index` (uint): Index of AnimationCurve in ShaderCurve asset.
    - `time` (float): Time to evaluate. [0 .. 1]

```c:ShaderCurveSample.shader
Shader "Custom/ShaderCurveSample"
{
    Properties
    {
        _CurveTex("ShaderCurve Texture", 2D) = "white" {}
        _CurveResolution("ShaderCurve Resolution", Float) = 1024
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Packages/io.github.ruccho.shadercurve/Runtime/ShaderCurve.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _CurveTex;
            float4 _CurveTex_TexelSize;
            float _CurveResolution;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

                float t = frac(_Time.y);

                float r = ShaderCurve_Evaluate(_CurveTex, _CurveTex_TexelSize, _CurveResolution, 0, t);

                fixed4 col = fixed4(r, r, r, 1.0);
                
                return col;
            }
            ENDCG
        }
    }
}

```

### ShaderGraph (UniversalRP / HighDefinitionRP)

 - `io.github.ruccho.shadercurve.shadergraph` package is required to use evaluation node.

 - Use `Evaluate ShaderCurve` node.
 ![image](https://user-images.githubusercontent.com/16096562/110014344-08a37b80-7d66-11eb-97a3-7fabd5242e7c.png)