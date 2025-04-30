Shader "Custom/UnlitTransparentColorMultiply"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100

        Pass
        {
            ZWrite Off
            Cull Off
            Lighting Off
            Blend SrcAlpha OneMinusSrcAlpha

            SetTexture [_MainTex] 
            {
                Combine texture * primary
            }
        }
    }
}
