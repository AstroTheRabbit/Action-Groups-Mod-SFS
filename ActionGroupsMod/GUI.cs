using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SFS.Builds;
using SFS.Input;
using SFS.Parts;
using SFS.UI.ModGUI;
using SFS.World;
using UITools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Button = SFS.UI.ModGUI.Button;
using GUIElement = SFS.UI.ModGUI.GUIElement;
using Object = UnityEngine.Object;
using Type = SFS.UI.ModGUI.Type;

namespace ActionGroupsMod
{
    public static class GUI
    {
        private static readonly int windowID = Builder.GetRandomID();
        private static readonly int actionGroupsWindowID = Builder.GetRandomID();
        private static readonly int actionGroupInfoWindowID = Builder.GetRandomID();

        public static Vector2Int windowSize = new Vector2Int(560, 740);
        public static Vector2Int HalfWindowSize => new Vector2Int((windowSize.x - 30) / 2, windowSize.y - 50);
        public static ActionGroup SelectedActionGroup { get; private set; }
        public static bool editingText;
        private static ActionGroup minimisedActionGroup;

        public static GameObject windowHolder;
        private static ClosableWindow window;

        private static Window window_groups;
        private static Window window_info;

        private static List<Button> buttons_groups;
        private static Button button_new;
        private static ActionGroupInfoUI actionGroupInfoUI;

        private static void CreateUI()
        {
            DestroyWindow();

            windowHolder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "ActionGroups - Window Holder");
            windowHolder.AddComponent<PartsOutline>();

            window = UIToolsBuilder.CreateClosableWindow
            (
                windowHolder.transform,
                windowID,
                HalfWindowSize.x + 20,
                windowSize.y,
                draggable: true,
                savePosition: true,
                titleText: "Action Groups"
            );
            window.RegisterPermanentSaving(Entrypoint.Main.ModNameID + "." + SceneManager.GetActiveScene().name);
            window.CreateLayoutGroup(Type.Horizontal);
            
            window_groups = Builder.CreateWindow(window, actionGroupsWindowID, HalfWindowSize.x, HalfWindowSize.y, savePosition: false);
            window_groups.CreateLayoutGroup(Type.Vertical, TextAnchor.UpperCenter);
            window_groups.EnableScrolling(Type.Vertical);

            window_info = Builder.CreateWindow(window, actionGroupInfoWindowID, HalfWindowSize.x, HalfWindowSize.y, savePosition: false);
            window_info.CreateLayoutGroup(Type.Vertical, TextAnchor.UpperCenter);
            window_info.EnableScrolling(Type.Vertical);

            window.OnMinimizedChangedEvent += () =>
            {
                if (Settings.settings.WindowMinimized = window.Minimized)
                {
                    minimisedActionGroup = SelectedActionGroup;
                    UpdateUI(null);
                }
                else
                {
                    UpdateUI(minimisedActionGroup);
                    minimisedActionGroup = null;
                }
            };
            window.Minimized = Settings.settings.WindowMinimized;
        }

        public static void DestroyWindow()
        {
            buttons_groups?.Clear();
            actionGroupInfoUI?.DestroyUI();
            if (windowHolder != null)
                Object.Destroy(windowHolder);
        }

