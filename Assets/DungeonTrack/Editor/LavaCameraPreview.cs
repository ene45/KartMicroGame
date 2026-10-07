using System.IO;
using System.Linq;
using Cinemachine;
using KartGame.KartSystems;
using MedievalCartoon.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DungeonTrack.Editor
{
    public static class LavaCameraPreview
    {
        public static void PrepareAndCapture()
        {
            LavaLandmarkBuilder.Build();
            LavaTrackTheme.CreateExample();
            Capture();
            LavaLandmarkBuilder.CaptureAndExport();
        }
        public static void RefineAndCapture()
        {
            LavaLandmarkBuilder.Build(); LavaTrackTheme.RefreshBuildExample();
            Capture(); LavaLandmarkBuilder.CaptureAndExport();
        }
        // Samples the actual virtual camera, including its composer and obstacle avoidance.
        public static void Capture()
        {
            string output = Path.GetFullPath("../../outputs"); Directory.CreateDirectory(output);
            var scene = EditorSceneManager.OpenScene(LavaTrackTheme.ScenePath);
            var all = scene.GetRootGameObjects();
            var kart = all.SelectMany(g => g.GetComponentsInChildren<ArcadeKart>(true)).First();
            var virtualCamera = all.SelectMany(g => g.GetComponentsInChildren<CinemachineVirtualCamera>(true)).First(c => c.Follow);
            var noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (noise) noise.m_AmplitudeGain = 0; // Shake disabled for still images only; scene is never saved.
            var camera = LavaLandmarkBuilder.PreviewCamera(scene, Vector3.zero, Vector3.forward, 60);
            camera.aspect = 16f / 9;
            string[] names = { "Esquina_1", "Esquina_2", "Esquina_3", "Esquina_4", "Anillo_Entrada", "Anillo_Curva_Sur" };
            Vector3[] points = { new Vector3(-104,0,51), new Vector3(70,0,80), new Vector3(107,0,-25),
                new Vector3(-40,11,-103), new Vector3(-28,0,82), new Vector3(89.2f,1.8f,-73.4f) };
            Vector3[] directions = { Vector3.forward, new Vector3(26,0,-16), Vector3.back,
                new Vector3(-1,.1f,.12f), Vector3.right, new Vector3(-22,0,-21) };
            for (int i = 0; i < names.Length; i++)
            {
                var road = all.SelectMany(g => g.GetComponentsInChildren<Transform>(true)).First(t => t.name == "01_Pista_Editable_18_24_32m").GetComponentsInChildren<Collider>();
                var position = points[i]; var up = Vector3.up;
                foreach (var surface in road)
                    if (surface.Raycast(new Ray(new Vector3(position.x, 100, position.z), Vector3.down), out var hit, 200)) { position = hit.point; up = hit.normal; break; }
                kart.transform.position = position + up * .35f;
                kart.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(directions[i], up), up);
                Physics.SyncTransforms(); virtualCamera.PreviousStateIsValid = false;
                virtualCamera.UpdateCameraState(Vector3.up, -1);
                var state = virtualCamera.State;
                camera.transform.SetPositionAndRotation(state.FinalPosition, state.FinalOrientation);
                camera.fieldOfView = state.Lens.FieldOfView;
                string path = output + "/Vista_Kart_" + names[i] + ".png";
                LavaLandmarkBuilder.Render(camera, path, 1600, 900);
                LavaLandmarkBuilder.Render(camera, path, 1600, 900);
                Debug.Log("LAVA_CAMERA_SAMPLE: " + names[i] + " position=" + camera.transform.position + " fov=" + camera.fieldOfView);
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); Debug.Log("LAVA_CAMERA_PREVIEWS_READY");
        }
    }
}
