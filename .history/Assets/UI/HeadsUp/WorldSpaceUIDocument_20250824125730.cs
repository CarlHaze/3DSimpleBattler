using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
//using UnityUtils;

public class WorldSpaceUIDocument : MonoBehaviour
{
    const string k_transparentShader = "Unlit/Transparent";
    const string k_textureShader = "Unlit/Texture";
    const string k_mainTex = "_MainTex";
    static readonly int Maintex = Shader.PropertyToID(k_mainTex);

    [SerializeField] int panelWidth = 1280;
    [SerializeField] int panelHeight = 720;
    [SerializeField] float panelScale = 1.0f;
    [SerializeField] float pixelsPerUnit = 500.0f;
    [SerializeField] VisualTreeAsset visualTreeAsset;
    [SerializeField] PanelSettings panelSettings;
    [SerializeField] RenderTexture renderTextureAsset;
    
        

}
