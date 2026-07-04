#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

public class VfxCommonProGUI : ShaderGUI
{
    public static Material copy = null;
    public static Material revokeMat = null;

    static Color mainColor = new Color(1.0f, 0.0f, 0.0f, 0.5f);
    static Color distColor = new Color(1.0f, 0.6f, 0.0f, 0.5f);
    static Color maskColor = new Color(1.0f, 1.0f, 0.0f, 0.5f);
    static Color dissColor = new Color(0.0f, 1.0f, 0.0f, 0.5f);
    static Color depthColor = new Color(0.8f, 0.8f, 0.8f, 0.5f); 
    static Color softColor = new Color(0.4f, 0.8f, 1.0f, 0.5f);
    static Color fresnelColor = new Color(1.0f, 0.8f, 0.4f, 0.5f); 
    static Color setingColor = new Color(0.0f, 0.46f, 1.0f, 0.5f);

    static bool mainFold, distFold, maskFold, dissFold, SetFold, depthFold, softFold, fresnelFold;

    int BlendModeSelectedOption = 0;
    int CullModeSelectedOption = 0;

    MaterialProperty MainColor = null;
    MaterialProperty MainTex = null;
    MaterialProperty MainTexSpeedX = null;
    MaterialProperty MainTexSpeedY = null;
    
    // ===== 新增极坐标属性声明 =====
    MaterialProperty UsePolarUV;
    MaterialProperty PolarCenter;

    MaterialProperty DistTex = null;
    MaterialProperty _Disturbance_Pow;
    MaterialProperty _DistSpeed_x;
    MaterialProperty _DistSpeed_y;
    MaterialProperty DistTex01 = null;
    MaterialProperty _DistSpeed01_x;
    MaterialProperty _DistSpeed01_y;
    MaterialProperty _DistMaskTex;
    MaterialProperty _MaskTex;
    MaterialProperty _MaskSpeed_x;
    MaterialProperty _MaskSpeed_y;
    MaterialProperty _Mask_Percentage;
    MaterialProperty _MaskSoft;
    MaterialProperty _DissolveTex;
    MaterialProperty _DissolveSpeed_x;
    MaterialProperty _DissolveSpeed_y;
    MaterialProperty _DissolveTex01;
    MaterialProperty _DissolveSpeed01_x;
    MaterialProperty _DissolveSpeed01_y;
    MaterialProperty _DissolveMask;
    MaterialProperty _Dissolve_Soft;
    MaterialProperty _DissEdgeRange;
    MaterialProperty _DissEdgeRangeSoft;
    MaterialProperty _DissEdgeColor;
    MaterialProperty _Zwrite;

    MaterialProperty UseDist;
    MaterialProperty UseSecondDist;
    MaterialProperty UseDistMask;
    MaterialProperty UseMask;
    MaterialProperty UseDissolve;
    MaterialProperty UseSecondDisslove;
    MaterialProperty UseDissloveMask;

    MaterialProperty DepthOffset, ForceFront, ZTest;
    MaterialProperty UseSoftParticles, SoftParticlesFadeDistance;
    MaterialProperty UseFresnel, FresnelColor, FresnelPower, FresnelIntensity, FresnelModelNormal;
    MaterialProperty FresnelGradient, GradientTex, GradientSpeed;

    MaterialEditor m_MaterialEditor;

