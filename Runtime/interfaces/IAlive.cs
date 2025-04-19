using UnityEngine;

public interface IAlive 
{
    float Health { get; set; }
    float MaxHealth { get; set; }
    bool isAlive { get; set; }

    void TakeDamage(float damage);
    void Die();
    void Freeze(bool a_freeze);
    void ReBirth();
    void ForceReBirth();
    void Heal(float healAmount);
    void SetHealth(float health);
    float GetHealth();
    float GetMaxHealth();
    bool IsAlive();
    void SetAlive(bool alive);
    void SetInvincible(bool invincible);
    bool IsInvincible();
    void SetInvincibleTime(float time);
    float GetInvincibleTime();



}
