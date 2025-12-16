using System.Collections.Generic;
using UnityEngine;
using Component = UnityEngine.Component;

namespace Utils
{
    public static class MonoUtility
    {
        public static T[] GetAllComponent<T>(this Transform root) where T : Component
        {
            var ls =new List<Transform>();
            GetAllChild(root,ls);
            var components = new List<T>();
            components.AddRange(root.GetComponents<T>());
            foreach (var transform in ls)
            {
                components.AddRange(transform.GetComponents<T>());
            }
            return components.ToArray();
        }
        private static void GetAllChild(Transform root,List<Transform> ls )
        {
            for (int i = 0; i < root.childCount; i++)
            {
                ls.Add(root.GetChild(i));
                GetAllChild(root.GetChild(i),ls);
            }
        }
        public static T GetOrAddComponent<T>(this Transform root) where T : Component
        {
            var component=root.GetComponent<T>();
            if(component!=null){
                return component;
            }else{
                return root.gameObject.AddComponent<T>();
            }

        }
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            return go.transform.GetOrAddComponent<T>();

        }
        public static Component GetOrAddComponent(this Transform root,System.Type type)
        {
            var component=root.GetComponent(type);
            if(component!=null){
                return component;
            }else{
                return root.gameObject.AddComponent(type);
            }

        }
    }
}