    public void FindProperties(MaterialProperty[] props)
    {
        MainColor = FindProperty("_MainColor", props);
        MainTex = FindProperty("_MainTex", props);
        MainTexSpeedX = FindProperty("_MainTexSpeed_x", props);
        MainTexSpeedY = FindProperty("_MainTexSpeed_y", props);

        // ===== 查找极坐标属性 =====
        UsePolarUV = FindProperty("_UsePolarUV", props);
        PolarCenter = FindProperty("_PolarCenter", props);

        DistTex = FindProperty("_DisturbanceTex", props);
        _DistSpeed_x = FindProperty("_DistSpeed_x", props);
        _DistSpeed_y = FindProperty("_DistSpeed_y", props);
        DistTex01 = FindProperty("_DisturbanceTex01", props);
        _DistSpeed01_x = FindProperty("_DistSpeed01_x", props);
        _DistSpeed01_y = FindProperty("_DistSpeed01_y", props);

        _Disturbance_Pow = FindProperty("_Disturbance_Pow", props);
        _DistMaskTex = FindProperty("_DistMask", props);
        _MaskTex = FindProperty("_MaskTex", props);
        _MaskSpeed_x = FindProperty("_MaskSpeed_x", props);
        _MaskSpeed_y = FindProperty("_MaskSpeed_y", props);
        _Mask_Percentage = FindProperty("_Mask_Percentage", props);
        _MaskSoft = FindProperty("_MaskSoft", props);
        _DissolveTex = FindProperty("_DissolveTex", props);
        _DissolveSpeed_x = FindProperty("_DissolveSpeed_x", props);
        _DissolveSpeed_y = FindProperty("_DissolveSpeed_y", props);

        _DissolveTex01 = FindProperty("_DissolveTex01", props);
        _DissolveSpeed01_x = FindProperty("_DissolveSpeed01_x", props);
        _DissolveSpeed01_y = FindProperty("_DissolveSpeed01_y", props);

        _DissolveMask = FindProperty("_DissolveMask", props);

        _Dissolve_Soft = FindProperty("_Dissolve_Soft", props);
        _DissEdgeRange = FindProperty("_DissEdgeRange", props);
        _DissEdgeRangeSoft = FindProperty("_DissEdgeRangeSoft", props);

        UseDist = FindProperty("_UseDist", props);
        UseSecondDist = FindProperty("_UseSecondDist", props);
        UseDistMask = FindProperty("_UseDistMask", props);
        UseMask = FindProperty("_UseMask", props);
        UseDissolve = FindProperty("_UseDissolve", props);
        UseSecondDisslove = FindProperty("_UseSecondDissolve", props);
        UseDissloveMask = FindProperty("_UseDissolveMask", props);

        _DissEdgeColor = FindProperty("_DissEdgeColor", props);
        _Zwrite = FindProperty("_Zwrite", props);
        
        DepthOffset = FindProperty("_DepthOffset", props);
        ForceFront = FindProperty("_ForceFront", props);
        ZTest = FindProperty("_ZTest", props);
        
        UseSoftParticles = FindProperty("_UseSoftParticles", props);
        SoftParticlesFadeDistance = FindProperty("_SoftParticlesFadeDistance", props);

        UseFresnel = FindProperty("_UseFresnel", props);
        FresnelModelNormal = FindProperty("_FresnelModelNormal", props);
        FresnelColor = FindProperty("_FresnelColor", props);
        FresnelPower = FindProperty("_FresnelPower", props);
        FresnelIntensity = FindProperty("_FresnelIntensity", props);
        FresnelGradient = FindProperty("_FresnelGradient", props);
        GradientTex = FindProperty("_GradientTex", props);
        GradientSpeed = FindProperty("_GradientSpeed", props);
    }

    static bool Foldouts(bool display, string title, ref Color color)
    {
        var style = new GUIStyle("ShurikenModuleTitle");
        style.font = new GUIStyle(EditorStyles.boldLabel).font;
        style.border = new RectOffset(15, 7, 4, 4);
        style.fixedHeight = 25;
        style.contentOffset = new Vector2(30f, -2f);
        style.fontSize = 12;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.0f);

        var rect = GUILayoutUtility.GetRect(16f, 25f, style);
        var colorRect = new Rect(rect.x + 200, rect.y + 4f, 55f, 15f);
        color = EditorGUI.ColorField(colorRect, color);
        style.normal.background = MakeTex(2, 4, color);
        GUI.Box(rect, title, style);

