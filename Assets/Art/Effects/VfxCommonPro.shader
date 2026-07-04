Shader "XuanFu/Particles/VfxCommonPro"
{
    Properties
    {
       _MainTex("MainTex", 2D) = "white" {}
       [HDR]_MainColor("MainColor",color) = (1,1,1,1)
       _MainTexSpeed_x("MainTexSpeed_x", Float) = 0
       _MainTexSpeed_y("MainTexSpeed_y", Float) = 0
       
       // ===== 新增：极坐标效果 =====
       [Toggle(_USE_POLAR_UV)] _UsePolarUV("Use Polar UV", Float) = 0
       _PolarCenter("Polar Center (XY)", Vector) = (0.5, 0.5, 0, 0)
       
       [Toggle(_USE_DISTURBANCE)]_UseDist("UseDisturbance",int)=0
       _DisturbanceTex("DisturbanceTex", 2D) = "black" {}
       _DistSpeed_x("DistSpeed_x", Float) = 0
       _DistSpeed_y("DistSpeed_y", Float) = 0
       [Toggle(_USE_SECOND_DISTURBANCE)]_UseSecondDist("UseSecondDisturbance",int)=0
       _DisturbanceTex01("DisturbanceTex01", 2D) = "black" {}
       _DistSpeed01_x("DistSpeed_x", Float) = 0
       _DistSpeed01_y("DistSpeed_y", Float) = 0
       [Toggle(_USE_DISTURBANCE_MASK)]_UseDistMask("UseDisturbanceMask",int)=0
       _DistMask("DistMask", 2D) = "white" {}
       _Disturbance_Pow("Disturbance_Pow", Float) = 0
       [Toggle(_USE_MASK)]_UseMask("UseMask",int)=0
       _MaskTex("MaskTex", 2D) = "white" {}
       _MaskSpeed_x("MaskSpeed_x",Float) = 0.0
       _MaskSpeed_y("MaskSpeed_y",Float) = 0.0
       _Mask_Percentage("MaskPercentage", Range(-1 , 1)) = 0
       _MaskSoft("MaskSoft",Float) = 0
       [Toggle(_USE_DISSOlVE)]_UseDissolve("UseDissolve",int)=0
       _DissolveTex("DissolveTex", 2D) = "white" {}
       _DissolveSpeed_x("DissolveSpeed_x",float)=0
       _DissolveSpeed_y("DissolveSpeed_y",float)=0
       [Toggle(_USE_SECOND_DISSOlVE)]_UseSecondDissolve("UseSecondDissolve",int)=0
       _DissolveTex01("DissolveTex01", 2D) = "black" {}
       _DissolveSpeed01_x("DissolveSpeed01_x",float)=0
       _DissolveSpeed01_y("DissolveSpeed01_y",float)=0
       [Toggle(_USE_DISSOlVE_MASK)]_UseDissolveMask("UseDissolveMask",int)=0
       _DissolveMask("DissolveMask", 2D) = "white" {}
       _Dissolve_Soft("Dissolve_Soft", Float) = 0.0
       _DissEdgeRange("DissEdgeRange",Float) = 0.0
       _DissEdgeRangeSoft("DissEdgeRangeSoft",Float) = 0.0
       [HDR]_DissEdgeColor("DissEdgeColor",Color) = (0.5,0.5,0.5,1)

       [Enum(UnityEngine.Rendering.CullMode)]_CullMode("CullMode", Float) = 0
       [Toggle]_Zwrite("Zwrite", Float) = 0
       [Enum(UnityEngine.Rendering.CompareFunction)]_ZTest("ZTest", Float) = 4
       [Enum(UnityEngine.Rendering.BlendMode)]_Src("Src", Float) = 0
       [Enum(UnityEngine.Rendering.BlendMode)]_Dst("Dst", Float) = 0

       _DepthOffset("Depth Offset", Range(-1.0, 1.0)) = 0.0
       [Toggle(_FORCE_FRONT)] _ForceFront("Force Always Front", Float) = 0
       [Toggle(_USE_SOFT_PARTICLES)] _UseSoftParticles("Use Soft Particles", Float) = 0
       _SoftParticlesFadeDistance("Soft Particles Fade Distance", Float) = 1.0

       // ===== 原有：菲涅尔效果 =====
       [Toggle(_USE_FRESNEL)] _UseFresnel("Use Fresnel", Float) = 0
       [Toggle(_FRESNEL_MODEL_NORMAL)] _FresnelModelNormal("Use Model Normal", Float) = 0
       [HDR] _FresnelColor("Fresnel Color", Color) = (1, 0.5, 0.2, 1)
       _FresnelPower("Fresnel Power", Range(0.1, 10)) = 2.0
       _FresnelIntensity("Fresnel Intensity", Range(0, 5)) = 1.0
       [Toggle(_FRESNEL_GRADIENT)] _FresnelGradient("Use Gradient Texture", Float) = 0
       _GradientTex("Gradient Texture", 2D) = "white" {}
       _GradientSpeed("Gradient Speed XY", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
       Tags { "RenderType"="Transparent" "Queue"="Transparent" }
       LOD 100
       Cull [_CullMode]
       ZWrite [_Zwrite]
       Lighting Off
       ZTest [_ZTest]
       Blend [_Src] [_Dst]

       Pass
       {
          CGPROGRAM
          #pragma vertex vert
          #pragma fragment frag
          #pragma target 3.0
          #pragma shader_feature _USE_DISTURBANCE
          #pragma shader_feature _USE_SECOND_DISTURBANCE
          #pragma shader_feature _USE_DISTURBANCE_MASK
          #pragma shader_feature _USE_MASK
          #pragma shader_feature _USE_DISSOlVE
          #pragma shader_feature _USE_SECOND_DISSOlVE
          #pragma shader_feature _USE_DISSOlVE_MASK
          #pragma shader_feature _FORCE_FRONT
          #pragma shader_feature _USE_SOFT_PARTICLES
          #pragma shader_feature _USE_FRESNEL
          #pragma shader_feature _FRESNEL_GRADIENT
          #pragma shader_feature _FRESNEL_MODEL_NORMAL
          // ===== 新增：极坐标宏 =====
          #pragma shader_feature _USE_POLAR_UV
          #pragma multi_compile_instancing
          #define ADDALPHA(col)  _Src*_Dst==1||_Src*_Dst==4? col.xyz*col.w:col.xyz
          #include "UnityCG.cginc"

          struct appdata
          {
             float4 vertex:POSITION;
             float3 normal:NORMAL;
             float4 uv:TEXCOORD0;
             float4 uv1:TEXCOORD1;
             float4 color:COLOR;
             UNITY_VERTEX_INPUT_INSTANCE_ID
          };

          struct v2f
          {
             float4 uv : TEXCOORD0;
             float4 uv1:TEXCOORD1;
             float4 vertex : SV_POSITION;
             float4 color:TEXCOORD2;
             float3 worldViewDir : TEXCOORD3;
             float3 worldNormal : TEXCOORD4;
             #ifdef _USE_SOFT_PARTICLES
             float4 screenPos : TEXCOORD5;
             #endif
             UNITY_VERTEX_INPUT_INSTANCE_ID
          };

          sampler2D _MainTex;
          float4 _MainTex_ST;
          float4 _MainColor;
          float _MainTexSpeed_x;
          float _MainTexSpeed_y;
          float _Src;
          float _Dst;
          float _Disturbance_Pow;

          // ===== 新增极坐标属性 =====
          #ifdef _USE_POLAR_UV
          float4 _PolarCenter;
          #endif

          #ifdef _USE_DISTURBANCE
          sampler2D _DisturbanceTex; float4 _DisturbanceTex_ST; float _DistSpeed_x; float _DistSpeed_y;
          #endif
          #ifdef _USE_SECOND_DISTURBANCE
          sampler2D _DisturbanceTex01; float4 _DisturbanceTex01_ST; float _DistSpeed01_x; float _DistSpeed01_y;
          #endif
          #ifdef _USE_DISTURBANCE_MASK
          sampler2D _DistMask; float4 _DistMask_ST;
          #endif
          #ifdef _USE_MASK
          sampler2D _MaskTex; float4 _MaskTex_ST; float _MaskSpeed_x; float _MaskSpeed_y; float _Mask_Percentage; float _MaskSoft;
          #endif
          #ifdef _USE_DISSOlVE
          sampler2D _DissolveTex; float4 _DissolveTex_ST; float _Dissolve_Soft; float _DissEdgeRange; float _DissEdgeRangeSoft; float _DissolveSpeed_x; float _DissolveSpeed_y;
          #endif
          #ifdef _USE_SECOND_DISSOlVE
          sampler2D _DissolveTex01; float4 _DissolveTex01_ST; float _DissolveSpeed01_x; float _DissolveSpeed01_y;
          #endif
          #ifdef _USE_DISSOlVE_MASK
          sampler2D _DissolveMask; float4 _DissolveMask_ST;
          #endif
          float4 _DissEdgeColor;
          float _DepthOffset;
          float _SoftParticlesFadeDistance;
          UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

          #ifdef _USE_FRESNEL
          sampler2D _GradientTex;
          float4 _GradientTex_ST;
          float4 _FresnelColor;
          float _FresnelPower;
          float _FresnelIntensity;
          float4 _GradientSpeed;
          #endif

          v2f vert (appdata v)
          {
             v2f o;
             UNITY_SETUP_INSTANCE_ID(v);
             UNITY_TRANSFER_INSTANCE_ID(v, o);
             
             o.vertex = UnityObjectToClipPos(v.vertex);
             #ifdef _USE_SOFT_PARTICLES
             o.screenPos = ComputeScreenPos(o.vertex);
             COMPUTE_EYEDEPTH(o.screenPos.z);
             #endif
             
             #ifdef _FORCE_FRONT
             #else
             o.vertex.z += _DepthOffset * 0.01;
             #endif

             #ifdef _USE_FRESNEL
             float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
             o.worldViewDir = normalize(_WorldSpaceCameraPos - worldPos);
             
             #ifdef _FRESNEL_MODEL_NORMAL
             o.worldNormal = UnityObjectToWorldNormal(v.normal);
             #else
             o.worldNormal = normalize(v.color.rgb * 2.0 - 1.0 + 0.0001);
             #endif
             #endif

             o.uv = v.uv;
             o.uv1 = v.uv1;
             o.color = v.color;
             return o;
          }
          
          half4 frag (v2f i) : SV_Target
          { 
             UNITY_SETUP_INSTANCE_ID(i);
             
             // ===== 新增：极坐标转换核心逻辑 =====
             float2 baseUV = i.uv.xy;
             #ifdef _USE_POLAR_UV
             float2 delta = baseUV - _PolarCenter.xy;
             // 极坐标半径 (V轴)
             float radius = length(delta) * 2.0; 
             // 极坐标角度 (U轴)，将 atan2 的 [-PI, PI] 映射到 [0, 1]
             float angle = atan2(delta.y, delta.x) / (2.0 * UNITY_PI) + 0.5;
             baseUV = float2(angle, radius);
             #endif
             // ====================================

             // 将后续所有的 i.uv.xy 替换为处理过极坐标的 baseUV
             float2 uv = TRANSFORM_TEX(baseUV, _MainTex);

             #ifdef _USE_MASK
             float2 uv_mask = TRANSFORM_TEX(baseUV, _MaskTex)+ float2(frac(_MaskSpeed_x*_Time.y), frac(_MaskSpeed_y*_Time.y));
             float mask = tex2D(_MaskTex, uv_mask).r;
             half maskValue=smoothstep(_Mask_Percentage, (_Mask_Percentage+_MaskSoft), mask);
             #else
             half maskValue=1;
             #endif

             #ifdef _USE_DISTURBANCE
             float2 uv_dist = TRANSFORM_TEX(baseUV, _DisturbanceTex) + float2(frac(_DistSpeed_x*_Time.y), frac(_DistSpeed_y*_Time.y));
             float dist=tex2D(_DisturbanceTex, uv_dist).r;
             #ifdef _USE_SECOND_DISTURBANCE
             float2 uv_dist1 = TRANSFORM_TEX(baseUV,_DisturbanceTex01) + float2(frac(_DistSpeed01_x*_Time.y), frac(_DistSpeed01_y*_Time.y));
             dist=max(dist,tex2D(_DisturbanceTex01, uv_dist1).r);
             #endif
             #ifdef _USE_DISTURBANCE_MASK
             float2 uv_DistMask=TRANSFORM_TEX(baseUV, _DistMask);
             float distMask=tex2D(_DistMask, uv_DistMask).r;
             dist *= distMask;
             #endif
             #else
             float dist=0;
             #endif

             #ifdef _USE_DISSOlVE
             float DissolveMask=0;
             float2 uv_DissolveTex = TRANSFORM_TEX(baseUV, _DissolveTex)+frac(float2(_DissolveSpeed_x,_DissolveSpeed_y)*_Time.y);
             float Dissolve = tex2D(_DissolveTex, uv_DissolveTex).r;
             #ifdef _USE_SECOND_DISSOlVE
             float2 uv_DissolveTex01 = TRANSFORM_TEX(baseUV, _DissolveTex01)+frac(float2(_DissolveSpeed01_x,_DissolveSpeed01_y)*_Time.y);
             float Dissolve01 = tex2D(_DissolveTex01, uv_DissolveTex01).r;
             Dissolve=max(Dissolve,Dissolve01);
             #endif
             #ifdef _USE_DISSOlVE_MASK
             float2 uv_DissolveMask = TRANSFORM_TEX(baseUV, _DissolveMask);
             DissolveMask=1-tex2D(_DissolveMask,uv_DissolveMask).r;
             #endif
             float diss=smoothstep(i.uv1.x+DissolveMask, (i.uv1.x+DissolveMask+_Dissolve_Soft), Dissolve);
             float dissEdge = smoothstep(_DissEdgeRange+i.uv1.x+DissolveMask, _DissEdgeRange+i.uv1.x+DissolveMask+_DissEdgeRangeSoft,Dissolve);
             #else
             float diss=1;
             float dissEdge=1;
             #endif

             uv = uv + dist *_Disturbance_Pow + float2(frac(_MainTexSpeed_x*_Time.y), frac(_MainTexSpeed_y*_Time.y)) + (i.uv.zw) - float2(0,1);
             float4 col = tex2D(_MainTex, uv);

             #ifdef _USE_FRESNEL
             float fresnel = 1.0 - saturate(dot(i.worldNormal, i.worldViewDir));
             fresnel = pow(saturate(fresnel), _FresnelPower) * _FresnelIntensity;

             float4 fresnelColor;
             #ifdef _FRESNEL_GRADIENT
             float2 gradUV = float2(fresnel, 0) + _GradientSpeed.xy * _Time.y;
             fresnelColor = tex2D(_GradientTex, gradUV);
             fresnelColor.rgb *= _FresnelColor.rgb;
             fresnelColor.a *= fresnel;
             #else
             fresnelColor = fresnel * _FresnelColor;
             #endif

             col.rgb = lerp(col.rgb, fresnelColor.rgb, fresnelColor.a);
             col.rgb += fresnelColor.rgb * 0.5; 
             col.a = max(col.a, fresnelColor.a * 0.8);
             #endif

             #ifdef _USE_SOFT_PARTICLES
             float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
             float partZ = i.screenPos.z;
             float fade = saturate((sceneZ - partZ) / max(_SoftParticlesFadeDistance, 0.0001));
             col.a *= fade;
             #endif

             half4 final;
             final.rgb = lerp(_DissEdgeColor.rgb, col.rgb * _MainColor.rgb, dissEdge) * i.color.rgb;
             final.a = col.a * maskValue * diss * _MainColor.a * i.color.a;
             final.rgb = ADDALPHA(final);
             return final;
          }
          ENDCG
       }
    }
    CustomEditor "VfxCommonProGUI"
}