using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PositionFollower : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _targetPosition;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float _smoothTime = 0.05f; // 追従の滑らかさ（値が小さいほど速く追従）

    private Vector3 _currentVelocity;

    private void LateUpdate()
    {
        if (_target == null) return;

        // 目標位置の計算（Targetの向きを考慮）
        _targetPosition = _target.position + (_target.rotation * _offset);

        // Vector3.SmoothDamp で滑らかに移動
        transform.position = Vector3.SmoothDamp(
            transform.position, 
            _targetPosition, 
            ref _currentVelocity, 
            _smoothTime
        );
    }

    public void SetTarget(Transform targetTransform)
    {
        _target = targetTransform;
    }

    public void ClearTarget()
    {
        _target = null;
    }

    public async UniTask WaitArrivalAsync()
    {
        if (_target == null) return;

        await UniTask.WaitUntil(() =>
        {
            float thresholdDistance = 0.01f;
            float sqrDistance = (transform.position - _targetPosition).sqrMagnitude;
            return sqrDistance <= (thresholdDistance * thresholdDistance);
        });
    }
}
