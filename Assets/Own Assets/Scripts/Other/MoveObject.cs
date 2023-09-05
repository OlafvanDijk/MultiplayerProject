using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MoveObject : MonoBehaviour
{
    [SerializeField] private Vector3 _moveBy;
    [SerializeField] private bool _local = true;

    private Vector3 _originalPos;
    private Transform _transform;

    private void Awake()
    {
        _transform = transform;
        if(_local)
            _originalPos = transform.localPosition;
        else
            _originalPos = _transform.position;
    }

    public void Move(bool activate)
    {
        if(_local)
        {
            if (activate)
                _transform.localPosition = _originalPos + _moveBy;
            else
                _transform.localPosition = _originalPos;
        }
        else
        {
            if (activate)
                _transform.position = _originalPos + _moveBy;
            else
                _transform.position = _originalPos;
        }
    }
}
