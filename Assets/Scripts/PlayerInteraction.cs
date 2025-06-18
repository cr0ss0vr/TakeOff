using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    List<GameObject> Interactables;

    public bool AddInteractable(GameObject gameObject)
    {
        if (Interactables.Contains(gameObject)) {
            Interactables.Add(gameObject);
            return true;
        } else {
            UnityEngine.Debug.LogError($"Error: Interactable already in collection.");
            return false;
        }
    }

    public bool RemoveInteractable(GameObject gameObject)
    {
        return Interactables.Remove(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        var interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
        Interactables = interactables.Select(i => i.gameObject).ToList();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Interactable interactable = hit.collider.GetComponent<Interactable>();

                if (interactable != null)
                {
                    interactable.state = !interactable.state;
                }
            }
        }
    }
}
