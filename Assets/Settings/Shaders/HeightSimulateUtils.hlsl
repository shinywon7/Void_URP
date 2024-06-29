#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl"
static const float Sqrt3 = 1.73205080757;
float  _MaxOffset;
float  _Damping;
float  _Precalculation;
float  _SpeedTweak;
float  _SampleSize;
float  _Resolution;
float  _VoidSize;
int    _Substeps;
float  _VerticalPushScale;
float  _HorizontalPushScale;
float  _VoidAge;
float  _HalfVoidWidth;
float  _WindPower;

float2 Unity_Voronoi_RandomVector_Deterministic_float (float2 UV, float offset)
{
    Hash_Tchou_2_2_float(UV, UV);
    return float2(sin(UV.y * offset), cos(UV.x * offset)) * 0.5 + 0.5;
}

float Unity_Voronoi_Deterministic_float(float2 UV, float AngleOffset, float CellDensity)
{
    float Out = 0;
    float2 g = floor(UV * CellDensity);
    float2 f = frac(UV * CellDensity);
    float t = 8.0;
    float3 res = float3(8.0, 0.0, 0.0);
    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 lattice = float2(x, y);
            float2 offset = Unity_Voronoi_RandomVector_Deterministic_float(lattice + g, AngleOffset);
            float d = distance(lattice + offset, f);
            if (d < res.x)
            {
                res = float3(d, offset.x, offset.y);
                Out = res.x;
            }
        }
    }
    return Out;
}