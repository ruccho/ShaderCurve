#ifndef SHADERCURVE_INCLIDED
#define SHADERCURVE_INCLUDED


//#define ShaderCurveEvaluate(tex, t, res, index)  ShaderCurve_Evaluate(tex, tex##_TexelSize, res, index)


float ShaderCurve_GetTextureValue(float4 color, uint channel)
{
    float value = 0;
    value = channel == 0 ? color.r : value;
    value = channel == 1 ? color.g : value; 
    value = channel == 2 ? color.b : value; 
    value = channel == 3 ? color.a : value; 

    return value;
}

float2 ShaderCurve_GetTexCoord(float4 curve_ts, uint pixelIndex)
{
    float x = floor(pixelIndex % curve_ts.z) * curve_ts.x;
    float y = floor(pixelIndex * curve_ts.x) * curve_ts.y;

    x += curve_ts.x * 0.5;
    y += curve_ts.y * 0.5;

    return float2(x, y);
}

float ShaderCurve_GetTextureValue(sampler2D curve, float4 curve_ts, uint pixelIndex, uint channel)
{
    float4 color = tex2D(curve, ShaderCurve_GetTexCoord(curve_ts, pixelIndex));

    return ShaderCurve_GetTextureValue(color, channel);
}

float ShaderCurve_Evaluate(sampler2D curve, float4 curve_ts, uint resolution, uint index, float t)
{
    uint pixelsPerCurve = resolution / 4;
    uint basePixelIndex = pixelsPerCurve * index;
    float sample = t * (resolution - 1);

    uint sample_A = floor(sample);
    uint sample_B = ceil(sample);

    uint pixelIndex_A = basePixelIndex + floor(sample_A / 4);
    uint pixelIndex_B = basePixelIndex + floor(sample_B / 4);
    uint channel_A = sample_A % 4.0;
    uint channel_B = sample_B % 4.0;

    float value_A = ShaderCurve_GetTextureValue(curve, curve_ts, pixelIndex_A, channel_A);
    float value_B = ShaderCurve_GetTextureValue(curve, curve_ts, pixelIndex_B, channel_B);

    float t_inner = frac(sample);

    float value = lerp(value_A, value_B, t_inner);

    return value;
}

#endif