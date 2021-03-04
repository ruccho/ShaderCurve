using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ShaderCurve.Editors
{
    [CustomEditor(typeof(ShaderCurveAsset))]
    public class ShaderCurveAssetEditor : Editor
    {
        private static readonly GUIContent previewTitle = new GUIContent("Baked Texture");
        private ShaderCurveAsset Target => target as ShaderCurveAsset;

        public override void OnInspectorGUI()
        {
            if (!Target)
            {
                base.OnInspectorGUI();
                return;
            }

            serializedObject.UpdateIfRequiredOrScript();

            var curvesProp = serializedObject.FindProperty("curves");
            var settingsProp = serializedObject.FindProperty("bakeSettings");
            var textureProp = serializedObject.FindProperty("bakedTexture");

            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.PropertyField(curvesProp);
                EditorGUILayout.PropertyField(settingsProp);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (GUILayout.Button("Bake"))
            {
                Bake(Target.BakeSettings);
            }
            if (GUILayout.Button("Clear"))
            {
                ClearTexture();
            }
            

            var texture = textureProp.objectReferenceValue as Texture2D;
            if (!texture)
            {
                EditorGUILayout.HelpBox("Not baked yet.", MessageType.Warning);
                return;
            }
            
            EditorGUILayout.HelpBox(
                $"Baked texture\n" +
                $" Size: {texture.width}x{texture.height}\n" +
                $" Format: {Enum.GetName(typeof(TextureFormat), texture.format)}",
                MessageType.Info);

            int width = texture.width;
            int height = texture.height;
            float ratio = (float) height / width;

            float minWidth = 50f;
            float minHeight = minWidth * ratio;

            float maxWidth = Mathf.Max(minWidth, width);
            float maxHeight = maxWidth * ratio;


            var textureRect = GUILayoutUtility.GetRect(minWidth, minHeight, maxWidth, maxHeight);
            float rectRatio = textureRect.height / textureRect.width;
            if (rectRatio < ratio) textureRect.width = textureRect.height / ratio;
            else textureRect.height = textureRect.width * ratio;
            EditorGUI.DrawPreviewTexture(textureRect, texture);
        }

        public override bool HasPreviewGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            var textureProp = serializedObject.FindProperty("bakedTexture");
            var texture = textureProp.objectReferenceValue as Texture2D;
            return texture;
        }

        public override GUIContent GetPreviewTitle()
        {
            return previewTitle;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            serializedObject.UpdateIfRequiredOrScript();
            var textureProp = serializedObject.FindProperty("bakedTexture");
            var texture = textureProp.objectReferenceValue as Texture2D;

            if (!texture) return;

            GUI.DrawTexture(r, texture);
        }

        private void Bake(BakeSettings settings)
        {
            //Find existing texture
            serializedObject.UpdateIfRequiredOrScript();
            var curvesProp = serializedObject.FindProperty("curves");
            var textureProp = serializedObject.FindProperty("bakedTexture");

            var texture = textureProp.objectReferenceValue as Texture2D;

            //Calculate size
            var textureSize = GetTextureSize(curvesProp, settings);
            int textureWidth = textureSize.x;
            int textureHeight = textureSize.y;
            
            TextureFormat format = TextureFormat.ARGB32;
            switch(settings.format)
            {
                case CurveTextureFormat.ARGB32:
                    format = TextureFormat.ARGB32;
                    break;
                case CurveTextureFormat.RGBAFloat:
                    format = TextureFormat.RGBAFloat;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //Create texture asset if needed
            if (!texture || format != texture.format)
            {
                if (texture)
                {
                    bool sure = EditorUtility.DisplayDialog("Texture format changed",
                        "Texture format is going to be changed. If we do that, existing baked texture is destroyed and references turn to missing. Are you sure?",
                        "OK", "Cancel");
                    if (!sure) return;
                    ClearTexture();
                }

                var t = new Texture2D(textureWidth, textureWidth, format, false)
                {
                    name = $"{target.name} Texture"
                };
                AssetDatabase.AddObjectToAsset(t, target);

                textureProp.objectReferenceValue = t;
                texture = t;
                serializedObject.ApplyModifiedProperties();
            }

            Bake(curvesProp, texture, settings);
            
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
        }
        
        
        private void ClearTexture()
        {
            serializedObject.UpdateIfRequiredOrScript();
            var textureProp = serializedObject.FindProperty("bakedTexture");
            var texture = textureProp.objectReferenceValue as Texture2D;
            
            if (texture)
            {
                DestroyImmediate(texture, true);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
            }

            textureProp.objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();

        }

        private static Vector2Int GetTextureSize(SerializedProperty curves, BakeSettings settings)
        {
            // Resolution
            int resolution = settings.resolution;
            
            if(!curves.isArray) throw new ArgumentException();

            int curveCount = curves.arraySize;
            
            if(curveCount <= 0) throw new ArgumentException();

            //4 channel per pixel
            int requiredPixelsPerCurve = resolution / 4;
            int requiredPixels = requiredPixelsPerCurve * curveCount;

            float minSize = Mathf.Sqrt(requiredPixels);
            int size = (int)Mathf.Pow(2, Mathf.CeilToInt(Mathf.Log(minSize, 2f)));
            
            return new Vector2Int(size, size);
        }

        private static void Bake(SerializedProperty curves, Texture2D target, BakeSettings settings)
        {
            var size = GetTextureSize(curves, settings);
            int width = size.x;
            int height = size.y;

            //Resize texture if needed
            if (width != target.width || height != target.height)
            {
                target.Resize(size.x, size.y);
                
            }
            
            if(!curves.isArray) throw new ArgumentException();

            int curveCount = curves.arraySize;
            
            if(curveCount <= 0) throw new ArgumentException();

            //Bake
            int resolution = settings.resolution;
            int basePixelIndex = 0;
            Color[] pixelBuffer = new Color[width * height];
            for (int c = 0; c < curveCount; c++)
            {
                var curveProp = curves.GetArrayElementAtIndex(c);
                var curve = curveProp.animationCurveValue;

                Color tempPixelColor = default;
                for (int i = 0; i < resolution; i++)
                {
                    float t = (float)i / (resolution - 1);
                    float v = curve.Evaluate(t);

                    int pixelIndex = basePixelIndex + i / 4;
                    int channel = i % 4;
                    
                    var p = pixelBuffer[pixelIndex];
                    switch (channel)
                    {
                        case 0:
                            tempPixelColor.r = v;
                            break;
                        case 1:
                            tempPixelColor.g = v;
                            break;
                        case 2:
                            tempPixelColor.b = v;
                            break;
                        case 3:
                            tempPixelColor.a = v;
                            pixelBuffer[pixelIndex] = tempPixelColor;
                            break;
                    }
                }

                int lastPixelIndex = basePixelIndex + (resolution - 1) / 4;
                pixelBuffer[lastPixelIndex] = tempPixelColor;

                basePixelIndex = lastPixelIndex + 1;

            }
            
            target.SetPixels(pixelBuffer);
            target.Apply();
        }
    }
}