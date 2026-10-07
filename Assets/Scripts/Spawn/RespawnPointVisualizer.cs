using System;
using System.Collections.Generic;
using UnityEngine;

namespace Spawn
{
    public class RespawnPointVisualizer : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            foreach (Transform child in transform)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(child.position, 1f);
            }
        }
    }
}
