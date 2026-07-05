// -----------------------------------------------------------------------
// <copyright file="CoroutineRunner.cs" company="William">
// Role Selector - MEC kullanmadan, saf Unity coroutine'lerini çalıştırmak için
// tek amaçlı bir MonoBehaviour taşıyıcısı.
// </copyright>
// -----------------------------------------------------------------------

namespace RoleSelector.Core
{
    using UnityEngine;

    /// <summary>
    /// Coroutine'leri barındırmak için sahneye eklenen, görünmez tek bir <see cref="MonoBehaviour"/>.
    /// EXILED/Project Mer'in dışarıdan sağladığı bir kütüphaneye (MEC vb.) ihtiyaç duymadan
    /// UnityEngine'in kendi <c>StartCoroutine</c>/<c>WaitForSeconds</c> altyapısını kullanır.
    /// </summary>
    internal sealed class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner instance;

        /// <summary>
        /// Gets the singleton instance, sahnede yoksa oluşturur.
        /// </summary>
        public static CoroutineRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject holder = new("RoleSelector_CoroutineRunner");
                    Object.DontDestroyOnLoad(holder);
                    instance = holder.AddComponent<CoroutineRunner>();
                }

                return instance;
            }
        }

        /// <summary>
        /// Sahnedeki taşıyıcı objeyi yok eder (plugin devre dışı bırakıldığında).
        /// </summary>
        public static void Destroy()
        {
            if (instance != null)
            {
                Object.Destroy(instance.gameObject);
                instance = null;
            }
        }
    }
}
