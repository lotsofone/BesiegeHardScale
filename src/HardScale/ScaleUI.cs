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

        public override string InitialWindowName()
        {
            return "HardScale";
        }

        public override Rect InitialWindowRect()
        {
            return new Rect(Screen.width - 300-200, Screen.height - 160-200, 300, 160);
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

        float aScale = 1;
        string aScaleText = "1";
        Vector3 singleScale, maxSingle, minSingle;
        string[] clipBoard = new string[]{"1", "1", "1"};

        protected bool lastMouseDown = false;
        protected override void WindowContent(int windowID)
        {
            if (lastMouseDown!=Input.GetMouseButton(0))
            {
                lastMouseDown = Input.GetMouseButton(0);
                if (lastMouseDown) MouseDown();
                else MouseUp();
            }
            GUILayout.BeginVertical();
            if (multiScale)
            {
                GUILayout.Label("scale by slider(0.5-2)");
                aScale = GUILayout.HorizontalSlider(aScale, 0.5f, 2f, GUILayout.Width(300));
                GUILayout.Label("scale by slider(0.1-10)");
                aScale = GUILayout.HorizontalSlider(aScale, 0.1f, 10f, GUILayout.Width(300));
                if (lastMouseDown && aScale != 1)
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
                        DoMultiScale(multiplier);
                        UpdatePostState();
                    }
                    catch (Exception) { }
                }
                GUILayout.EndHorizontal();
            }
            else//single scale
            {
                var bb = (BlockBehaviour)AdvancedBlockEditor.Instance.selectionController.Selection[0];
                singleScale = bb.transform.localScale;
                if (!lastMouseDown)
                {
                    minSingle = singleScale * 0.1f; maxSingle = singleScale * 3;
                }
                singleScale[0] = SingleScaleSlider(singleScale[0], minSingle[0], maxSingle[0], "x");
                singleScale[1] = SingleScaleSlider(singleScale[1], minSingle[1], maxSingle[1], "y");
                singleScale[2] = SingleScaleSlider(singleScale[2], minSingle[2], maxSingle[2], "z");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("copy", GUILayout.Width(50)))
                {
                    for (int i = 0; i < 3; i++) clipBoard[i] = singleScale[i].ToString();
                }
                if (GUILayout.Button("paste", GUILayout.Width(50)))
                {
                    try
                    {
                        LogPreState();
                        for (int i = 0; i < 3; i++) singleScale[i] = Convert.ToSingle(clipBoard[i]);
                        UpdatePostState();
                    }
                    catch (Exception) { };
                }
                for (int i = 0; i < 3; i++) clipBoard[i] = GUILayout.TextField(clipBoard[i]);
                GUILayout.EndHorizontal();
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
            //ModConsole.Log("add undo ");
            List<UndoAction> undoActions = new List<UndoAction>();
            for(int i=0; i<originalSelection.Count; i++)
            {
                var bb = originalSelection[i] as BlockBehaviour;
                var a = new UndoActionScale(Machine.Active(), bb.Guid, bb.Position, originalPositions[i], bb.Scale, originalScales[i]);
                undoActions.Add(a);
            }

            Machine.Active().UndoSystem.AddActions(undoActions);
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

        void DoSingleScale(Vector3 scale)
        {
            var bb = (BlockBehaviour)AdvancedBlockEditor.Instance.selectionController.Selection[0];
            bb.SetScale(scale);
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
            }
        }
    }
}
