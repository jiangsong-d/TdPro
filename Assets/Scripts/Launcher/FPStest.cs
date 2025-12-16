using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float pollingTime = 0.5f; // 更新间隔（秒）
    private float timeCounter;
    private int frameCounter;

    void Update()
    {
        timeCounter += Time.deltaTime;
        frameCounter++;
        
        if (timeCounter >= pollingTime)
        {
            int fps = Mathf.RoundToInt(frameCounter / timeCounter);
            fpsText.text = "FPS: " + fps;
            
            timeCounter -= pollingTime;
            frameCounter = 0;
        }
    }
}