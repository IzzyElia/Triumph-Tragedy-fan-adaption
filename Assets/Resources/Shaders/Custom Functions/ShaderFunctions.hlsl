#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED
void Overlay_half(float4 Background, float4 B, out float4 Out)
{
    if (B.r < 0.01 && B.g < 0.01 && B.b < 0.01)
    {
        Out = Background;
    }
    else
    {
        Out = B;
    }
}
void Overlay_float(float4 Background, float4 B, out float4 Out)
{
    if (B.r < 0.01 && B.g < 0.01 && B.b < 0.01)
    {
        Out = Background;
    }
    else
    {
        Out = B;
    }
}
void ThresholdCheck_half (float2 uv, float threshold, float4 A, float4 B, out float4 Out)
{
    if (uv.x >= 1-threshold || uv.x <= threshold 
    || uv.y >= 1-threshold || uv.y <= threshold)
    {
        Out = A;
    }
    else {
        Out = B;
    }
}
void ThresholdCheck_float (float2 uv, float threshold, float4 A, float4 B, out float4 Out)
{
    if (uv.x >= 1-threshold || uv.x <= threshold 
    || uv.y >= 1-threshold || uv.y <= threshold)
    {
        Out = A;
    }
    else {
        Out = B;
    }
}
void GradientCenter_half(float2 uv, float innerThreshold, float outerThreshold, out float Out)
{
    const float distance = length(uv - float2(0.5, 0.5));

    const float normalizedDistance = (outerThreshold - distance) / (outerThreshold - innerThreshold);
    
    Out = saturate(normalizedDistance);
}
#endif //MYHLSLINCLUDE INCLUDED