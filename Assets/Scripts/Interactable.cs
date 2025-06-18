using UnityEngine;

public class Interactable : MonoBehaviour
{
    [SerializeField] PlayerInteraction pi;

    public bool state { get; set; } = false;

    private void Awake()
    {
        pi.AddInteractable(gameObject);
    }
}