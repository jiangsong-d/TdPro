using UnityEngine;
using UpdetaFramework;

namespace UpdetaFramework
{
    internal class UpdetaEventDriver : MonoBehaviour
    {
        void Update()
        {
            UpdetaEvent.Update();
        }
        void OnDestroy()
        {
            UpdetaEvent.Destroy();
        }
    }
}