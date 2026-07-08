using UnityEngine;

[RequireComponent(typeof(ParabolicMover))]
public class ProjectlieBase : MonoBehaviour
{
    [Tooltip("発射物タイプ")] public ProjectileType Type;
}
