using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class LookAt : MonoBehaviour
{
    [SerializeField] private ELookAtType _lookAtType;
    [ShowIf(nameof(_lookAtType), ELookAtType.Object)] public Transform _objectTransform;

    [SerializeField] private bool _selfInit = true;
    [SerializeField] private bool _freezeX;
    [SerializeField] private bool _updatePos;

    private Transform _lookAtTransform;

    [Serializable]
    public enum ELookAtType
    {
        Object,
        Camera,
        Player
    }

    private void Start()
    {
        if (_selfInit)
            Init();
    }

    public void Init()
    {
        switch (_lookAtType)
        {
            case ELookAtType.Object:
                _lookAtTransform = _objectTransform;
                break;
            case ELookAtType.Camera:
                _lookAtTransform = Camera.main.transform;
                break;
            case ELookAtType.Player:
                _lookAtTransform = PlayerInfoManager.Instance.EyesPosition.transform;
                break;
            default:
                _lookAtTransform = Camera.main.transform; ;
                break;
        }

        if (!_updatePos)
        {
            DoLookAt();
            Destroy(this);
        }
    }

    private void Update()
    {
        if (!_updatePos)
            return;
        DoLookAt();
    }

    /// <summary>
    /// Looks at the transform.
    /// If the transform is player or camera then rotation should also be added to the equation.
    /// </summary>
    private void DoLookAt()
    {
        if (_lookAtTransform == null)
            return;

        Vector3 lookAt = Vector3.zero;
        Vector3 up = Vector3.up;

        if (_lookAtType == ELookAtType.Player || _lookAtType == ELookAtType.Camera)
        {
            Quaternion rotation = _lookAtTransform.rotation;
            lookAt = transform.position + rotation * Vector3.forward;
            up = rotation * Vector3.up;
        }
        else
        {
            lookAt = _lookAtTransform.position + Vector3.forward;
        }

        transform.LookAt(lookAt, up);
        if (_freezeX)
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.x = 0;
            transform.rotation = Quaternion.Euler(euler);
        }
    }
}
