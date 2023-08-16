using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeMaterial : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private Material _activationMaterial;
    [SerializeField] private Material _deactivationMaterial;

    public void Activate(bool activate)
    {
        if(activate)
            _renderer.material = _activationMaterial;
        else
            _renderer.material = _deactivationMaterial;
    }
}
