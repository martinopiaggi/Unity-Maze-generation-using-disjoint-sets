using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DisjointSetMaze.Editor
{
    public static class ProjectModernizer
    {
        private const string SettingsFolder = "Assets/Settings";
        private const string MaterialsFolder = "Assets/Materials";
        private const string RendererPath = SettingsFolder + "/Disjoint Set Maze Renderer.asset";
        private const string PipelinePath = SettingsFolder + "/Disjoint Set Maze URP.asset";
        private const string LegacyPathMarkerMaterialPath = "Assets/New Material 1.mat";
        private const string PathMarkerMaterialPath = MaterialsFolder + "/Path Marker.mat";
        private const string WallMaterialPath = MaterialsFolder + "/Wall.mat";
        private const string WallPrefabPath = "Assets/Wall.prefab";
        private const string PathMarkerPrefabPath = "Assets/Cube.prefab";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/Disjoint Set Maze/Apply Modern Project Settings")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play mode before applying project settings.");
            }

            EnsureFolder(SettingsFolder);
            EnsureFolder(MaterialsFolder);

            var pipeline = ConfigureRenderPipeline();
            ConfigureProjectSettings(pipeline);
            ConfigureBuildSettings();

            var wallMaterial = ConfigureWallMaterial();
            var pathMarkerMaterial = ConfigurePathMarkerMaterial();
            ConfigurePrefab(WallPrefabPath, "Wall", wallMaterial, true);
            ConfigurePrefab(PathMarkerPrefabPath, "Path Marker", pathMarkerMaterial, false);
            ConfigureScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validate();
            Debug.Log("Disjoint Set Maze modernization completed successfully.");
        }

        [MenuItem("Tools/Disjoint Set Maze/Validate Modern Project")]
        public static void Validate()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                throw new InvalidOperationException("The URP asset is missing.");
            }

            if (GraphicsSettings.defaultRenderPipeline != pipeline)
            {
                throw new InvalidOperationException("The URP asset is not assigned in Graphics Settings.");
            }

            if (EditorGraphicsSettings.batchRendererGroupShaderStrippingMode != BatchRendererGroupStrippingMode.KeepAll)
            {
                throw new InvalidOperationException("GPU Resident Drawer requires BatchRendererGroup variants set to Keep All.");
            }

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null || renderer.renderingMode != RenderingMode.ForwardPlus)
            {
                throw new InvalidOperationException("The Forward+ renderer is missing or misconfigured.");
            }

            if (!pipeline.useSRPBatcher || pipeline.gpuResidentDrawerMode != GPUResidentDrawerMode.InstancedDrawing)
            {
                throw new InvalidOperationException("SRP Batcher and GPU Resident Drawer must be enabled.");
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException("The URP Lit shader is unavailable.");
            }

            ValidateMaterial(WallMaterialPath, shader);
            ValidateMaterial(PathMarkerMaterialPath, shader);
            ValidatePrefab(WallPrefabPath);
            ValidatePrefab(PathMarkerPrefabPath);

            if (Array.FindIndex(
                    EditorBuildSettings.scenes,
                    scene => scene.enabled && scene.path == ScenePath) < 0)
            {
                throw new InvalidOperationException($"{ScenePath} is not enabled in Build Settings.");
            }

            var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var gameObject in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject.gameObject) > 0)
                        {
                            throw new InvalidOperationException($"Missing script on {gameObject.name} in {ScenePath}.");
                        }
                    }
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            Debug.Log("Disjoint Set Maze project validation passed.");
        }

        private static UniversalRenderPipelineAsset ConfigureRenderPipeline()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.name = "Disjoint Set Maze Renderer";
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            rendererData.renderingMode = RenderingMode.ForwardPlus;
            rendererData.depthPrimingMode = DepthPrimingMode.Auto;
            EditorUtility.SetDirty(rendererData);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                pipeline.name = "Disjoint Set Maze URP";
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            pipeline.renderScale = 1f;
            pipeline.shadowDistance = 100f;
            pipeline.useSRPBatcher = true;
            pipeline.supportsDynamicBatching = false;
            pipeline.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
            pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras = true;
            SetSerializedInt(pipeline, "m_MainLightShadowsSupported", 1);
            SetSerializedInt(pipeline, "m_AdditionalLightShadowsSupported", 0);
            SetSerializedObjectReference(pipeline, "m_RendererDataList", rendererData);
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            var currentQuality = QualitySettings.GetQualityLevel();
            var qualityNames = QualitySettings.names;
            for (var qualityIndex = 0; qualityIndex < qualityNames.Length; qualityIndex++)
            {
                QualitySettings.SetQualityLevel(qualityIndex, false);
                QualitySettings.renderPipeline = pipeline;
                QualitySettings.vSyncCount = 1;
                QualitySettings.antiAliasing = 0;
            }

            QualitySettings.SetQualityLevel(currentQuality, false);
            return pipeline;
        }

        private static void ConfigureProjectSettings(UniversalRenderPipelineAsset pipeline)
        {
            PlayerSettings.productName = "Disjoint Set Maze";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.gcIncremental = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.usePlayerLog = true;
            SetBatchRendererGroupVariants(BatchRendererGroupStrippingMode.KeepAll);
            PlayerSettings.SetStaticBatchingForPlatform(BuildTarget.StandaloneWindows64, false);

            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSettings.externalVersionControl = "Visible Meta Files";
            EditorSettings.lineEndingsForNewScripts = LineEndingsMode.Unix;
            EditorSettings.projectGenerationRootNamespace = "DisjointSetMaze";

            if (GraphicsSettings.defaultRenderPipeline != pipeline)
            {
                throw new InvalidOperationException("Failed to assign the URP asset.");
            }
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static Material ConfigureWallMaterial()
        {
            var shader = RequireUrpLitShader();
            var material = AssetDatabase.LoadAssetAtPath<Material>(WallMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Wall" };
                AssetDatabase.CreateAsset(material, WallMaterialPath);
            }

            ConfigureLitMaterial(material, new Color(0.24f, 0.27f, 0.31f, 1f), 0.05f, 0.28f);
            return material;
        }

        private static Material ConfigurePathMarkerMaterial()
        {
            var shader = RequireUrpLitShader();
            var material = AssetDatabase.LoadAssetAtPath<Material>(PathMarkerMaterialPath);
            if (material == null)
            {
                var legacy = AssetDatabase.LoadAssetAtPath<Material>(LegacyPathMarkerMaterialPath);
                if (legacy != null)
                {
                    var error = AssetDatabase.MoveAsset(LegacyPathMarkerMaterialPath, PathMarkerMaterialPath);
                    if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
                    material = AssetDatabase.LoadAssetAtPath<Material>(PathMarkerMaterialPath);
                }
            }

            if (material == null)
            {
                material = new Material(shader) { name = "Path Marker" };
                AssetDatabase.CreateAsset(material, PathMarkerMaterialPath);
            }

            ConfigureLitMaterial(material, new Color(0.02f, 0.72f, 0.42f, 1f), 0f, 0.18f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.01f, 0.5f, 0.22f, 1f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureLitMaterial(Material material, Color color, float metallic, float smoothness)
        {
            material.shader = RequireUrpLitShader();
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
        }

        private static Shader RequireUrpLitShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? throw new InvalidOperationException("URP Lit shader was not found.");
        }

        private static void ConfigurePrefab(string path, string name, Material material, bool isWall)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.name = name;
                root.transform.localPosition = Vector3.zero;
                root.transform.localRotation = Quaternion.identity;

                var renderer = root.GetComponentInChildren<MeshRenderer>(true)
                    ?? throw new InvalidOperationException($"No MeshRenderer found in {path}.");
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = isWall ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = isWall;
                renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

                if (isWall)
                {
                    GameObjectUtility.SetStaticEditorFlags(
                        root,
                        StaticEditorFlags.OccluderStatic |
                        StaticEditorFlags.OccludeeStatic);
                }
                else
                {
                    var collider = root.GetComponent<BoxCollider>();
                    if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
                    GameObjectUtility.SetStaticEditorFlags(root, 0);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = null;
            Light directionalLight = null;
            MazeGenerator generator = null;

            foreach (var root in scene.GetRootGameObjects())
            {
                camera ??= root.GetComponentInChildren<Camera>(true);
                directionalLight ??= root.GetComponentInChildren<Light>(true);
                generator ??= root.GetComponentInChildren<MazeGenerator>(true);
            }

            if (camera == null || directionalLight == null || generator == null)
            {
                throw new InvalidOperationException("SampleScene is missing its camera, light, or maze generator.");
            }

            generator.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.05f, 1f);
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            var flyCamera = camera.GetComponent<FlyCamera>();
            if (flyCamera != null)
            {
                var serializedCamera = new SerializedObject(flyCamera);
                serializedCamera.FindProperty("_lookSensitivity").floatValue = 0.08f;
                serializedCamera.ApplyModifiedPropertiesWithoutUndo();
            }

            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 1.2f;
            directionalLight.shadows = LightShadows.Soft;
            directionalLight.lightmapBakeType = LightmapBakeType.Realtime;

            RenderSettings.sun = directionalLight;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.28f, 0.34f, 0.42f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.16f, 0.19f, 0.23f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
            RenderSettings.ambientIntensity = 1f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ValidateMaterial(string path, Shader expectedShader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader != expectedShader || !material.enableInstancing)
            {
                throw new InvalidOperationException($"Material validation failed for {path}.");
            }
        }

        private static void ValidatePrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
                    {
                        throw new InvalidOperationException($"Missing script in {path}.");
                    }
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetBatchRendererGroupVariants(BatchRendererGroupStrippingMode mode)
        {
            var graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsSettings == null || graphicsSettings.Length == 0)
            {
                throw new InvalidOperationException("GraphicsSettings.asset could not be loaded.");
            }

            var serialized = new SerializedObject(graphicsSettings[0]);
            var property = serialized.FindProperty("m_BrgStripping")
                ?? serialized.FindProperty("m_BatchRendererGroupShaderStrippingMode");
            if (property == null)
            {
                throw new InvalidOperationException("Graphics Settings has no BatchRendererGroup stripping field.");
            }

            property.intValue = (int)mode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedInt(UnityEngine.Object target, string propertyName, int value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Missing serialized property {propertyName} on {target.name}.");
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName)
                ?? throw new InvalidOperationException($"Missing serialized property {propertyName} on {target.name}.");
            if (property.isArray)
            {
                property.arraySize = 1;
                property.GetArrayElementAtIndex(0).objectReferenceValue = value;
            }
            else
            {
                property.objectReferenceValue = value;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }
    }
}
