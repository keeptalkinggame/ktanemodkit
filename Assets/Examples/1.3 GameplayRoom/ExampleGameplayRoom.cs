using UnityEngine;

public class ExampleGameplayRoom : MonoBehaviour
{
    public Light roomLight;

	void Awake()
    {
        KMGameplayRoom gameplayRoom = GetComponent<KMGameplayRoom>();
        Debug.Log("Setting on light change");
        gameplayRoom.OnLightChange = OnLightChange;
	}

    public void OnLightChange(bool on)
    {
        roomLight.enabled = on;
    }
}
