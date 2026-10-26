// GameMaker's draw_set_blend_mode_ext(bm_inv_dest_color, bm_inv_src_color) for a sprite
// (obj_npc's draw while `inverted`, U.INC-T39): result = src * (1 - dst) + dst * (1 - src).
// The sprite's colour is premultiplied by its alpha first, so its transparent texels add nothing
// and leave what is behind them untouched. Unlit; drawn by SpriteRenderer (its _MainTex, vertex
// colour and flip), in the URP 2D renderer's unlit pass.
Shader "Incognito/InvertBlend"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        ZWrite Off
        Blend OneMinusDstColor OneMinusSrcColor

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                // SpriteRenderer.flipX/flipY are applied in the vertex shader under SRP, as
                // URP's own sprite shaders do.
                SetUpSpriteInstanceProperties();
                float3 positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output;
                output.positionCS = TransformObjectToHClip(positionOS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                return half4(texel.rgb * texel.a, texel.a);
            }
            ENDHLSL
        }
    }
}