        public static void UpdateUI(ActionGroup selected, bool setStagingSelected = false)
        {
            SelectedActionGroup = selected;
            
            if (windowHolder == null)
            {
                CreateUI();
            }

            buttons_groups?.ForEach(Destroy);
            button_new.Destroy();
            editingText = false;

            if (setStagingSelected)
            {
                Patches.StagingDrawer.SetSelected(null);
            }

            if (selected == null)
            {
                window.Size = new Vector2Int(HalfWindowSize.x + 20, windowSize.y);
                window_info.Active = false;
            }
            else
            {
                window.Size = windowSize;
                window_info.Active = true;
            }

            if (window.Minimized)
            {
                return;
            }

            buttons_groups = ActionGroupManager
                .GetCurrentActionGroups()?
                .Select(ag => CreateActionGroupUI(ag, ag == selected))
                .ToList();
            
            button_new = Builder.CreateButton
            (
                window_groups,
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
        }

        private static Button CreateActionGroupUI(ActionGroup ag, bool selected)
        {
            Button button = Builder.CreateButton
            (
                window_groups,
                HalfWindowSize.x - 10,
                120,
                text: $"{ag.name}\n({KeybindScreen.GetDisplayName(ag.key)})",
                onClick: () => UpdateUI(selected ? null : ag, true)
            );
            if (selected)
            {
                button.SetSelected(true);
                actionGroupInfoUI?.DestroyUI();
                actionGroupInfoUI = new ActionGroupInfoUI(ag, window_info);
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

        public static void OnPlayerChange(Player playerNew)
        {
            if (playerNew is Rocket rocket && windowHolder != null)
            {
                windowHolder.SetActive(rocket.hasControl.Value);
                UpdateUI(null);
            }
        }
    }

    public class ActionGroupInfoUI
    {
        private readonly TextInput input_name;
        private readonly Button button_key;
        private readonly Container container_hold_delete;
        private readonly Button button_activate;
        private readonly Separator seperator_icons;
        private readonly Container container_icons;

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
            input_name.field.onEndEdit.AddListener
            (
                input =>
                {
                    ag.name = input;
                    GUI.UpdateUI(ag);
                }
            );
            input_name.field.onSelect.AddListener(_ => GUI.editingText = true);
            input_name.field.onDeselect.AddListener(_ => GUI.editingText = false);

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

            //* Activate
            if (ActionGroupManager.InWorld)
            {
                button_activate = Builder.CreateButton
                (
                    window,
                    GUI.HalfWindowSize.x - 10,
                    50,
                    text: "Activate",
                    onClick: ag.Activate
                );
            }

            // * Part Icons
            seperator_icons = Builder.CreateSeparator(window, GUI.HalfWindowSize.x - 10, 20);
            container_icons = Builder.CreateContainer(window);
            container_icons.CreateLayoutGroup(Type.Horizontal);

            Container partIconsHolderLeft = Builder.CreateContainer(container_icons);
            partIconsHolderLeft.CreateLayoutGroup(Type.Vertical);
            
            Container partIconsHolderRight = Builder.CreateContainer(container_icons);
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
            button_activate.Destroy();
            seperator_icons.Destroy();
            container_icons.Destroy();
        }

        public void OpenKeybindScreen(ActionGroup ag)
        {
            button_key.SetSelected(true);
            KeybindScreen.Open
            (
                ag.key,
                result =>
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
    internal class KeybindScreen : Screen_Base
    {
        private static KeybindScreen main;
        private static readonly int blurWindowID = Builder.GetRandomID();
        private Window blurWindow;
        private KeybindingsPC.Key currentKey;
        private Action<KeybindingsPC.Key> onResult;

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
            
            string ctrl = Application.platform == RuntimePlatform.OSXPlayer ? "Cmd" : "Ctrl";

            if (k.ctrl)
                return ctrl + " + " + GetString();
            else
                return GetString();

            string GetString()
            {
                // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
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
                    case KeyCode.RightControl:
                        return "Cmd";
                    case KeyCode.LeftShift:
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
                    case KeyCode.KeypadEnter:
                        return "Enter";
                    default:
                        return k.key.ToString();
                }
            }
        }
    }

    // ? Derived from `SFS.Builds.BuildSelector`.
    public class PartsOutline : MonoBehaviour, I_GLDrawer
    {
        internal void Awake()
        {
            GLDrawer.Register(this);
        }

        internal void OnDestroy()
        {
            GLDrawer.Unregister(this);
        }

        void I_GLDrawer.Draw()
        {
            if (GUI.SelectedActionGroup != null)
                BuildSelector.DrawOutline(GUI.SelectedActionGroup.parts, false, Color.white, 0.1f);
        }
    }
}