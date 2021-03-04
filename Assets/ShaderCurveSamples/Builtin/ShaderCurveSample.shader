Shader "Custom/ShaderCurveSample"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        
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
            // make fog work
            #pragma multi_compile_fog

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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _CurveTex;
            float4 _CurveTex_TexelSize;
            float _CurveResolution;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);

                float time = frac(_Time.y * 0.5) * 2 - 1.0;
                float t = 1.0 - pow(abs(time), 1.0);

                t = saturate(t);
                

                float r = ShaderCurve_Evaluate(_CurveTex, _CurveTex_TexelSize, _CurveResolution, 0, t);

                r *= 0.4;

                float d = distance(float2(0.5, 0.5), i.uv);

                float c = step(d, r);

                col = fixed4(c, c, c, 1.0);
                
                return col;
            }
            ENDCG
        }
    }
}
