using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Modding;

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
            return new Rect(Screen.width - 300-200, Screen.height - 128-200, 300, 128);
        }

        public override bool ShouldShowGUI()
        {
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

        void ShowWindowPosition()
        {
            if (windowRect.x + windowRect.width > Screen.width)
                windowRect.x = Screen.width - windowRect.width;
            if (windowRect.y + windowRect.height > Screen.height)
                windowRect.y = Screen.height - windowRect.height;
        }

        protected void OnSelectionCountChange(int count)
        {
            if (showGUI != (count > 0))
            {
                showGUI = (count > 0);
                if (showGUI)
                {
                    ShowWindowPosition();
                }
            }

            if (count > 1)
            {
                multiScale = true;
                InitMultiScale();
            }
            else if (count == 1)
            {
                multiScale = false;
                InitSingleScale();
            }
        }

        List<ISelectable> originalSelection = new List<ISelectable>();
        List<Vector3> originalPositions = new List<Vector3>();
        List<Vector3> originalScales = new List<Vector3>();

        protected void InitMultiScale()
        {
            originalSelection.Clear();
            originalPositions.Clear();
            originalScales.Clear();
            foreach (var islc in AdvancedBlockEditor.Instance.selectionController.Selection)
            {
                var bb = (BlockBehaviour)islc;
                originalSelection.Add(islc);
                originalPositions.Add(bb.transform.position);
                originalScales.Add(bb.transform.localScale);
            }
            aScale = 1;
        }

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
        protected override void WindowContent(int windowID)
        {
            GUILayout.BeginVertical();
            if (multiScale)
            {
                GUILayout.Label("scale by slider(0.5-2)");
                aScale = GUILayout.HorizontalSlider(aScale, 0.5f, 2f, GUILayout.Width(300));
                if (aScale != 1)
                    DoMultiScale(aScale);
                GUILayout.Label("scale by slider(0.1-10)");
                aScale = GUILayout.HorizontalSlider(aScale, 0.1f, 10f, GUILayout.Width(300));
                if (aScale != 1)
                    DoMultiScale(aScale);
                if(aScale != 1 && !Input.GetMouseButton(0))
                {
                    InitMultiScale();
                }
                GUILayout.Label("scale by value");
                GUILayout.BeginHorizontal();
                aScaleText = GUILayout.TextField(aScaleText);
                if (GUILayout.Button("Execute"))
                {
                    try
                    {
                        float multiplier = Convert.ToSingle(aScaleText);
                        DoMultiScale(multiplier);
                        InitMultiScale();
                    }
                    catch (Exception) { }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                var bb = (BlockBehaviour)AdvancedBlockEditor.Instance.selectionController.Selection[0];
                singleScale = bb.transform.localScale;
                if (!Input.GetMouseButton(0))
                {
                    maxSingle = singleScale * 3;
                    minSingle = singleScale * 0.1f;
                }
                singleScale[0] = SingleScaleSlider(singleScale[0], minSingle[0], maxSingle[0], "x");
                singleScale[1] = SingleScaleSlider(singleScale[1], minSingle[1], maxSingle[1], "y");
                singleScale[2] = SingleScaleSlider(singleScale[2], minSingle[2], maxSingle[2], "z");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("copy", GUILayout.Width(40)))
                {
                    for (int i = 0; i < 3; i++) clipBoard[i] = singleScale[i].ToString();
                }
                if (GUILayout.Button("paste", GUILayout.Width(40)))
                {
                    try
                    {
                        for (int i = 0; i < 3; i++) singleScale[i] = Convert.ToSingle(clipBoard[i]);
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
                bb.SetPosition((originalPositions[i] - pivot.position) * multiplier + pivot.position);
                bb.SetScale(originalScales[i] * multiplier);
            }
        }
    }
}
