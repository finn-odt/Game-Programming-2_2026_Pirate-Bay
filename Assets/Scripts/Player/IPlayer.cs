using System.Collections.Generic;
using UnityEngine;

namespace SLTypes
{
    public interface IPlayer : IHuman
    {
        int CollectedCoins { get; }
        
        List<GameObject> CurrentGrounds { get; set; }  // for camera culling (CameraController.cs)

        void AddCoins(int amount);
        void SetInitialCoins(int coins);
    }
}