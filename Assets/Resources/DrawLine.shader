// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "UI/DrawLine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Width ("Width", Range(1, 100)) = 2
        _Softness ("Softness", Range(0, 1)) = 0.5

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _TexSize;
            float _Width;
            float _Softness;
            sampler2D _MainTex;
            
            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

                OUT.texcoord = v.texcoord;

                OUT.color = v.color * _Color;
                return OUT;
            }

            float2x2 rotate2D(float angle){
                return float2x2(
                    cos(angle), -sin(angle),
                    sin(angle), cos(angle));
            }

            float distFromLine(float2 p, float2 pLine1, float2 pLine2, float maxDist){
                float dist;
                float2 p1P2 = pLine2 - pLine1;
                float2 p1P2N = normalize(p1P2);
                float lineXDir = dot(p1P2N, float2(1, 0));
                float p1PDir = dot(normalize(p - pLine1), p1P2N);
                float p2PDir = dot(normalize(p - pLine2), p1P2N);

                if (p1PDir * lineXDir > 0 && p2PDir * lineXDir < 0 
                    || p1PDir * lineXDir < 0 && p2PDir * lineXDir > 0 
                    || lineXDir == 0 && p.y > pLine1.y && p.y < pLine2.y
                    || lineXDir == 0 && p.y < pLine1.y && p.y > pLine2.y
                ){
                    float2 p3 = pLine1 + dot(p1P2N, p - pLine1) * p1P2N;
                    return length(p3 - p);
                }
                return 1;
            }

            float points[200];
            float numPts;
            static float pts[] = {
                0.1, 0.5,
                0.9, 0.5,
                0.5, 0.1,
                0.5, 0.9
            };

            fixed4 frag(v2f IN) : SV_Target {
                float aspect = _TexSize.x / _TexSize.y; // width / height
                float width = _Width * aspect * 0.5/_TexSize.x;
                float aaWidth = (width * _Softness);

                float4 lineCol = _Color;
                float4 lineColA0 = _Color;
                lineColA0.a = 0;
                float4 result = lineColA0;

                float2 p0 = float2(IN.texcoord.x * aspect, IN.texcoord.y);

                for (int i = 0; i < 3; i++) {
                    float2 p1 = float2(pts[i*2] * aspect, pts[i*2 + 1]);
                    float2 p2 = float2(pts[(i+1)*2] * aspect, pts[(i+1)*2 + 1]);

                    float dist = distFromLine(p0, p1, p2, width);
                    if (dist < width+aaWidth) {
                        float a = smoothstep(width-aaWidth, width+aaWidth, dist);
                        float4 tmp = lerp(lineCol, lineColA0, a);
                        if (tmp.a > result.a) result = tmp;
                    }
                }

                return result;
            }
        ENDCG
        }
    }
}