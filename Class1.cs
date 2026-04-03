using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace LethalCompanyModTemplate
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class FarmHighlight : BaseUnityPlugin
    {
        public const string modGUID = "markviews.farmHighlight";
        public const string modName = "FarmHighlight";
        public const string modVersion = "1.0.0";

        private Harmony harmony;
        public static ManualLogSource log;
        public static Dictionary<string, Material> colors = new Dictionary<string, Material>();

        void Awake()
        {
            log = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            log.LogMessage(modGUID + " has loaded succesfully.");

            harmony = new Harmony(modGUID);
            harmony.PatchAll();

            BuiltinFunctions.Methods.Add("highlight", new PyFunction("highlight", new Func<List<IPyObject>, Simulation, Execution, int, double>(Highlight), null, false));
            Farm.allKeyWords.Add("highlight");
            Farm.startUnlocks.Add("highlight");

            GameObject obj = new GameObject(modName);
            obj.AddComponent<HighlightClass>();
            DontDestroyOnLoad(obj);

            initColors();
        }

        // TODO add more colors
        private void initColors()
        {
            AddColor("red", new Color(1f, 0f, 0f, 0.3f));
            AddColor("green", new Color(0f, 1f, 0f, 0.3f));
            AddColor("blue", new Color(0f, 0f, 1f, 0.3f));
            AddColor("orange", new Color(1f, 0.5f, 0f, 0.3f));
            AddColor("yellow", new Color(1f, 1f, 0f, 0.3f));
            AddColor("pink", new Color(1f, 0f, 1f, 0.3f));
            AddColor("white", new Color(1f, 1f, 1f, 0.3f));
            AddColor("black", new Color(0f, 0f, 0f, 0.3f));
            AddColor("gray", new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        private void AddColor(string name, Color color)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            colors.Add(name, mat);
        }

        private static double Highlight(List<IPyObject> parameters, Simulation sim, Execution exec, int droneId)
        {

            Drone drone = sim.farm.drones[droneId];
            Material mat = colors["green"];
            float seconds = -1;

            if (parameters.Count > 0)
            {
                string arg1 = CodeUtilities.ToNiceString(parameters[0], 0, null, false).ToLower();

                if (arg1 == "none")
                {
                    if (HighlightClass.highlights.ContainsKey(drone.pos))
                    {
                        HighlightClass.highlights.Remove(drone.pos);
                    }
                    return 0.0;
                }

                if (colors.ContainsKey(arg1))
                {
                    mat = colors[arg1];
                }

                // TODO throw correct error (invalid color)
                else
                {
                    throw new ExecuteException("error_empty_print", -1, -1);
                }

            }
            if (parameters.Count > 1)
            {
                string arg2 = CodeUtilities.ToNiceString(parameters[1], 0, null, false);
                if (float.TryParse(arg2, out float outSeconds))
                {
                    seconds = outSeconds;
                }

                // TODO throw correct error (invalid number)
                else
                {
                    throw new ExecuteException("error_empty_print", -1, -1);
                }
            }

            HighlightClass.SpawnHighlight(drone.pos, mat, seconds);
            return 0.0;
        }

    }

}

public class HighlightClass : MonoBehaviour
{

    public static Mesh mesh = null;
    public static Dictionary<Vector2Int, Highlight> highlights = new Dictionary<Vector2Int, Highlight>();
    public static List<Highlight> toDelete = new List<Highlight>();

    public static async void SpawnHighlight(Vector2Int pos, Material mat, float seconds)
    {
        // go to main thread
        await Awaitable.MainThreadAsync();

        if (mesh == null)
        {
            FarmRenderer farmRend = UnityEngine.Object.FindFirstObjectByType<FarmRenderer>();
            Type type = typeof(FarmRenderer);
            FieldInfo _hoverMeshField = type.GetField("hoverMesh", BindingFlags.Instance | BindingFlags.NonPublic);
            mesh = _hoverMeshField.GetValue(farmRend) as Mesh;
        }

        highlights.Add(pos, new Highlight(pos, mat, seconds));
    }

    public void Update()
    {

        foreach (var item in highlights)
        {
            Highlight highlight = item.Value;
            if (highlight.deleteTime != -1 && Time.time >= highlight.deleteTime)
            {
                toDelete.Add(highlight);
                continue;
            }

            Matrix4x4 matrix = MainSim.Inst.sceneScaler.localToWorldMatrix * Matrix4x4.Translate(new Vector3(-highlight.pos.x, highlight.pos.y, 0.1f));
            Graphics.DrawMesh(mesh, matrix, highlight.material, 0);
        }

        foreach (Highlight highlight in toDelete)
        {
            // verify this is the same highlight (not replaced yet)
            if (highlight == highlights[highlight.pos])
            {
                highlights.Remove(highlight.pos);
            }
        }
        toDelete.Clear();

    }

}

public class Highlight
{

    public Vector2Int pos;
    public Material material;
    public float deleteTime;

    public Highlight(Vector2Int pos, Material material, float duration)
    {
        this.pos = pos;
        this.material = material;

        if (duration == -1)
        {
            this.deleteTime = -1;
        }
        else
        {
            this.deleteTime = Time.time + duration;
        }

    }

}