using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UITools;
using SFS.Input;
using SFS.UI.ModGUI;

using Type = SFS.UI.ModGUI.Type;
using Object = UnityEngine.Object;
using SFS.World;

namespace ActionGroupsMod
{
    public static class GUI
    {
        static readonly int windowID = Builder.GetRandomID();
        static readonly int actionGroupsWindowID = Builder.GetRandomID();
        static readonly int actionGroupInfoWindowID = Builder.GetRandomID();

        public static Vector2Int windowSize = new Vector2Int(560, 740);
        public static Vector2Int HalfWindowSize => new Vector2Int((windowSize.x - 30) / 2, windowSize.y - 50);
        public static bool ActionGroupSelected { get; private set; } = false;

        public static GameObject windowHolder;
        static ClosableWindow window;

        static Window window_actionGroups;
        static Window window_actionGroupInfo;
    
        static List<Button> actionGroupButtons;
        static ActionGroupInfoUI actionGroupInfoUI;

        public static void CreateUI(string sceneName)
        {
            windowHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "ActionGroups - Window Holder");

            window = UIToolsBuilder.CreateClosableWindow
            (
                windowHolder.transform,
                windowID,
                windowSize.x,
                windowSize.y,
                draggable: true,
                savePosition: true,
                titleText: "Action Groups"
            );
            window.RegisterPermanentSaving(Main.main.ModNameID + "." + sceneName);
            window.CreateLayoutGroup(Type.Horizontal);
            
            window_actionGroups = Builder.CreateWindow(window, actionGroupsWindowID, HalfWindowSize.x, HalfWindowSize.y, savePosition: false);
            window_actionGroups.CreateLayoutGroup(Type.Vertical, TextAnchor.UpperCenter);
            window_actionGroups.EnableScrolling(Type.Vertical);

            window_actionGroupInfo = Builder.CreateWindow(window, actionGroupInfoWindowID, HalfWindowSize.x, HalfWindowSize.y, savePosition: false);
            window_actionGroupInfo.CreateLayoutGroup(Type.Vertical, TextAnchor.UpperCenter);
            window_actionGroupInfo.EnableScrolling(Type.Vertical);

            RedrawUI(null);
        }

        public static void DestroyWindow()
        {
            actionGroupButtons.Clear();
            actionGroupInfoUI?.DestroyUI();
            Object.Destroy(windowHolder);
        }

        public static void RedrawUI(ActionGroup selected, bool setStagingSelected = false)
        {
            actionGroupButtons?.ForEach(Destroy);

            if (selected == null)
            {
                window.Size = new Vector2Int(HalfWindowSize.x + 20, windowSize.y);
                window_actionGroupInfo.Active = false;
            }
            else
            {
                window.Size = windowSize;
                window_actionGroupInfo.Active = true;
            }

            actionGroupButtons = ActionGroupManager
                .GetCurrentActionGroups()
                .Select((ActionGroup ag) => CreateActionGroupUI(ag, ag == selected))
                .ToList();

            if (setStagingSelected)
                Patches.StagingDrawer.SetSelected(null);
        }

        static Button CreateActionGroupUI(ActionGroup ag, bool selected)
        {
            Button button = Builder.CreateButton
            (
                window_actionGroups,
                HalfWindowSize.x - 10,
                120,
                text: ag.name + "\n(" + KeybindScreen.GetDisplayName(ag.key) + ")",
                onClick: () => RedrawUI(selected ? null : ag, true)
            );
            if (selected)
            {
                button.SetSelected(true);
                actionGroupInfoUI?.DestroyUI();
                actionGroupInfoUI = new ActionGroupInfoUI(ag, window_actionGroupInfo);
            }
            return button;
        }

        public static void SetSelected(this Button button, bool selected)
        {
            AccessTools.FieldRefAccess<Button, SFS.UI.ButtonPC>(button, "_button").SetSelected(selected);
        }

