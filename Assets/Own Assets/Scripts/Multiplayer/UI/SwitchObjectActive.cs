using System.Collections.Generic;
using UnityEngine;

public class SwitchObjectActive : MonoBehaviour
{
    public List<GameObject> _firstObjectsToDeativate;
    public List<GameObject> _secondObjectsToDeativate;

    public void Activate(bool activate)
    {
        ActivateList(!activate, _firstObjectsToDeativate);
        ActivateList(activate, _secondObjectsToDeativate);
    }

    private void ActivateList(bool activate, List<GameObject> gameObjects)
    {
        foreach (GameObject item in gameObjects)
        {
            item.SetActive(activate);
        }
    }
}
