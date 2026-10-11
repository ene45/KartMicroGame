using System;
using System.IO;
using System.Linq;
using KartGame.KartSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.SceneManagement;

namespace DungeonTrack.Editor
{
    public static class DungeonTeleportSetup
    {
        const string Example = "Assets/DungeonTrack/DungeonCircuit_AnilloLava 1.unity";
        const string Request = "Assets/DungeonTrack/Editor/ConfigurarTeletransporte.request";
        const string PointName = "Punto_Reaparicion_Entrada";

        [MenuItem("Tools/Dungeon Track/Teletransporte/Configurar regreso desde el suelo central")]
        public static void ConfigureCurrent() => Configure(SceneManager.GetActiveScene());

        public static KartTeleportSurface Configure(Scene scene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Salí de Play Mode para configurar el teletransporte.");
            var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            var floor = all.Single(t => t.name == "Sala_Suelo_Libre_Central");
            var circuit = all.Single(t => t.name == "CIRCUITO_MAZMORRA_PROBUILDER");
            var roads = all.Single(t => t.name == "01_Pista_Editable_18_24_32m").GetComponentsInChildren<Collider>();
            if (floor.GetComponent<Collider>() == null) throw new InvalidOperationException("El suelo central necesita su collider.");
            var karts = all.Select(t => t.GetComponent<ArcadeKart>()).Where(k => k != null).ToArray();
            var existing = floor.GetComponent<KartTeleportSurface>();
            var player = existing != null && existing.player != null ? existing.player : karts.Single();
            var point = existing != null && existing.destination != null ? existing.destination : all.SingleOrDefault(t => t.name == PointName);
            Vector3 position = Vector3.zero, forward = Vector3.right, up = Vector3.up;
            if (point == null)
            {
                var portal = all.SingleOrDefault(t => t.name == "Entrada_Portal_Integrado");
                if (portal != null) { position = portal.position; forward = portal.forward; }
                else
                {
                    var vault = all.Single(t => t.name == "Entrada_Boveda").GetComponent<ProBuilderMesh>();
                    var points = vault.positions.Select(p => vault.transform.TransformPoint(p)).ToArray();
                    position = new Vector3((points.Min(p => p.x) + points.Max(p => p.x)) / 2,
                        points.Min(p => p.y) - 2.6f, (points.Min(p => p.z) + points.Max(p => p.z)) / 2);
                    forward = circuit.TransformDirection(Vector3.right);
                }
                position -= forward * 7;
                var ray = new Ray(position + Vector3.up * 80, Vector3.down);
                bool found = false; float highest = float.NegativeInfinity;
                foreach (var road in roads)
                    if (road.Raycast(ray, out var hit, 160) && hit.point.y > highest)
                    { found = true; highest = hit.point.y; position = hit.point; up = hit.normal; }
                if (!found) throw new InvalidOperationException("No hay pista firme delante del portal; no se creó un destino al vacío.");
                position += up * .65f;
            }
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Regreso al circuito desde el suelo central");
            if (point == null)
            {
                var empty = new GameObject(PointName); SceneManager.MoveGameObjectToScene(empty, scene);
                Undo.RegisterCreatedObjectUndo(empty, "Agregar punto de reaparición");
                empty.transform.SetParent(circuit, true);
                empty.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Vector3.ProjectOnPlane(forward, up), up));
                point = empty.transform;
            }
            var surface = existing != null ? existing : Undo.AddComponent<KartTeleportSurface>(floor.gameObject);
            Undo.RecordObject(surface, "Asignar destino y jugador");
            surface.destination = point; surface.player = player;
            PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            Undo.CollapseUndoOperations(undo); EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = point.gameObject;
            Debug.Log("DUNGEON_TELEPORT_CONFIGURED: " + scene.path + "; destino=" + point.position + "; frente=" + point.forward);
            return surface;
        }

        public static void BuildExample()
        {
            var scene = EditorSceneManager.OpenScene(Example, OpenSceneMode.Single);
            Configure(scene); EditorSceneManager.SaveScene(scene); UnityEditor.AssetDatabase.SaveAssets();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene = EditorSceneManager.OpenScene(Example, OpenSceneMode.Single);
            var surface = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<KartTeleportSurface>(true)).Single();
            if (surface.player == null || surface.destination == null || surface.GetComponent<Collider>().isTrigger)
                throw new InvalidOperationException("Referencias o collider del teletransporte incorrectos.");
            Debug.Log("DUNGEON_TELEPORT_VALIDATED: suelo sólido y destino vacío guardados con jugador asignado.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [InitializeOnLoadMethod] static void QueueInstall() => EditorApplication.update += TryInstall;
        static void TryInstall()
        {
            if (!File.Exists(Request)) { EditorApplication.update -= TryInstall; return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene(); if (scene.path != Example) return;
            EditorApplication.update -= TryInstall;
            try
            {
                Configure(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); AssetDatabase.DeleteAsset(Request);
                Debug.Log("DUNGEON_TELEPORT_INSTALLED: suelo central y punto de regreso configurados y escena guardada.");
            }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
