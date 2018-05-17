using UnityEngine;
using TMPro;

public class Framerate : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI text;

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        text.text = $"{(int)(1.0/deltaTime)} FPS\n{deltaTime* 1000 : 0.00} ms";
    }
}
