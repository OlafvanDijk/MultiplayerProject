using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HighlightSelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool DoScaling;
    [ShowIf(nameof(DoScaling))] public Vector3 Scaling = Vector3.one;

    public UnityEvent OnButtonHighlight = new();
    public UnityEvent OnButtonStopHighlight = new();

    private bool _highlighted;
    private bool _scaleSet;
    private Vector3 _originalScale;

    private Sequence _sequence;

    private void Start()
    {
        if (!_scaleSet)
        {
            _scaleSet = true;
            _originalScale = transform.localScale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_highlighted)
            return;

        _highlighted = true;

        if (DoScaling)
            Scale(true);

        OnButtonHighlight.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _highlighted = false;
        if (DoScaling)
        {
            Scale(false);
        }
        OnButtonStopHighlight.Invoke();
    }

    private void Scale(bool doScale)
    {
        if (doScale)
        {
            _sequence?.Kill(true);
            _sequence = DOTween.Sequence();
            _sequence.Insert(0, transform.DOScale(Scaling, 0.2f));
        }
        else
        {
            _sequence?.Kill(true);
            _sequence = DOTween.Sequence();
            _sequence.Insert(0, transform.DOScale(_originalScale, 0.25f));
        }
    }


}
