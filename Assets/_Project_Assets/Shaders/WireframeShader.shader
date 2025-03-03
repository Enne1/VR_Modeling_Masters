Shader "Custom/Wireframe"
{
    Properties
    {
        _Color ("Color", Color) = (0,1,0,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            ZWrite On
            ColorMask RGB
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #include "UnityCG.cginc"

            struct v2g
            {
                float4 pos : SV_POSITION;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float color : COLOR;
            };

            v2g vert(appdata_base v)
            {
                v2g o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g input[3], inout TriangleStream<g2f> output)
            {
                for (int i = 0; i < 3; i++)
                {
                    g2f o;
                    o.pos = input[i].pos;
                    o.color = 1.0;
                    output.Append(o);
                }
            }

            fixed4 frag(g2f i) : SV_Target
            {
                return fixed4(0, 1, 0, 1); // Green color
            }

            ENDCG
        }
    }
}
