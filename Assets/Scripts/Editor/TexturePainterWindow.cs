using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yucatan.Painting;

namespace Yucatan.Editor
{
    public class TexturePainterWindow : EditorWindow
    {
        private Color brushColour = Color.black;
        private float brushSize = 5;

        private const int MAX_BRUSH_SIZE = 100;

        private int prevControl;
        private int controlId;

        private bool Painting = false;

        private int brushType = 0;
        private Transform projector;
        private MeshCollider canvas;

        private Vector2 mousePos;
        private SceneView sceneView;

        private bool pKeyDown = false;

        void OnEnable()
        {
            SceneView.onSceneGUIDelegate += SceneGUI;
        }

        void OnDisable()
        {
            SceneView.onSceneGUIDelegate -= SceneGUI;
        }

        Texture2D GetTextureForRenderer(MeshRenderer meshRenderer)
        {
            Texture2D texture = meshRenderer.sharedMaterial.mainTexture as Texture2D;
            if (texture == null)
            {
                List<Texture2D> allTextures = new List<Texture2D>();
                Shader shader = meshRenderer.sharedMaterial.shader;
                for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        Texture _tex = meshRenderer.sharedMaterial.GetTexture(ShaderUtil.GetPropertyName(shader, i));
                        Texture2D _tex2d = _tex as Texture2D;
                        if (_tex2d != null)
                        {
                            allTextures.Add(_tex2d);
                        }
                    }
                }
                if (allTextures.Count > 0)
                {
                    texture = allTextures[0];
                }
            }
            return texture;
        }

        private bool DoPaint(bool click)
        {
            if (!Painting) { return false; }

            Event e = Event.current;

            GameObject paintOn = null;
            if (canvas != null)
            {
                paintOn = canvas.gameObject;
            }
            else
            {
                paintOn = Selection.activeGameObject;
            }

            if (paintOn == null) { return false; }

            MeshRenderer meshRenderer = paintOn.GetComponent<MeshRenderer>();
            MeshFilter meshFilter = paintOn.GetComponent<MeshFilter>();
            MeshCollider meshCollider = paintOn.GetComponent<MeshCollider>();

            if (meshRenderer == null || meshFilter == null || meshCollider == null) { return false; }

            Texture2D texture = GetTextureForRenderer(meshRenderer);
            if (texture == null) { return false; }
            
            IBrush brush = new ScreenCircleBrush(brushColour, brushSize*sceneView.camera.pixelWidth, sceneView.camera, mousePos);
            if (brushType == 1)
            {
                brush = new ScreenRectangleBrush(brushColour, new Vector2(brushSize * sceneView.camera.pixelWidth, brushSize * sceneView.camera.pixelWidth), sceneView.camera, mousePos);
            }
            else if (brushType == 2)
            {
                brush = new ScreenRectangleBrush(brushColour, new Vector2((brushSize * sceneView.camera.pixelWidth) * 1.5f, (brushSize * sceneView.camera.pixelWidth) * 0.75f), sceneView.camera, mousePos);
            }
            else if (brushType == 3)
            {
                if (click) { return false; }
                brush = new ProjectorRectangleBrush(brushColour, new Vector2(brushSize * 1.5f, brushSize * 0.75f), projector);
            }

            if (Painter.Paint(brush, texture, meshCollider))
            {
                return true;
            }
            return false;
        }

        void Update()
        {
            if (projector != null && canvas != null && pKeyDown) { DoPaint(false); }
        }

        
        private void SceneGUI(SceneView sceneView)
        {
            if (!Painting) return;

            Event e = Event.current;
            controlId = GUIUtility.GetControlID("PainterTool".GetHashCode(), FocusType.Passive);
            if (e.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                GUIUtility.hotControl = 0;
            }
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                mousePos = e.mousePosition;
                mousePos.y = sceneView.position.height - mousePos.y - 16; //?? I think there is a 16px tool bar or something which we need to account for
                this.sceneView = sceneView;
            }
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
            {
                if (DoPaint(true))
                {
                    // get the sceneview focus
                    if (e.type == EventType.MouseDown)
                    {
                        GUIUtility.hotControl = controlId;
                    }
                }
            }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.P)
            {
                pKeyDown = true;
            }
            if (e.type == EventType.KeyUp && e.keyCode == KeyCode.P)
            {
                pKeyDown = false;
            }
        }
        

        [MenuItem("Window/Texture Painter")]
        static void ShowWindow()
        {
            GetWindow<TexturePainterWindow>("Texture Painter");
        }

        void OnGUI()
        {
            GUILayout.Label("Paint me baby", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Size");
            brushSize = GUILayout.HorizontalSlider(brushSize, 0, MAX_BRUSH_SIZE);
            GUILayout.EndHorizontal();
            brushColour = EditorGUILayout.ColorField("Color", brushColour);
            brushType = EditorGUILayout.Popup("Brush Type", brushType, new string[] { "Circle", "Square", "Rectangle", "Projected Rectangle"});

            canvas = (MeshCollider)EditorGUILayout.ObjectField("Canvas", canvas, typeof(MeshCollider), true);
            projector = (Transform)EditorGUILayout.ObjectField("Projector", projector, typeof(Transform), true);

            Painting = GUILayout.Toggle(Painting, "Paint", "Button");

            if  (GUILayout.Button("Save Texture"))
            {
                Texture2D paintedTexture = GetTextureForRenderer(canvas.GetComponent<MeshRenderer>());
                byte[] pngData = paintedTexture.EncodeToPNG();
                string path = AssetDatabase.GetAssetPath(paintedTexture);
                System.IO.File.WriteAllBytes(path, pngData);
            }
        }
    }
}