        var e = Event.current;
        var toggleRect = new Rect(rect.x + 15f, rect.y + 2f, 13f, 13f);
        if (e.type == EventType.Repaint)
            EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            display = !display;
            e.Use();
        }
        return display;
    }

    private static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        FindProperties(properties);
        m_MaterialEditor = materialEditor;
        Material targetMaterial = materialEditor.target as Material;

        GUI_Copy(targetMaterial);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        BlendMode(targetMaterial);

        GUI_MainTex(targetMaterial);
        GUI_DistTex(targetMaterial);
        GUI_MaskTex(targetMaterial);
        GUI_DissTex(targetMaterial);

        if (depthFold = Foldouts(depthFold, "深度控制", ref depthColor))
        {
            GUI.backgroundColor = depthColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Introduce("正值：特效在模型后面 | 负值：特效在模型前面 | Force Front：永远最前层");
            
            m_MaterialEditor.ShaderProperty(DepthOffset, "深度偏移");
            
            EditorGUI.BeginChangeCheck();
            m_MaterialEditor.ShaderProperty(ForceFront, "强制最前");
            if (EditorGUI.EndChangeCheck() || ForceFront.floatValue > 0)
            {
                targetMaterial.SetFloat("_ZTest", ForceFront.floatValue > 0 ? 8 : 4);
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));

        if (softFold = Foldouts(softFold, "软粒子", ref softColor))
        {
            GUI.backgroundColor = softColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Introduce("消除粒子与场景物体碰撞时的硬边缘。需要开启相机深度贴图。");
            m_MaterialEditor.ShaderProperty(UseSoftParticles, "开启软粒子");
            if (UseSoftParticles.floatValue > 0)
            {
                m_MaterialEditor.ShaderProperty(SoftParticlesFadeDistance, "软粒子淡出距离");
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));

        if (fresnelFold = Foldouts(fresnelFold, "菲涅尔", ref fresnelColor))
        {
            GUI.backgroundColor = fresnelColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Introduce("纯色/渐变菲涅尔效果。可选择使用模型法线(网格)或顶点颜色(粒子)作为法线方向。");
            m_MaterialEditor.ShaderProperty(UseFresnel, "开启菲涅尔");
            if (UseFresnel.floatValue > 0)
            {
                m_MaterialEditor.ShaderProperty(FresnelModelNormal, "使用模型法线 (非粒子系统)");
                m_MaterialEditor.ShaderProperty(FresnelColor, "菲涅尔颜色");
                m_MaterialEditor.ShaderProperty(FresnelPower, "菲涅尔硬度");
                m_MaterialEditor.ShaderProperty(FresnelIntensity, "菲涅尔强度");
                
                EditorGUILayout.BeginHorizontal();
                m_MaterialEditor.ShaderProperty(FresnelGradient, "使用渐变纹理");
                EditorGUILayout.EndHorizontal();
                
                if (FresnelGradient.floatValue > 0)
                {
                    m_MaterialEditor.ShaderProperty(GradientTex, "渐变贴图");
                    m_MaterialEditor.ShaderProperty(GradientSpeed, "渐变UV滚动速度");
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));

        if (SetFold = Foldouts(SetFold, "渲染设置", ref setingColor))
        {
            GUI.backgroundColor = setingColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Introduce("！渲染冲突可RenderQueue加一，当渲染多个物体都拥有相同材质球时可开启GPU Instancing降低消耗");
            GUI_Depth(targetMaterial);
            GUI_RanderQueue(targetMaterial);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }

    void BlendMode(Material material)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("混合模式");
        GUIContent[] options = new GUIContent[]{
            new GUIContent("add"),
            new GUIContent("alpha_blend"),
            new GUIContent("multiply_blend"),
            new GUIContent("soft_add"),
            new GUIContent("2x Multiply"),
            new GUIContent("premultiply"),
        };
            
        if (material.GetFloat("_Src") == 1 && material.GetFloat("_Dst") == 1) BlendModeSelectedOption = 0;
        if (material.GetFloat("_Src") == 5 && material.GetFloat("_Dst") == 10) BlendModeSelectedOption = 1;
        if (material.GetFloat("_Src") == 2 && material.GetFloat("_Dst") == 0) BlendModeSelectedOption = 2;
        if (material.GetFloat("_Src") == 4 && material.GetFloat("_Dst") == 1) BlendModeSelectedOption = 3;
        if (material.GetFloat("_Src") == 2 && material.GetFloat("_Dst") == 3) BlendModeSelectedOption = 4;
        if (material.GetFloat("_Src") == 1 && material.GetFloat("_Dst") == 10) BlendModeSelectedOption = 5;

        BlendModeSelectedOption = EditorGUILayout.Popup(BlendModeSelectedOption, options);
        if (BlendModeSelectedOption == 0) { material.SetFloat("_Src", 1); material.SetFloat("_Dst", 1); }
        if (BlendModeSelectedOption == 1) { material.SetFloat("_Src", 5); material.SetFloat("_Dst", 10); }
        if (BlendModeSelectedOption == 2) { material.SetFloat("_Src", 2); material.SetFloat("_Dst", 0); }
        if (BlendModeSelectedOption == 3) { material.SetFloat("_Src", 4); material.SetFloat("_Dst", 1); }
        if (BlendModeSelectedOption == 4) { material.SetFloat("_Src", 2); material.SetFloat("_Dst", 3); }
        if (BlendModeSelectedOption == 5) { material.SetFloat("_Src", 1); material.SetFloat("_Dst", 10); }
        EditorGUILayout.EndHorizontal();
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
    }

    void GUI_MainTex(Material material)
    {
        if (mainFold = Foldouts(mainFold, "主帖图", ref mainColor))
        {
            GUI.backgroundColor = mainColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (MainTex != null)
            {
                m_MaterialEditor.ShaderProperty(MainTex, "主帖图");
                m_MaterialEditor.ShaderProperty(MainColor, "颜色");
                m_MaterialEditor.ShaderProperty(MainTexSpeedX, "X速度");
                m_MaterialEditor.ShaderProperty(MainTexSpeedY, "Y速度");

                // ===== 新增：极坐标 GUI 控件 =====
                EditorGUILayout.Space(); // 加点间距美观一点
                m_MaterialEditor.ShaderProperty(UsePolarUV, "开启极坐标 (Polar UV)");
                if (UsePolarUV.floatValue > 0)
                {
                    Introduce("U轴变为旋转角度，V轴变为扩散半径。配合流动速度可制作旋涡或放射光芒。");
                    m_MaterialEditor.ShaderProperty(PolarCenter, "极坐标中心点 (XY)");
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
    }

    void GUI_DistTex(Material material)
    {
        if (distFold = Foldouts(distFold, "扰动", ref distColor))
        {
            GUI.backgroundColor = distColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            m_MaterialEditor.ShaderProperty(UseDist, "开启扰动");
            if (UseDist.floatValue > 0)
            {
                m_MaterialEditor.ShaderProperty(DistTex, "扰动帖图");
                m_MaterialEditor.ShaderProperty(_DistSpeed_x, "X扰动速度");
                m_MaterialEditor.ShaderProperty(_DistSpeed_y, "Y扰动速度");
                GUI_Dist01Tex(material);
                GUI_DistMaskTex(material);
                m_MaterialEditor.ShaderProperty(_Disturbance_Pow, "扰动强度");
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
    }

    void GUI_Dist01Tex(Material material)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        m_MaterialEditor.ShaderProperty(UseSecondDist, "开启第二套扰动");
        if (UseSecondDist.floatValue > 0)
        {
            m_MaterialEditor.ShaderProperty(DistTex01, "叠加扰动");
            m_MaterialEditor.ShaderProperty(_DistSpeed01_x, "X扰动速度");
            m_MaterialEditor.ShaderProperty(_DistSpeed01_y, "Y扰动速度");
        }
        EditorGUILayout.EndVertical();
    }

    void GUI_DistMaskTex(Material material)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        m_MaterialEditor.ShaderProperty(UseDistMask, "开启扰动遮罩");
        if (UseDistMask.floatValue > 0)
        {
            m_MaterialEditor.ShaderProperty(_DistMaskTex, "扰动遮罩");
        }
        EditorGUILayout.EndVertical();
    }

    void GUI_MaskTex(Material material)
    {
        if (maskFold = Foldouts(maskFold, "遮罩", ref maskColor))
        {
            GUI.backgroundColor = maskColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            m_MaterialEditor.ShaderProperty(UseMask, "开启遮罩");
            if (UseMask.floatValue > 0)
            {
                m_MaterialEditor.ShaderProperty(_MaskTex, "遮罩贴图");
                m_MaterialEditor.ShaderProperty(_MaskSpeed_x, "遮罩流动速度x");
                m_MaterialEditor.ShaderProperty(_MaskSpeed_y, "遮罩流动速度y");
                m_MaterialEditor.ShaderProperty(_Mask_Percentage, "遮罩百分比");
                m_MaterialEditor.ShaderProperty(_MaskSoft, "遮罩软硬");
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
    }

    void GUI_DissTex(Material material)
    {
        if (dissFold = Foldouts(dissFold, "溶解", ref dissColor))
        {
            GUI.backgroundColor = dissColor;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Introduce("！使用溶解最好开启粒子系统CustomVertexStream,TEXCOORD0.zw控制主帖图移动,TEXCOORD1.x控制溶解，如果不开启会被模型第二套uv值影响");
            m_MaterialEditor.ShaderProperty(UseDissolve, "开启溶解");
            if (UseDissolve.floatValue > 0)
            {
                m_MaterialEditor.ShaderProperty(_DissolveTex, "溶解贴图");
                m_MaterialEditor.ShaderProperty(_DissolveSpeed_x, "溶解流动速度x");
                m_MaterialEditor.ShaderProperty(_DissolveSpeed_y, "溶解流动速度y");
                GUI_SecondDissTex(material);
                GUI_DissMask(material);
                m_MaterialEditor.ShaderProperty(_Dissolve_Soft, "溶解软硬");
                m_MaterialEditor.ShaderProperty(_DissEdgeRange, "溶解边缘范围");
                m_MaterialEditor.ShaderProperty(_DissEdgeRangeSoft, "溶解边缘软硬");
                m_MaterialEditor.ShaderProperty(_DissEdgeColor, "溶解边缘颜色");
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
    }

    void GUI_SecondDissTex(Material material)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        m_MaterialEditor.ShaderProperty(UseSecondDisslove, "开启第二套溶解");
        if (UseSecondDisslove.floatValue > 0)
        {
            m_MaterialEditor.ShaderProperty(_DissolveTex01, "溶解贴图");
            m_MaterialEditor.ShaderProperty(_DissolveSpeed01_x, "溶解流动速度x");
            m_MaterialEditor.ShaderProperty(_DissolveSpeed01_y, "溶解流动速度y");
        }
        EditorGUILayout.EndVertical();
    }

    void GUI_DissMask(Material material)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        Introduce("溶解遮罩贴图最好设置它的平铺模式(WrapMode)为Clamp，否则边缘会有一点不齐整");
        m_MaterialEditor.ShaderProperty(UseDissloveMask, "开启溶解遮罩");
        if (UseDissloveMask.floatValue > 0)
        {
            m_MaterialEditor.ShaderProperty(_DissolveMask, "溶解遮罩贴图");
        }
        EditorGUILayout.EndVertical();
    }

    void GUI_Depth(Material material)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("正反面剔除");
        GUIContent[] options = new GUIContent[]{
            new GUIContent("双面显示"), new GUIContent("正面剔除"), new GUIContent("背面剔除")
        };
        if (material.GetFloat("_CullMode") == 0) CullModeSelectedOption = 0;
        else if (material.GetFloat("_CullMode") == 1) CullModeSelectedOption = 1;
        else if (material.GetFloat("_CullMode") == 2) CullModeSelectedOption = 2;
        
        CullModeSelectedOption = EditorGUILayout.Popup(CullModeSelectedOption, options);
        material.SetFloat("_CullMode", CullModeSelectedOption);
        EditorGUILayout.EndHorizontal();
        m_MaterialEditor.ShaderProperty(_Zwrite, "遮挡远处");
    }

    void GUI_RanderQueue(Material material)
    {
        MaterialProperty[] props = { };
        base.OnGUI(m_MaterialEditor, props);
    }

    void GUI_Copy(Material mat)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("复制(Copy)"))
        {
            VfxCommonProGUI.copy = new Material(mat);
        }
        if (GUILayout.Button("粘贴(Paste)"))
        {
            if (VfxCommonProGUI.copy != null)
            {
                VfxCommonProGUI.revokeMat = new Material(mat);
                mat.CopyPropertiesFromMaterial(VfxCommonProGUI.copy);
            }
            else
            {
                Debug.LogWarning("没有可粘贴的材质属性数据，请先复制属性");
            }
        }
        if (GUILayout.Button("撤销(Revoke)"))
        {
            if (VfxCommonProGUI.revokeMat != null)
            {
                mat.CopyPropertiesFromMaterial(VfxCommonProGUI.revokeMat);
                VfxCommonProGUI.revokeMat = null;
            }
            else
            {
                Debug.LogWarning("没有可撤销的材质属性数据");
            }
        }
        EditorGUILayout.EndHorizontal();
        Introduce("！撤销按钮只适用于粘贴了别的材质属性之后能撤销回去");
    }

    void Introduce(string x)
    {
        GUIStyle style = new GUIStyle(EditorStyles.wordWrappedLabel);
        GUILayout.Label(x, style);
    }
}
#endif