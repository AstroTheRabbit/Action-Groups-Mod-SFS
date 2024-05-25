using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using UITools;
using SFS.Input;
using SFS.Parts;
using SFS.Builds;
using SFS.UI.ModGUI;

using Type = SFS.UI.ModGUI.Type;
using Object = UnityEngine.Object;
using Space = SFS.UI.ModGUI.Space;
using Button = SFS.UI.ModGUI.Button;

namespace ActionGroupsMod
{
    public static class GUI
    {
        static readonly int windowID = Builder.GetRandomID();
        static readonly int actionGroupsWindowID = Builder.GetRandomID();
        static readonly int actionGroupInfoWindowID = Builder.GetRandomID();
        static PartsOutline partsOutline;

        public static Vector2Int windowSize = new Vector2Int(560, 740);
        public static Vector2Int HalfWindowSize => new Vector2Int((windowSize.x - 30) / 2, windowSize.y - 50);
        public static ActionGroup SelectedActionGroup { get; private set; } = null;
        static ActionGroup minimisedActionGroup = null;

        public static GameObject windowHolder;
        static ClosableWindow window;

        static Window window_actionGroups;
        static Window window_actionGroupInfo;
    
        static List<Button> actionGroupButtons;
        static Button newActionGroupButton;
        static ActionGroupInfoUI actionGroupInfoUI;

        public static void CreateUI(string sceneName)
        {
            DestroyWindow();

            windowHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "ActionGroups - Window Holder");
            partsOutline = windowHolder.AddComponent<PartsOutline>();

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
            window.OnMinimizedChangedEvent += () =>
            {
                if (window.Minimized)
                {
                    minimisedActionGroup = SelectedActionGroup;
                    SelectedActionGroup = null;
                }
                else
                {
                    UpdateUI(minimisedActionGroup);
                }
            };
            
            window_actionGroups = Builder.CreateWindow(window, actionGroupsWindowID, HalfWindowSize.x, HalfWindowSize.y, savePosition: false);
            window_actionGroups.CreateLayoutGroup(Type.Vertical, TextAnchor.UpperCenter);
            window_actionGroups.EnableScrolling(Type.Vertical);

            window_actionGroupInfo = Builder.CreateWindow(window, actionGroupInfoWindowID, HalfWindowSize.x, HalfWindowSize.y, savePosition: false);
            window_actionGroupInfo.CreateLayoutGroup(Type.Vertical, TextAnchor.UpperCenter);
            window_actionGroupInfo.EnableScrolling(Type.Vertical);

            UpdateUI(null);
        }

        public static void DestroyWindow()
        {
            actionGroupButtons?.Clear();
            actionGroupInfoUI?.DestroyUI();
            if (windowHolder != null)
                Object.Destroy(windowHolder);
        }

        public static void UpdateUI(ActionGroup selected, bool setStagingSelected = false)
        {
            actionGroupButtons?.ForEach(Destroy);
            newActionGroupButton.Destroy();

            SelectedActionGroup = selected;

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
            
            newActionGroupButton = Builder.CreateButton
                (
                    window_actionGroups,
                    HalfWindowSize.x - 10,
                    60,
                    text: "New",
                    onClick: () =>
                    {
                        ActionGroup ag = new ActionGroup();
                        ActionGroupManager.GetCurrentActionGroups().Add(ag);
                        UpdateUI(ag, true);
                    }
                );

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
                text: $"{ag.name}\n({KeybindScreen.GetDisplayName(ag.key)})",
                onClick: () => UpdateUI(selected ? null : ag, true)
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
        readonly TextInput input_name;
        readonly Button button_key;
        readonly Container container_hold_delete;
        readonly Separator seperator_partIcons;
        readonly Container container_partIcons;

        public ActionGroupInfoUI(ActionGroup ag, Window window)
        {
            // * Name
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
                    GUI.UpdateUI(ag);
                }
            );

