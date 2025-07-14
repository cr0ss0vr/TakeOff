using UnityEngine;

public class TempCamDisabler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Camera.main != null && Camera.main != GetComponent<Camera>())
        {
            gameObject.SetActive(false);
        }
    }
}
