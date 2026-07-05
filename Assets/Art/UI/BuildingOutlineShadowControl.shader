Shader "Custom/BuildingOutline_CustomShadow"
{
    Properties
    {
        //===== 基础材质 =====
        _MainTex ("主纹理", 2D) = "white" {}
        _Color ("固有色", Color) = (1,1,1,1)
        _Gloss ("高光光滑度", Range(0,1)) = 0.2

        //===== 外描边设置（已修复ZOffset报错） =====
        _OutlineWidth ("描边粗细", Range(0, 0.15)) = 0.02
        _OutlineColor ("描边颜色", Color) = (0.1,0.1,0.1,1)
        _OutlineDepthOffset ("描边防穿插偏移", Range(0,0.01)) = 0.001

        //===== 自定义阴影控制（调影子颜色在这里） =====
        _ShadowTint ("阴影染色(影子颜色)", Color) = (0.25,0.28,0.32,1)
        _ShadowBrightness ("阴影明暗深浅", Range(0,1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue"="Geometry"
            "LightMode"="ForwardBase"
        }

        // Pass1：外描边（增加雾支持）
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            // 开启雾编译宏
            #pragma multi_compile_fog

            float _OutlineWidth;
            float _OutlineDepthOffset;
            fixed4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(1) // 雾插值坐标
            };

            v2f vert (appdata v)
            {
                v2f o;
                float3 normalVS = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                float4 posVS = mul(UNITY_MATRIX_MV, v.vertex);
                // 宽度+偏移合并替代ZOffset指令，无编译报错
                posVS.xyz += normalVS * (_OutlineWidth + _OutlineDepthOffset);
                o.pos = mul(UNITY_MATRIX_P, posVS);
                UNITY_TRANSFER_FOG(o, o.pos); // 传递雾因子
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = _OutlineColor;
                UNITY_APPLY_FOG(i.fogCoord, col); // 混合雾
                return col;
            }
            ENDCG
        }

        // Pass2：主体渲染 + 自定义阴影颜色 + 雾效兼容
        Pass
        {
            Name "BASE"
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_fog // 雾关键宏
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Gloss;

            // 阴影参数声明
            fixed4 _ShadowTint;
            float _ShadowBrightness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                SHADOW_COORDS(3)
                UNITY_FOG_COORDS(4) // 雾插值通道，避开shadow占用的3号
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos); // 计算并传递雾系数
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 基础贴图固有色
                fixed3 albedo = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
                float3 worldN = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(worldN, lightDir));

                // 采样阴影衰减值 shadowAtten = 1=受光，0=阴影区域
                float shadowAtten = SHADOW_ATTENUATION(i);

                // 受光亮部颜色（正常灯光照射效果）
                fixed3 litColor = albedo * _LightColor0.rgb * NdotL;
                // 阴影颜色公式：贴图底色 × 自定义染色 × 阴影亮度
                fixed3 shadowColor = albedo * _ShadowTint.rgb * _ShadowBrightness;
                // 在亮部与自定义阴影之间平滑插值
                fixed3 diffuseFinal = lerp(shadowColor, litColor, shadowAtten);

                // 环境漫反射补光
                diffuseFinal += albedo * ShadeSH9(float4(worldN,1)) * 0.25;

                // 高光，阴影区域自动弱化高光更真实
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 halfDir = normalize(lightDir + viewDir);
                float specPow = lerp(2, 128, _Gloss);
                float spec = pow(saturate(dot(worldN, halfDir)), specPow);
                diffuseFinal += _LightColor0.rgb * spec * _Gloss * shadowAtten;

                fixed4 finalCol = fixed4(diffuseFinal, 1);
                UNITY_APPLY_FOG(i.fogCoord, finalCol); // 雾混合
                return finalCol;
            }
            ENDCG
        }

        // 阴影投射通道
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    FallBack "Diffuse"
    CustomEditor "UnityEditor.ShaderGUI"
}