using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light))]
public class TorchController : MonoBehaviour
{
    [Header("Light Settings")]
    public float baseIntensity = 5f;
    public float minIntensity = 0.3f;    
    public float maxIntensity = 8f;

    [Header("Distance Compensation")]
    public float idealDistance = 2.5f;   
    public float compensationStrength = 2.2f;
    public LayerMask raycastMask;

    [Header("Flicker")]
    public bool enableFlicker = true;
    public float minTimeBetweenFlickers = 2f;  
    public float maxTimeBetweenFlickers = 8f; 
    public float flickerDuration = 0.07f;      
    public float cutoutIntensity = 0.2f;       

    [Header("EnemyFlicker")]
    public bool enableEnemyFlicker = false;
    public float flickerSpeed = 8f;
    public float flickerAmount = 80f;

    private float flickerTimer;
    private float flickerDurationTimer;
    private bool isFlickering;

    private Light lightSource;
    private float flickerOffset;

    void Start()
    {
        lightSource = GetComponent<Light>();
        lightSource.cookie = GenerateTorchCookie();
        flickerOffset = Random.Range(0f, 100f);
        flickerTimer = Random.Range(minTimeBetweenFlickers, maxTimeBetweenFlickers);
    }

    void Update()
    {
        float targetIntensity = baseIntensity;

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, lightSource.range, raycastMask))
        {
            float distance = hit.distance;
            float distanceRatio = distance / idealDistance; 

            float compensation = Mathf.Pow(distanceRatio, compensationStrength);
            targetIntensity = Mathf.Clamp(baseIntensity * compensation, minIntensity, maxIntensity);
            Debug.Log("Target Intensity: " + targetIntensity + " (Distance: " + distance + ")"); 
        }

        if (enableFlicker)
        {
            if (isFlickering)
            {
                flickerDurationTimer -= Time.deltaTime;
                targetIntensity *= cutoutIntensity;

                if (flickerDurationTimer <= 0f)
                {
                    isFlickering = false;
                    flickerTimer = Random.Range(minTimeBetweenFlickers, maxTimeBetweenFlickers);
                }
            }
            else
            {
                flickerTimer -= Time.deltaTime;

                if (flickerTimer <= 0f)
                {
                    isFlickering = true;
                    flickerDurationTimer = flickerDuration;
                }
            }
        }

        if (enableEnemyFlicker)
        {
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, flickerOffset);
            targetIntensity += (noise - 0.5f) * flickerAmount;
        }

        float lerpSpeed = targetIntensity < lightSource.intensity ? 25f : 10f;
        lightSource.intensity = Mathf.Lerp(lightSource.intensity, targetIntensity, Time.deltaTime * lerpSpeed);
    }

    public static Texture2D GenerateTorchCookie(int size = 256)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.Alpha8, false);
        Vector2 centre = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), centre) / maxDist;

                float alpha = Mathf.Clamp01(1f - Mathf.Pow(dist, 0.6f));
                tex.SetPixel(x, y, new Color(alpha, alpha, alpha, alpha));
            }
        }

        tex.Apply();
        return tex;
    }
}