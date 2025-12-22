using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class TubeViewClick : MonoBehaviour, IPointerClickHandler
{
    private TubeView tubeView;
    private void Awake()
    {
        tubeView = GetComponentInParent<TubeView>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (tubeView == null) return;

        TubeController.Ins?.OnTubeClicked(tubeView);
    }
}
