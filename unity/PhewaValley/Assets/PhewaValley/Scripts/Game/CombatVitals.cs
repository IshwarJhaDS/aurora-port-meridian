using UnityEngine;

namespace PhewaValley
{
    public sealed class CombatVitals
    {
        public float Health { get; private set; } = 100;
        public float Stamina { get; private set; } = 100;
        public bool GuardBroken { get; private set; }
        float recovery;

        public void Reset() { Health = Stamina = 100; recovery = 0; GuardBroken = false; }

        public bool Spend(float amount)
        {
            if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount) || Stamina < amount || Health <= 0) return false;
            Stamina -= amount; recovery = .65f; return true;
        }

        public float Hit(float damage, bool guarded, bool invulnerable)
        {
            GuardBroken = false;
            if (invulnerable || damage <= 0 || float.IsNaN(damage) || float.IsInfinity(damage) || Health <= 0) return 0;
            if (guarded && Spend(14)) return 0;
            GuardBroken = guarded;
            if (guarded) { Stamina = 0; recovery = 1.2f; }
            float taken = Mathf.Min(Health, damage); Health -= taken; return taken;
        }

        public void Tick(float dt, bool guarding)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            recovery = Mathf.Max(0, recovery - dt);
            if (recovery == 0 && !guarding && Health > 0) Stamina = Mathf.Min(100, Stamina + dt * 22);
        }
    }
}
