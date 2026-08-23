using System.Collections.Generic;
using UnityEngine;

namespace SLTypes
{
    public interface ILife
    {
        Transform Transform { get; }
        Vector3 Position { get; }
        
        int Health { get; }
        float HealthPercentage { get; }
        
        void AddHealth(int amount);
        void TakeDamage(int amount);

        void SetInitialPosition(Vector3 pos, Quaternion rot);
    }
}