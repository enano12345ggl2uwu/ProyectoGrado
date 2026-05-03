using UnityEngine;

/// <summary>
/// Helper para evitar que una acción se dispare múltiples veces en poco tiempo.
/// Uso: en ColorJump para evitar contar doble por cruce agresivo.
///
/// Ejemplo:
///   private ActionDebouncer debouncer = new ActionDebouncer();
///
///   if (debouncer.TryFire(cooldownSeconds: 0.5f))
///   {
///       // Esta línea solo se ejecuta cada 0.5s como máximo
///       OnCorrectAnswer();
///   }
/// </summary>
public class ActionDebouncer
{
    private float lastFireTime = -999f;

    public bool TryFire(float cooldownSeconds)
    {
        float now = Time.time;
        if (now - lastFireTime >= cooldownSeconds)
        {
            lastFireTime = now;
            return true;
        }
        return false;
    }

    public void Reset()
    {
        lastFireTime = -999f;
    }
}
