// https://developer.download.nvidia.com/cg/

Shader "Unlit/PlayerRingEffect"
{
    Properties  // input data
    {  
      // create a variable with a default value
      _ColorA ("Color A", Color) = (1, 1, 1, 1)
      _ColorB ("Color B", Color) = (1, 1, 1, 1)
      _Scale ("UV Scale", Float) = 1.0
      _offset ("UV Offset", Float) = 0.0
        
      _no_of_lines ("Number of vert. Lines", Int) = 8
      _no_of_swirls ("Number of Swirls in a Line", Int) = 6
      _attitude_of_swirls ("Attitude of Swirls", Float) = 0.01
        
      [Enum(Up, 0, Down, 1)]
      _FlowDirection ("Flow Direction", Float) = 1
    }
    SubShader
    {
        // subshader tags
        Tags { 
          "RenderType"="Transparent"  // tag for render pipeline (for post processing)
          "Queue"="Transparent"  // render order
        }
        Pass
        {
            // pass tags

            
            ZWrite Off  // deactivate depth buffer
            ZTest LEqual  // Always: draw always, LEqual: draw in front of, GEqual: draw behind of
            Blend One One  // additive

            //Blend DstColor Zero  // mutiply

            CGPROGRAM
            #pragma vertex vert  // specify that there is a vertex-shader called vert
            #pragma fragment frag  // specify that there is a fragment-shader called frag

            #include "UnityCG.cginc"  

            #define TAU 6.28318530718

            float4 _ColorA;
            float4 _ColorB;
            int _no_of_lines;
            int _no_of_swirls;
            float _attitude_of_swirls;
            float _FlowDirection;
            //float _Scale;
            //float _Offset;

            // automatically filled by unity
            struct meshData  // mesh data per vertex
            {
                float4 vertex : POSITION;  // vertex position
                float3 normals: NORMAL;  // vertex normal
                //float4 tangent: TANGENT;  // vertex tangent  // direction of the tangent (0-2), sign/mirrored? (3)
                //float4 color: COLOR;  // vertex color (rgba)
                float2 uv0 : TEXCOORD0;  // uv0 coordinates (e.g. normal map textures) [float4 also possible]
                //float2 uv1 : TEXCOORD1;  // uv1 coordinates (e.g lightmap coordinates) [float4 also possible]
            };

            // data that should be passed from the vertex to the fragment shader
            // you get the interpolated value for pixels from the vertices
            struct Interpolators
            {
                float4 vertex : SV_POSITION;  // clip space position (of this vertex)
                float3 normal : TEXCOORD0;  // here TEXCOORD0 is just a "placeholder" for separating multiple variables
                float2 uv : TEXCOORD1;
            };

            Interpolators vert (meshData v)
            {
                Interpolators o;
                o.vertex = UnityObjectToClipPos(v.vertex);  // multiplying by the mvp-(model view projection)-matrix (local space to clip space)

                o.normal = UnityObjectToWorldNormal(v.normals);  // object to world coordinates (normal mode) -> mul(float(3x3)unity_ObjectToWorld, v.normals);
                o.uv = v.uv0;
                
                return o;
            }

            // bool [0 or 1]
            // int
            // float (32 bit float)
            // half (16 bit float)
            // fixed (lower precision) [-1 to 1]
            // vector = datatype4
            // matrix = datatype4x4

            // .xy, .yx .rg, .abgr ... are possible to use for parameters (swizzling?)

            // returns color for pixels
            float4 frag (Interpolators i) : SV_Target  // output to the frame buffer
            {
                // use UnityObjectToWorldNormal() here, when the object has more vertices than pixels

                //return float4(i.normal, 1);  

                // function frac(value) -> returns: value - floor(value)
                // saturate(value) -> clamp between 0 and 1

                // Time component: _Time.xyzw (y = seconds, w = seconds / 20)

                float offset = cos(i.uv.x * TAU * _no_of_swirls) * _attitude_of_swirls;
                
                float direction = _FlowDirection * 2.0 - 1.0;
                float time = direction * _Time.y * 0.1;

                float t = cos((i.uv.y + offset + time) * TAU * _no_of_lines) * 0.5 + 0.5;  // TAU makes sure that a whole period is gone through
                t *= (1 - i.uv.y);

                float topBottomRemover = (abs(i.normal.y) < 0.999);  // remove top and bottom (on basis of the y-normals)

                float waves = t * topBottomRemover;

                float4 gradient = lerp(_ColorA, _ColorB, i.uv.y);
                float alpha = lerp(_ColorA.a, _ColorB.a, i.uv.y);

                float4 output = gradient * waves;
                output.a = alpha;
                return output; 

                // blend between two colors (based on x-uv-coordinate)
                //float4 outputColor = lerp(_ColorA, _ColorB, i.uv.x);
                //return outputColor;  
            }

            ENDCG
        }
    }
}
