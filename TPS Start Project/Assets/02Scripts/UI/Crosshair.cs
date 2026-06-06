using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    public Image AimPointReticle;
    public Image HitPointReticle;

    public float SmoothTime = 0.2f;
    
    private Camera _screenCamera;
    private RectTransform _crossHairRectTransform;

    private Vector2 _currentHitPointVelocity;
    private Vector2 _targetPoint;

    private void Awake()
    {
        _screenCamera = Camera.main;
        _crossHairRectTransform = HitPointReticle.GetComponent<RectTransform>();
    }

    public void SetActiveCrosshair(bool p_isActive)
    {
        HitPointReticle.enabled = p_isActive;
        AimPointReticle.enabled = p_isActive;
    }

    public void UpdatePosition(Vector3 p_worldPoint)
    {
        _targetPoint = _screenCamera.WorldToScreenPoint(p_worldPoint);
    }

    private void Update()
    {
        if (!HitPointReticle.enabled) return;

        _crossHairRectTransform.position = Vector2.SmoothDamp(_crossHairRectTransform.position, _targetPoint,
            ref _currentHitPointVelocity, SmoothTime);
    }
}