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
