using UnityEngine;

/// <summary>
/// Gây sát thương dùng chung cho các đòn của Gronk (kiếm ném, chém bổ đất).
/// Ưu tiên DucThanh.PlayerStats; nếu không có thì gửi message "TakeDamage" (MobHealthBar, EnemyTest...).
/// </summary>
public static class GronkDamage
{
    public static void Apply(GameObject target, float amount)
    {
        if (!target || amount <= 0f) return;

        var stats = target.GetComponentInParent<DucThanh.PlayerStats>();
        if (stats)
        {
            if (!stats.IsDead) stats.ModifyHealth(-amount);
            return;
        }
        target.SendMessageUpwards("TakeDamage", amount, SendMessageOptions.DontRequireReceiver);
    }
}