            // * Keybind
            button_key = Builder.CreateButton
            (
                window,
                GUI.HalfWindowSize.x - 10,
                50,
                text: KeybindScreen.GetDisplayName(ag.key),
                onClick: () => OpenKeybindScreen(ag)
            );

            // * Hold & Delete
            container_hold_delete = Builder.CreateContainer(window);
            container_hold_delete.CreateLayoutGroup(Type.Horizontal);
            
            Button button_hold = null;
            button_hold = Builder.CreateButton
            (
                container_hold_delete,
                (GUI.HalfWindowSize.x - 30) / 2,
                50,
                text: "Hold",
                onClick: () =>
                {
                    ag.holdToActivate = !ag.holdToActivate;
                    button_hold.SetSelected(ag.holdToActivate);
                }
            );
            button_hold.SetSelected(ag.holdToActivate);

            Builder.CreateButton
            (
                container_hold_delete,
                (GUI.HalfWindowSize.x - 30) / 2,
                50,
                text: "Delete",
                onClick: () =>
                {
                    ActionGroupManager.GetCurrentActionGroups().Remove(ag);
                    GUI.UpdateUI(null);
                }
            );


            // * Part Icons
            seperator_partIcons = Builder.CreateSeparator(window, GUI.HalfWindowSize.x - 10, 20);
            container_partIcons = Builder.CreateContainer(window);
            container_partIcons.CreateLayoutGroup(Type.Horizontal);

            Container partIconsHolderLeft = Builder.CreateContainer(container_partIcons);
            partIconsHolderLeft.CreateLayoutGroup(Type.Vertical);
            
            Container partIconsHolderRight = Builder.CreateContainer(container_partIcons);
            partIconsHolderRight.CreateLayoutGroup(Type.Vertical);

            int heightLeft = 0, heightRight = 0;
            foreach (Part part in ag.parts)
            {
                if (heightLeft < heightRight)
                {
                    CreatePartIcon(partIconsHolderLeft, part, GUI.HalfWindowSize.x / 2, out int height);
                    heightLeft += height;
                }
                else
                {
                    CreatePartIcon(partIconsHolderRight, part, GUI.HalfWindowSize.x / 2, out int height);
                    heightRight += height;
                }
            }
        }

        public void DestroyUI()
        {
            input_name.Destroy();
            button_key.Destroy();
            container_hold_delete.Destroy();
            seperator_partIcons.Destroy();
            container_partIcons.Destroy();
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
                    GUI.UpdateUI(ag);
                }
            );
        }

        // ? Derived from `SFS.World.StageUI.CreatePartIcon`.
        public static RawImage CreatePartIcon(Transform holder, Part part, int width, out int height)
        {
            SFS.Base.partsLoader.parts.TryGetValue(part.name, out Part originalPart);
            PartSave save = new PartSave(part)
            {
                orientation = originalPart != null ? originalPart.orientation.orientation.Value.GetCopy() : new SFS.Parts.Modules.Orientation(1, 1, 0)
            };
            RenderTexture texture = PartIconCreator.main.CreatePartIcon_Staging(save, width);
            RawImage image = Object.Instantiate(Patches.StagingDrawer.stageUIPrefab.iconPrefab, holder);
            image.gameObject.SetActive(true);
            image.texture = texture;
            image.rectTransform.sizeDelta = new Vector2(texture.width, texture.height) / 2;
            height = texture.height;
            return image;
        }
    }

    // ? Derived from `SFS.Input.KeyBinder`.
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

    // ? Derived from `SFS.Builds.BuildSelector`.
    public class PartsOutline : MonoBehaviour, I_GLDrawer
    {
        void Awake()
        {
            GLDrawer.Register(this);
        }

        void OnDestroy()
        {
            GLDrawer.Unregister(this);
        }

        public void Draw()
        {
            if (GUI.SelectedActionGroup != null)
                BuildSelector.DrawOutline(GUI.SelectedActionGroup.parts, false, Color.white, 0.1f);
        }
    }
}