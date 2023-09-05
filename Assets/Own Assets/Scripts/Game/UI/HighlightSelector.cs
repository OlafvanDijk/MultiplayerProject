using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HighlightSelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private bool _doScaling = true;
    [ShowIf(nameof(_doScaling))] [SerializeField] private Vector3 _scaling = new(1.2f, 1.2f, 1f);
    [SerializeField] private bool _doHighlight = true;
    [ShowIf(nameof(_doHighlight))][SerializeField] private GameObject _border;
    [ShowIf(nameof(_doHighlight))] [SerializeField] private Color _highlightColor = Color.white;

    public UnityEvent OnButtonHighlight = new();
    public UnityEvent OnButtonStopHighlight = new();

    private bool _highlighted;
    private bool _scaleSet;
    private Vector3 _originalScale;
    private GameObject _highlight;

    private Sequence _sequence;

    private void Awake()
    {
        if (!_scaleSet)
        {
            _scaleSet = true;
            _originalScale = transform.localScale;
        }

        if(_doHighlight)
        {
            _highlight = Instantiate(_border, _border.transform.parent);
            _highlight.transform.SetSiblingIndex(_border.transform.GetSiblingIndex() + 1);
            _highlight.SetActive(false);
            Image borderImage = _highlight.GetComponent<Image>();
            borderImage.raycastTarget = false;
            borderImage.color = _highlightColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_highlighted)
            return;

        _highlighted = true;

        if (_doScaling)
            Scale(true);

        if (_doHighlight)
            _highlight.SetActive(true);

        OnButtonHighlight.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(!_highlighted)
            return;

        _highlighted = false;
        if (_doScaling)
            Scale(false);

        if (_doHighlight)
            _highlight.SetActive(false);

        OnButtonStopHighlight.Invoke();
    }

    private void Scale(bool doScale)
    {
        if (doScale)
        {
            _sequence?.Kill(true);
            _sequence = DOTween.Sequence();
            _sequence.Insert(0, transform.DOScale(_scaling, 0.2f));
        }
        else
        {
            _sequence?.Kill(true);
            _sequence = DOTween.Sequence();
            _sequence.Insert(0, transform.DOScale(_originalScale, 0.25f));
        }
    }


}
