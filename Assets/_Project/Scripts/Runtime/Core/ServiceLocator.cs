using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectStS.Core
{
    /// <summary>
    /// 싱글톤 대신 사용하는 서비스 등록/조회 패턴.
    /// GameBootstrapper에서 초기화되며, DontDestroyOnLoad 오브젝트에 연결된다.
    /// </summary>
    public static class ServiceLocator
    {
        #region Private Fields

        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>(16);

        #endregion

        #region Public Methods

        /// <summary>
        /// 서비스 인스턴스를 등록한다.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            Type type = typeof(T);

            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] {type.Name} 서비스가 이미 등록되어 있습니다. 덮어씁니다.");
            }

            _services[type] = service;
        }

        /// <summary>
        /// 등록된 서비스를 조회한다.
        /// </summary>
        public static T Get<T>() where T : class
        {
            Type type = typeof(T);

            if (_services.TryGetValue(type, out object service))
            {
                return (T)service;
            }

            Debug.LogError($"[ServiceLocator] {type.Name} 서비스가 등록되지 않았습니다.");
            return null;
        }

        /// <summary>
        /// 등록된 서비스를 시도 조회한다. 없으면 false를 반환한다.
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            Type type = typeof(T);

            if (_services.TryGetValue(type, out object obj))
            {
                service = (T)obj;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// 특정 서비스 등록을 해제한다.
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            _services.Remove(typeof(T));
        }

        /// <summary>
        /// 모든 서비스 등록을 초기화한다.
        /// </summary>
        public static void Clear()
        {
            _services.Clear();
        }

        #endregion
    }
}
