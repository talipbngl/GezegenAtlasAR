using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

namespace GezegenAtlasi.Editor
{
    [InitializeOnLoad]
    public static class ProjectBuilder
    {
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string RuntimeMaterialsFolder = "Assets/Resources/RuntimeMaterials";
        private const string LoaderType = "UnityEngine.XR.ARCore.ARCoreLoader";

        static ProjectBuilder()
        {
            EditorApplication.delayCall += AutoPrepare;
        }

        private static void AutoPrepare()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoPrepare;
                return;
            }
            EnsureRuntimeMaterials();
            if (!File.Exists(ScenePath)) Prepare();
        }

        [MenuItem("Tools/Gezegen Atlası/1 • Projeyi Hazırla")]
        public static void Prepare()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Önce Play modundan çıkın.");
            ConfigurePlayer();
            ConfigureXR();
            EnsureRuntimeMaterials();
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
            CreateScene();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Gezegen Atlası hazır. Play ile 3B önizlemeyi açın; Android modülü kuruluysa APK oluşturabilirsiniz.");
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Gezegen Atlası AR";
            PlayerSettings.companyName = "Gezegen Atlası";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.gezegenatlasi.ar");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.Android.forceSDCardPermission = false;
        }

        private static void ConfigureXR()
        {
            var arCore = ARCoreSettings.GetOrCreateSettings();
            arCore.requirement = ARCoreSettings.Requirement.Optional;
            arCore.depth = ARCoreSettings.Requirement.Optional;
            var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (general == null || general.AssignedSettings == null)
            {
                Debug.LogWarning("XR ayarı oluşturulamadı. Project Settings > XR Plug-in Management > Android > ARCore kutusunu işaretleyin.");
                return;
            }
            if (!XRPackageMetadataStore.IsLoaderAssigned(LoaderType, BuildTargetGroup.Android) &&
                !XRPackageMetadataStore.AssignLoader(general.AssignedSettings, LoaderType, BuildTargetGroup.Android))
                Debug.LogWarning("ARCore yükleyicisi otomatik atanamadı. XR Plug-in Management > Android altında ARCore'u işaretleyin.");
        }

        private static void EnsureRuntimeMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(RuntimeMaterialsFolder))
                AssetDatabase.CreateFolder("Assets/Resources", "RuntimeMaterials");

            EnsureMaterial("PlanetSurface", "Standard", false);
            EnsureMaterial("SunSurface", "Standard", true);
            EnsureMaterial("OrbitLine", "Sprites/Default", false);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureMaterial(string name, string shaderName, bool emission)
        {
            string path = RuntimeMaterialsFolder + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;

            var shader = Shader.Find(shaderName);
            if (shader == null)
                throw new InvalidOperationException("Gerekli shader bulunamadı: " + shaderName);

            var material = new Material(shader) { name = name };
            if (emission)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.white);
            }
            AssetDatabase.CreateAsset(material, path);
        }

        private static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var app = new GameObject("Uygulama").AddComponent<PlanetApp>();

            var sessionObject = new GameObject("AR Session");
            sessionObject.AddComponent<ARSession>();
            sessionObject.AddComponent<ARInputManager>();

            var rig = new GameObject("XR Origin (Mobile AR)");
            var origin = rig.AddComponent<XROrigin>();
            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(rig.transform, false);
            var arCameraObject = new GameObject("AR Camera");
            arCameraObject.tag = "MainCamera";
            arCameraObject.transform.SetParent(offset.transform, false);
            var arCamera = arCameraObject.AddComponent<Camera>();
            arCamera.clearFlags = CameraClearFlags.SolidColor;
            arCamera.backgroundColor = Color.black;
            arCamera.nearClipPlane = .01f;
            arCamera.farClipPlane = 20f;
            arCameraObject.AddComponent<ARCameraManager>();
            arCameraObject.AddComponent<ARCameraBackground>();
            var occlusion = arCameraObject.AddComponent<AROcclusionManager>();
            occlusion.requestedEnvironmentDepthMode = EnvironmentDepthMode.Fastest;
            arCameraObject.AddComponent<AudioListener>();
            var driver = arCameraObject.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new InputActionProperty(new InputAction("AR position", InputActionType.Value, "<XRHMD>/centerEyePosition"));
            driver.rotationInput = new InputActionProperty(new InputAction("AR rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation"));
            driver.trackingStateInput = new InputActionProperty(new InputAction("AR tracking", InputActionType.Value, "<XRHMD>/trackingState"));
            origin.Camera = arCamera;
            origin.CameraFloorOffsetObject = offset;

            var previewCameraObject = new GameObject("3B Preview Camera");
            var previewCamera = previewCameraObject.AddComponent<Camera>();
            previewCamera.backgroundColor = new Color(.012f, .037f, .073f);
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.fieldOfView = 52f;
            previewCamera.nearClipPlane = .01f;
            previewCamera.farClipPlane = 20f;
            previewCamera.transform.position = new Vector3(0f, 1.35f, -2.15f);
            previewCamera.transform.LookAt(new Vector3(0f, .04f, 0f));

            var fillObject = new GameObject("Yumuşak önizleme ışığı");
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = .85f;
            fill.color = new Color(.78f, .89f, 1f);
            fillObject.transform.rotation = Quaternion.Euler(45f, -20f, 0f);

            app.Configure(rig, sessionObject, arCamera, occlusion, previewCamera);
            rig.SetActive(false);
            sessionObject.SetActive(false);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        [MenuItem("Tools/Gezegen Atlası/2 • Android APK Oluştur")]
        public static void BuildAndroid()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Build Profiles > Android > Switch Platform seçin, sonra bu menüyü yeniden açın.");
            if (!File.Exists(ScenePath)) Prepare();
            EnsureRuntimeMaterials();
            ConfigureXR();
            if (!XRPackageMetadataStore.IsLoaderAssigned(LoaderType, BuildTargetGroup.Android))
                throw new InvalidOperationException("XR Plug-in Management > Android altında ARCore'u etkinleştirin.");

            string directory = Path.GetFullPath("Build");
            Directory.CreateDirectory(directory);
            string apk = Path.Combine(directory, "GezegenAtlasi-Android.apk");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apk,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("APK oluşturulamadı. Console penceresindeki ilk hatayı kontrol edin.");
            Debug.Log("APK oluşturuldu: " + apk);
        }
    }
}
