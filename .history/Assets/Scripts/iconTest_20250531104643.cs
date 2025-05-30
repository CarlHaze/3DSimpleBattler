using UnityEngine;

public class iconTest : MonoBehaviour
{
   // Attach this to your plane in a test scene
void Start()
{
    Renderer rend = GetComponent<Renderer>();
    Texture2D tex = Resources.Load<Texture2D>("sword");
    rend.material.shader = Shader.Find("Unlit/Texture");
    rend.material.mainTexture = tex;
}


    // Update is called once per frame
    void Update()
    {
        
    }
}
