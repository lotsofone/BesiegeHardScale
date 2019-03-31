using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;
using Modding.Blocks;
using UnityEngine.SceneManagement;

namespace HardScale
{
    class ScaleUI : SafeUIBehaviour
    {
        public bool showGUI { get; private set; } = false;
        private bool multiScale = false;
        private ModKey copyKey;
        private ModKey pasteKey;

        protected override void Awake()
        {
            base.Awake();
            this.copyKey = ModKeys.GetKey("lto_hardscale_copy");
            this.pasteKey = ModKeys.GetKey("lto_hardscale_paste");
        }

        public override string InitialWindowName()
        {
            return "HardScale";
        }

        public override Rect InitialWindowRect()
        {
            return new Rect(Screen.width - 300 - 200, Screen.height - 160 - 200, 300, 160);
        }

        public override bool ShouldShowGUI()
        {
            List<string> scene = new List<string> { "INITIALISER", "TITLE SCREEN", "LevelSelect", "LevelSelect1", "LevelSelect2", "LevelSelect3" };

            if (SceneManager.GetActiveScene().isLoaded)
            {

                if (scene.Exists(match => match == SceneManager.GetActiveScene().name))
                {
                    return false;
                }
            }

            return !StatMaster.levelSimulating && !StatMaster.inMenu && !StatMaster.isMainMenu &&
                this.showGUI;
        }

        int lastSelectionCount = 0;
        void Update()
        {
            if (lastSelectionCount != AdvancedBlockEditor.Instance.selectionController.Selection.Count)
            {
                lastSelectionCount = AdvancedBlockEditor.Instance.selectionController.Selection.Count;
                OnSelectionCountChange(lastSelectionCount);
            }
        }

        protected void OnSelectionCountChange(int count)
        {
            showGUI = (count > 0);
            if (!showGUI)
            {
                lastMouseDown = false;
                UICought = false;
            }

            if (count > 1)
            {
                multiScale = true;
                aScale = 1;
            }
            else if (count == 1)
            {
                multiScale = false;
            }
        }

        List<ISelectable> originalSelection = new List<ISelectable>();
        List<Vector3> originalPositions = new List<Vector3>();
        List<Vector3> originalScales = new List<Vector3>();

        void InitSingleScale()
        {
            var bb = (BlockBehaviour)AdvancedBlockEditor.Instance.selectionController.Selection[0];
            singleScale = bb.transform.localScale;
            maxSingle = singleScale * 3;
            minSingle = singleScale * 0.1f;
        }

        float lastSnap = 0.05f;
        string snap = "0.05";

        float aScale = 1;
        string aScaleText = "1";
        Vector3 singleScale, maxSingle, minSingle;
        string[] clipBoard = new string[] { "1", "1", "1" };

