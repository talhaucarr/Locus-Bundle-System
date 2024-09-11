using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BundleSystem
{
    [Serializable]
    public class AssetReference
    {
        public string guid;
    }
}

#if UNITY_EDITOR

namespace BundleSystem
{
    using UnityEditor;
    using UnityEngine;
    
    [CustomPropertyDrawer(typeof(AssetReference))]
    public class AssetReferencePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            var guidProperty = property.FindPropertyRelative("guid");

            Object selectedAsset = EditorGUI.ObjectField(
                new Rect(position.x, position.y, position.width, position.height),
                AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guidProperty.stringValue)),
                typeof(Object),
                false
            );

            if (selectedAsset != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(selectedAsset);
                string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

                guidProperty.stringValue = assetGuid;
            }

            property.serializedObject.ApplyModifiedProperties();
            EditorGUI.EndProperty();
        }
    }
}
#endif