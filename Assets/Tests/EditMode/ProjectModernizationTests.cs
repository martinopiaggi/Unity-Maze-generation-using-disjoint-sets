using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DisjointSetMaze.Tests
{
    public sealed class ProjectModernizationTests
    {
        private const string PipelinePath = "Assets/Settings/Disjoint Set Maze URP.asset";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [Test]
        public void ProjectTargetsUnitySixThreeLts()
        {
            Assert.That(Application.unityVersion, Does.StartWith("6000.3."));
        }

        [Test]
        public void ProjectUsesOnlyTheNewInputSystem()
        {
            var settings = File.ReadAllText("ProjectSettings/ProjectSettings.asset");

            StringAssert.Contains("activeInputHandler: 1", settings);
        }

        [Test]
        public void UrpIsAssignedGlobally()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);

            Assert.That(pipeline, Is.Not.Null);
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.SameAs(pipeline));
            Assert.That(pipeline.useSRPBatcher, Is.True);
            Assert.That(pipeline.gpuResidentDrawerMode, Is.EqualTo(GPUResidentDrawerMode.InstancedDrawing));
            Assert.That(EditorGraphicsSettings.batchRendererGroupShaderStrippingMode,
                Is.EqualTo(BatchRendererGroupStrippingMode.KeepAll));
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
                "Assets/Settings/Disjoint Set Maze Renderer.asset");
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.renderingMode, Is.EqualTo(RenderingMode.ForwardPlus));
        }

        [Test]
        public void SampleSceneHasNoMissingScripts()
        {
            var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    {
                        Assert.That(
                            GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject),
                            Is.Zero,
                            $"Missing script on {transform.name}.");
                    }
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