        protected bool lastMouseDown = false;
        protected override void WindowContent(int windowID)
        {
            if (lastMouseDown != Input.GetMouseButton(0))
            {
                lastMouseDown = Input.GetMouseButton(0);
                if (lastMouseDown) MouseDown();
                else MouseUp();
            }
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("snap to");
            snap = GUILayout.TextField(snap, GUILayout.Width(100));
            try
            {
                float newSnap = Convert.ToSingle(snap);
                if (newSnap < 0) newSnap = 0; lastSnap = newSnap;
            }
            catch (Exception) { }
            GUILayout.Label(lastSnap.ToString(), GUILayout.Width(100));
            GUILayout.EndHorizontal();
            if (multiScale)
            {
                GUILayout.Label("scale by slider(0.5-2)");
                aScale = GUILayout.HorizontalSlider(aScale, 0.5f, 2f, GUILayout.Width(300));
                GUILayout.Label("scale by slider(0.1-10)");
                aScale = GUILayout.HorizontalSlider(aScale, 0.1f, 10f, GUILayout.Width(300));
                if (UICought && lastSnap > 0.0001f)
                {
                    aScale = 1 + lastSnap * (Mathf.Round((aScale - 1) / lastSnap));
                }
                if (UICought && originalScales.Count > 0)
                    DoMultiScale(aScale);
                GUILayout.Label("scale by value");
                GUILayout.BeginHorizontal();
                aScaleText = GUILayout.TextField(aScaleText);
                if (GUILayout.Button("Execute"))
                {
                    try
                    {
                        float multiplier = Convert.ToSingle(aScaleText);
                        LogPreState();
                        aScale = multiplier;
                        DoMultiScale(aScale);
                        UpdatePostState();
                    }
                    catch (Exception) { }
                }
                GUILayout.EndHorizontal();
            }
            else//single scale
            {
                if (AdvancedBlockEditor.Instance.selectionController.Selection.Count > 0)
                {
                    var bb = (BlockBehaviour)AdvancedBlockEditor.Instance.selectionController.Selection[0];
                    singleScale = bb.transform.localScale;
                }

                if (!lastMouseDown)
                {
                    minSingle = singleScale * 0.1f;
                    maxSingle = singleScale * 3;
                    for (int i = 0; i < 3; i++)
                    {
                        if (maxSingle[i] < 0.1f) maxSingle[i] = 0.1f;
                    }
                }
                singleScale[0] = SingleScaleSlider(singleScale[0], minSingle[0], maxSingle[0], "x");
                singleScale[1] = SingleScaleSlider(singleScale[1], minSingle[1], maxSingle[1], "y");
                singleScale[2] = SingleScaleSlider(singleScale[2], minSingle[2], maxSingle[2], "z");
                if (UICought && lastSnap > 0.0001f && originalScales.Count == 1)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        singleScale[i] = originalScales[0][i] + lastSnap * (Mathf.Round((singleScale[i] - originalScales[0][i]) / lastSnap));
                        if (singleScale[i] < 0) singleScale[i] = 0;
                    }
                }
                if(StatMaster.Mode.selectedTool != StatMaster.Tool.Modify)
                    GUILayout.Label("paste bin: (ctrl+C/ctrl+V)");
                else
                    GUILayout.Label("paste bin: (hotkey disabled)");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("copy", GUILayout.Width(50)) || (copyKey.IsPressed && StatMaster.Mode.selectedTool != StatMaster.Tool.Modify))
                {
                    for (int i = 0; i < 3; i++) clipBoard[i] = singleScale[i].ToString();
                }
                if (GUILayout.Button("paste", GUILayout.Width(50)) || (pasteKey.IsPressed && StatMaster.Mode.selectedTool != StatMaster.Tool.Modify))
                {
                    try
                    {
                        LogPreState();
                        for (int i = 0; i < 3; i++) singleScale[i] = Convert.ToSingle(clipBoard[i]);
                        DoSingleScale(singleScale);
                        UpdatePostState();
                    }
                    catch (Exception) { };
                }
                for (int i = 0; i < 3; i++) clipBoard[i] = GUILayout.TextField(clipBoard[i]);
                GUILayout.EndHorizontal();
                if (UICought)
                    DoSingleScale(singleScale);
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }
        bool UICought = false;
        void MouseDown()//从鼠标按下开始记录
        {
            if (!ShouldShowGUI()) return;
            if (!this.windowRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y))) return;

            UICought = true;
            LogPreState();
        }
        void LogPreState()
        {
            originalSelection.Clear();
            originalPositions.Clear();
            originalScales.Clear();
            foreach (var islc in AdvancedBlockEditor.Instance.selectionController.Selection)
            {
                var bb = (BlockBehaviour)islc;
                originalSelection.Add(islc);
                originalPositions.Add(bb.Position);
                originalScales.Add(bb.Scale);
            }
            aScale = 1;
        }
        void MouseUp()
        {
            if (!UICought) return;
            UpdatePostState();
            UICought = false;
        }
        void UpdatePostState()
        {
            bool addUndo = true;
            if (originalScales.Count == 1)
            {
                var bb = originalSelection[0] as BlockBehaviour;
                if (originalScales[0] == bb.Scale)
                    addUndo = false;
            }
            else
            {
                if (aScale == 1) addUndo = false;
            }
            if (addUndo)
            {
                //ModConsole.Log("addundo");
                List<UndoAction> undoActions = new List<UndoAction>();
                for (int i = 0; i < originalSelection.Count; i++)
                {
                    var bb = originalSelection[i] as BlockBehaviour;
                    var a = new UndoActionScale(Machine.Active(), bb.Guid, bb.Position, originalPositions[i], bb.Scale, originalScales[i]);
                    undoActions.Add(a);
                }
                Machine.Active().UndoSystem.AddActions(undoActions);
            }
            originalPositions.Clear();
            originalScales.Clear();
            originalSelection.Clear();

            aScale = 1;
        }

        protected float SingleScaleSlider(float value, float min, float max, string name)
        {
            GUILayout.BeginHorizontal();
            float ret = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(230));
            GUILayout.Label(name);
            GUILayout.Label(ret.ToString(), GUILayout.Width(60));
            GUILayout.EndHorizontal();
            return ret;
        }
        public readonly static string[] killMappers = new string[]{"x-scale", "y-scale", "z-scale", "scaling" };
        void DoSingleScale(Vector3 scale)
        {
            var bb = (BlockBehaviour)AdvancedBlockEditor.Instance.selectionController.Selection[0];
            bb.SetScale(scale);
            //have tried to make compatibla with easy scale, but failed
            /*foreach (var slider in bb.Sliders)
            {
                if (slider.Key == "x-scale")
                {
                    slider.SetValue(scale[0]);
                    slider.Value = scale[0];
                    slider.ApplyValue();
                }
                else if (slider.Key == "y-scale")
                {
                    slider.SetValue(scale[1]);
                    slider.Value = scale[1];
                    slider.ApplyValue();
                }
                else if (slider.Key == "z-scale")
                {
                    slider.SetValue(scale[2]);
                    slider.Value = scale[2];
                    slider.ApplyValue();
                }
            }*/
        }

        void DoMultiScale(float multiplier)
        {
            if (multiplier < 0) return;
            //中心点位置
            Transform pivot = AdvancedBlockEditor.Instance.ToolTransform;

            for(int i=0; i<this.originalSelection.Count; i++)
            {
                BlockBehaviour bb = this.originalSelection[i] as BlockBehaviour;
                bb.SetPosition((Machine.Active().BuildingMachine.TransformPoint(originalPositions[i]) - pivot.position) * multiplier + pivot.position);
                bb.SetScale(originalScales[i] * multiplier);
                //have tried to make compatibla with easy scale, but failed
                /*foreach (var slider in bb.Sliders)
                {
                    if (slider.Key == "x-scale")
                    {
                        slider.SetValue(originalScales[i][0] * multiplier);
                        slider.Value = originalScales[i][0] * multiplier;
                        slider.ApplyValue();

                    }
                    else if (slider.Key == "y-scale")
                    {
                        slider.SetValue(originalScales[i][1] * multiplier);
                        slider.Value = originalScales[i][1] * multiplier;
                        slider.ApplyValue();
                    }
                    else if (slider.Key == "z-scale")
                    {
                        slider.SetValue(originalScales[i][2] * multiplier);
                        slider.Value = originalScales[i][2] * multiplier;
                        slider.ApplyValue();
                    }
                }*/
            }
        }
    }
}