        public static void Destroy(this GUIElement ui)
        {
            if (ui != null)
                Object.Destroy(ui.gameObject);
        }
    }

    public class ActionGroupInfoUI
    {
        public TextInput input_name;
        public Button button_key;

        public ActionGroupInfoUI(ActionGroup ag, Window window)
        {
            input_name = Builder.CreateTextInput
            (
                window,
                GUI.HalfWindowSize.x - 10,
                50,
                text: ag.name
            );
            AccessTools.FieldRefAccess<TextInput, TMPro.TMP_InputField>("field").Invoke(input_name).onEndEdit.AddListener
            (
                (string input) =>
                {
                    ag.name = input;
                    GUI.RedrawUI(ag);
                }
            );

            button_key = Builder.CreateButton
            (
                window,
                GUI.HalfWindowSize.x - 10,
                50,
                text: KeybindScreen.GetDisplayName(ag.key),
                onClick: () => OpenKeybindScreen(ag)
            );
        }

        public void DestroyUI()
        {
            Object.Destroy(input_name.gameObject);
            Object.Destroy(button_key.gameObject);
        }

        public void OpenKeybindScreen(ActionGroup ag)
        {
            button_key.SetSelected(true);
            KeybindScreen.Open
            (
                ag.key,
                (KeybindingsPC.Key result) =>
                {
                    ag.key = result;
                    GUI.RedrawUI(ag);
                }
            );
        }
    }

    // ? Based on `SFS.Input.KeyBinder`.
    class KeybindScreen : Screen_Base
    {
        static KeybindScreen main;
        static readonly int blurWindowID = Builder.GetRandomID();
        Window blurWindow;
        KeybindingsPC.Key currentKey;
        Action<KeybindingsPC.Key> onResult;

        public override bool PauseWhileOpen => true;

        public static void Open(KeybindingsPC.Key currentKey, Action<KeybindingsPC.Key> onResult)
        {
            if (main == null)
            {
                main = Builder.CreateHolder(Builder.SceneToAttach.BaseScene, "ActionGroups - Keybind Screen").AddComponent<KeybindScreen>();
                Vector2Int screenSize = Vector2Int.CeilToInt(UIUtility.CanvasPixelSize);
                main.blurWindow = Builder.CreateWindow(main.transform, blurWindowID, screenSize.x, screenSize.y, posY: screenSize.y / 2, savePosition: false, opacity: 0.5f);
                main.blurWindow.CreateLayoutGroup(Type.Vertical);
                Builder.CreateLabel(main.blurWindow, screenSize.x, 100, text: "Assign action group keybind...\n(Use escape to unbind)");
            }
            main.currentKey = currentKey;
            main.onResult = onResult;
            ScreenManager.main.OpenScreen(() => main);
        }

        public override void OnOpen()
        {
            blurWindow.Active = true;
        }

        public override void OnClose()
        {
            blurWindow.Active = false;
            onResult(currentKey);
        }

        public override void ProcessInput()
        {
            if (!Input.anyKeyDown && !Input.GetKeyUp(KeyCode.LeftControl))
            {
                return;
            }
            foreach (KeyCode value in Enum.GetValues(typeof(KeyCode)))
            {
                if ((Input.GetKeyDown(value) && value != KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.LeftControl))
                {
                    if (value == KeyCode.Escape)
                    {
                        currentKey = null;
                    }
                    else if (value != KeyCode.Mouse0 && value != KeyCode.Mouse1)
                    {
                        currentKey = new KeybindingsPC.Key
                        {
                            ctrl = Input.GetKey(KeyCode.LeftControl),
                            key = Input.GetKeyUp(KeyCode.LeftControl) ? KeyCode.LeftControl : value
                        };
                    }
                    ScreenManager.main.CloseCurrent();
                    break;
                }
            }
        }

        public static string GetDisplayName(KeybindingsPC.Key k)
        {
            if (k == null)
                return "...";
            return (k.ctrl ? "Cmd + " : "") + GetString();
            string GetString()
            {
                switch (k.key)
                {
                    case KeyCode.UpArrow:
                        return "Up";
                    case KeyCode.DownArrow:
                        return "Down";
                    case KeyCode.LeftArrow:
                        return "Left";
                    case KeyCode.RightArrow:
                        return "Right";
                    case KeyCode.LeftControl:
                        return "Cmd";
                    case KeyCode.RightControl:
                        return "Cmd";
                    case KeyCode.LeftShift:
                        return "Shift";
                    case KeyCode.RightShift:
                        return "Shift";
                    case KeyCode.Period:
                        return "<";
                    case KeyCode.Comma:
                        return ">";
                    case KeyCode.LeftBracket:
                        return "[";
                    case KeyCode.RightBracket:
                        return "]";
                    case KeyCode.Return:
                        return "Enter";
                    case KeyCode.KeypadEnter:
                        return "Enter";
                    default:
                        return k.key.ToString();
                };
            }
        }
    }
}