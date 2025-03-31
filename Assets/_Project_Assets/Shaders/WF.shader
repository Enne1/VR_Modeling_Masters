Shader "Custom/WireframeUnlit_URP"
{
    Properties
    {
        _WireframeColor ("Wireframe Color", Color) = (1,0,0,1) // Wireframe color (Red in this case)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Name "WIRE"
            Tags { "LightMode"="UniversalForward" } // Use Universal Forward rendering mode
            Cull Front // Make sure the edges of the mesh are rendered properly

            HLSLPROGRAM
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Declare input and output structures
            struct Attributes
            {
                float4 vertex : POSITION;
            };

            struct Varyings
            {
                float4 position : POSITION;
            };

            // Declare the wireframe color property
            float4 _WireframeColor;

            // Vertex shader: Transform the vertex position to clip space
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.position = TransformObjectToHClip(v.vertex); // Transform to clip space
                return o;
            }

            // Fragment shader: Return the wireframe color
            half4 frag(Varyings i) : SV_Target
            {
                return _WireframeColor; // Return the wireframe color (Red)
            }

            ENDHLSL
        }
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass {
            // Fallback pass for older pipeline support (if needed)
        }
    }

    Fallback "Universal Forward"
}

