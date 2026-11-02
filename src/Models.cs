using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace MiniKeyboardCyber
{
    public enum ActionCategory : byte
    {
        None = 0,
        Keyboard = 1,
        Multimedia = 2,
        Mouse = 3,
        Led = 8
    }

    public enum MultimediaAction : byte
    {
        PlayPause = 1,
        PrevTrack = 2,
        NextTrack = 3,
        Mute = 4,
        VolumeUp = 5,
        VolumeDown = 6
    }

    public enum MouseActionType : byte
    {
        LeftClick = 1,
        RightClick = 2,
        MiddleClick = 3,
        WheelUp = 4,
        WheelDown = 5
    }

    [Serializable]
    public class KeyStrokeStep
    {
        public byte ModifierMask { get; set; }
        public byte HidKeyCode { get; set; }
        public string KeyLabel { get; set; }

        public KeyStrokeStep()
        {
            KeyLabel = "";
        }

        public KeyStrokeStep(byte modifierMask, byte hidKeyCode, string keyLabel)
        {
            ModifierMask = modifierMask;
            HidKeyCode = hidKeyCode;
            KeyLabel = keyLabel ?? "";
        }

        public KeyStrokeStep Clone()
        {
            return new KeyStrokeStep(ModifierMask, HidKeyCode, KeyLabel);
        }

        public string ToDisplayString()
        {
            List<string> parts = new List<string>();
            if ((ModifierMask & 0x01) != 0) parts.Add("Ctrl");
            if ((ModifierMask & 0x10) != 0) parts.Add("RCtrl");
            if ((ModifierMask & 0x02) != 0) parts.Add("Shift");
            if ((ModifierMask & 0x20) != 0) parts.Add("RShift");
            if ((ModifierMask & 0x04) != 0) parts.Add("Alt");
            if ((ModifierMask & 0x40) != 0) parts.Add("RAlt");
            if ((ModifierMask & 0x08) != 0) parts.Add("Win");
            if ((ModifierMask & 0x80) != 0) parts.Add("RWin");

            if (!string.IsNullOrEmpty(KeyLabel) && HidKeyCode != 0)
            {
                parts.Add(KeyLabel);
            }
            return string.Join("+", parts.ToArray());
        }
    }

    [Serializable]
    public class ButtonConfig
    {
        public byte SlotId { get; set; }
        public string SlotName { get; set; }
        public string ShortSlotName { get; set; }
        public string CustomAlias { get; set; }
        public ActionCategory Category { get; set; }
        public List<KeyStrokeStep> Steps { get; set; }
        public byte StandaloneModifierMask { get; set; }
        public MultimediaAction MediaAction { get; set; }
        public MouseActionType MouseAction { get; set; }
        public byte MouseWheelModifier { get; set; } // 0=None, 1=Ctrl, 2=Shift, 4=Alt

        public ButtonConfig()
        {
            SlotName = "";
            ShortSlotName = "";
            CustomAlias = "";
            Category = ActionCategory.None;
            Steps = new List<KeyStrokeStep>();
            MediaAction = MultimediaAction.PlayPause;
            MouseAction = MouseActionType.LeftClick;
        }

        public ButtonConfig(byte slotId, string slotName, string shortName) : this()
        {
            SlotId = slotId;
            SlotName = slotName;
            ShortSlotName = shortName;
        }

        [XmlIgnore]
        public bool IsConfigured
        {
            get
            {
                if (Category == ActionCategory.None) return false;
                if (Category == ActionCategory.Keyboard)
                {
                    return Steps.Count > 0 || StandaloneModifierMask != 0;
                }
                return true;
            }
        }

        public void Clear()
        {
            Category = ActionCategory.None;
            Steps.Clear();
            StandaloneModifierMask = 0;
            CustomAlias = "";
            MouseWheelModifier = 0;
        }

        public ButtonConfig Clone()
        {
            ButtonConfig copy = new ButtonConfig(SlotId, SlotName, ShortSlotName);
            copy.CustomAlias = CustomAlias;
            copy.Category = Category;
            copy.StandaloneModifierMask = StandaloneModifierMask;
            copy.MediaAction = MediaAction;
            copy.MouseAction = MouseAction;
            copy.MouseWheelModifier = MouseWheelModifier;
            foreach (var s in Steps)
            {
                copy.Steps.Add(s.Clone());
            }
            return copy;
        }

        public string GetCategoryBadge()
        {
            switch (Category)
            {
                case ActionCategory.Keyboard:
                    return Steps.Count > 1 ? "MACRO" : "TECLADO";
                case ActionCategory.Multimedia:
                    return "MEDIA";
                case ActionCategory.Mouse:
                    return "RATÓN";
                default:
                    return "VACÍO";
            }
        }

        public string GetTechnicalSummary()
        {
            if (!IsConfigured) return "Sin asignar";

            switch (Category)
            {
                case ActionCategory.Keyboard:
                    if (Steps.Count == 0 && StandaloneModifierMask != 0)
                    {
                        return new KeyStrokeStep(StandaloneModifierMask, 0, "").ToDisplayString();
                    }
                    List<string> stepStrings = new List<string>();
                    foreach (var s in Steps)
                    {
                        stepStrings.Add(s.ToDisplayString());
                    }
                    return string.Join(" → ", stepStrings.ToArray());

                case ActionCategory.Multimedia:
                    switch (MediaAction)
                    {
                        case MultimediaAction.PlayPause: return "► Play / Pausa";
                        case MultimediaAction.PrevTrack: return "◄◄ Pista Anterior";
                        case MultimediaAction.NextTrack: return "►► Pista Siguiente";
                        case MultimediaAction.Mute: return "✖ Silenciar Audio";
                        case MultimediaAction.VolumeUp: return "▲ Volumen +";
                        case MultimediaAction.VolumeDown: return "▼ Volumen -";
                        default: return "Multimedia";
                    }

                case ActionCategory.Mouse:
                    string modPrefix = "";
                    if (MouseAction == MouseActionType.WheelUp || MouseAction == MouseActionType.WheelDown)
                    {
                        if (MouseWheelModifier == 1) modPrefix = "Ctrl + ";
                        else if (MouseWheelModifier == 2) modPrefix = "Shift + ";
                        else if (MouseWheelModifier == 4) modPrefix = "Alt + ";
                    }
                    switch (MouseAction)
                    {
                        case MouseActionType.LeftClick: return "◉ Click Izquierdo";
                        case MouseActionType.RightClick: return "◉ Click Derecho";
                        case MouseActionType.MiddleClick: return "◉ Click Central";
                        case MouseActionType.WheelUp: return modPrefix + "▲ Rueda Arriba";
                        case MouseActionType.WheelDown: return modPrefix + "▼ Rueda Abajo";
                        default: return "Ratón";
                    }

                default:
                    return "Sin asignar";
            }
        }

        public string GetPrimaryLabel()
        {
            if (!IsConfigured) return "VACÍO";
            if (!string.IsNullOrEmpty(CustomAlias)) return CustomAlias;
            return GetTechnicalSummary();
        }
    }

    [Serializable]
    public class LayerProfile
    {
        public byte LayerIndex { get; set; } // 1, 2, 3
        public string ProfileName { get; set; }
        public byte LedMode { get; set; } // 0 = Off, 1 = Mode 1, 2 = Mode 2
        public List<ButtonConfig> Slots { get; set; }

        public LayerProfile()
        {
            LayerIndex = 1;
            ProfileName = "Perfil Cyberpunk";
            LedMode = 1;
            Slots = new List<ButtonConfig>();
        }

        public static LayerProfile CreateEmpty(byte layerIndex, string name)
        {
            LayerProfile profile = new LayerProfile
            {
                LayerIndex = layerIndex,
                ProfileName = name,
                LedMode = 1
            };

            for (byte i = 1; i <= 12; i++)
            {
                profile.Slots.Add(new ButtonConfig(i, string.Format("Tecla {0:00}", i), string.Format("K{0:00}", i)));
            }

            // Knob 1: 13=Left, 14=Center, 15=Right
            profile.Slots.Add(new ButtonConfig(13, "Knob 1 · Giro Izquierda (⟲)", "K1 ⟲"));
            profile.Slots.Add(new ButtonConfig(14, "Knob 1 · Click Central (●)", "K1 ●"));
            profile.Slots.Add(new ButtonConfig(15, "Knob 1 · Giro Derecha (⟳)", "K1 ⟳"));

            // Knob 2: 16=Left, 17=Center, 18=Right
            profile.Slots.Add(new ButtonConfig(16, "Knob 2 · Giro Izquierda (⟲)", "K2 ⟲"));
            profile.Slots.Add(new ButtonConfig(17, "Knob 2 · Click Central (●)", "K2 ●"));
            profile.Slots.Add(new ButtonConfig(18, "Knob 2 · Giro Derecha (⟳)", "K2 ⟳"));

            return profile;
        }

        public ButtonConfig GetSlot(byte slotId)
        {
            foreach (var s in Slots)
            {
                if (s.SlotId == slotId) return s;
            }
            return Slots[0];
        }

        public int GetConfiguredCount()
        {
            int count = 0;
            foreach (var s in Slots)
            {
                if (s.IsConfigured) count++;
            }
            return count;
        }

        public string GetLedModeDescription()
        {
            switch (LedMode)
            {
                case 0: return "Modo 0 · LED Apagado";
                case 1: return "Modo 1 · Iluminación Reactiva / Fija";
                case 2: return "Modo 2 · Efecto Respiración Neón";
                default: return "Modo " + LedMode;
            }
        }

        public void SaveToFile(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(LayerProfile));
            using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
            {
                serializer.Serialize(writer, this);
            }
        }

        public static LayerProfile LoadFromFile(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(LayerProfile));
            using (StreamReader reader = new StreamReader(path, Encoding.UTF8))
            {
                return (LayerProfile)serializer.Deserialize(reader);
            }
        }

        public static LayerProfile CreatePresetProductivity(byte layerIndex)
        {
            LayerProfile p = CreateEmpty(layerIndex, "Productividad & Dev Pro");
            p.LedMode = 1;

            SetKey(p, 1, "Copiar", 0x01, 6, "C");             // Ctrl+C
            SetKey(p, 2, "Pegar", 0x01, 25, "V");             // Ctrl+V
            SetKey(p, 3, "Cortar", 0x01, 27, "X");            // Ctrl+X
            SetKey(p, 4, "Guardar Todo", 0x03, 22, "S");      // Ctrl+Shift+S

            SetKey(p, 5, "Deshacer", 0x01, 29, "Z");          // Ctrl+Z
            SetKey(p, 6, "Rehacer", 0x01, 28, "Y");           // Ctrl+Y
            SetKey(p, 7, "Buscar", 0x01, 9, "F");             // Ctrl+F
            SetKey(p, 8, "Captura Pantalla", 0x0A, 22, "S");  // Win+Shift+S

            SetKey(p, 9, "Cambiar App", 0x04, 43, "Tab");     // Alt+Tab
            SetKey(p, 10, "Terminal / Ejecutar", 0x08, 21, "R"); // Win+R
            SetKey(p, 11, "Admin. Tareas", 0x03, 41, "ESC");  // Ctrl+Shift+Esc
            SetKey(p, 12, "Bloquear PC", 0x08, 15, "L");      // Win+L

            // Knob 1: Audio Control
            SetMedia(p, 13, "Bajar Volumen", MultimediaAction.VolumeDown);
            SetMedia(p, 14, "Silenciar Audio", MultimediaAction.Mute);
            SetMedia(p, 15, "Subir Volumen", MultimediaAction.VolumeUp);

            // Knob 2: Zoom / Scroll
            SetMouse(p, 16, "Zoom - (Alejar)", MouseActionType.WheelDown, 1);
            SetMedia(p, 17, "Play / Pausa", MultimediaAction.PlayPause);
            SetMouse(p, 18, "Zoom + (Acercar)", MouseActionType.WheelUp, 1);

            return p;
        }

        public static LayerProfile CreatePresetStreaming(byte layerIndex)
        {
            LayerProfile p = CreateEmpty(layerIndex, "Streamer & Creator Deck");
            p.LedMode = 2;

            SetKey(p, 1, "Escena 1 (Intro)", 0x03, 58, "F1");
            SetKey(p, 2, "Escena 2 (Cámara)", 0x03, 59, "F2");
            SetKey(p, 3, "Escena 3 (Juego)", 0x03, 60, "F3");
            SetKey(p, 4, "Escena 4 (BRB)", 0x03, 61, "F4");

            SetKey(p, 5, "Mute Mic Discord", 0x03, 16, "M");
            SetKey(p, 6, "Ensordecer Discord", 0x03, 7, "D");
            SetKey(p, 7, "Iniciar Stream", 0x03, 66, "F9");
            SetKey(p, 8, "Grabar Clip", 0x04, 67, "F10");

            SetMedia(p, 9, "Pista Anterior", MultimediaAction.PrevTrack);
            SetMedia(p, 10, "Play / Pausa", MultimediaAction.PlayPause);
            SetMedia(p, 11, "Pista Siguiente", MultimediaAction.NextTrack);
            SetKey(p, 12, "Mostrar Escritorio", 0x08, 7, "D");

            SetMedia(p, 13, "Volumen -", MultimediaAction.VolumeDown);
            SetMedia(p, 14, "Mute General", MultimediaAction.Mute);
            SetMedia(p, 15, "Volumen +", MultimediaAction.VolumeUp);

            SetMouse(p, 16, "Scroll Arriba", MouseActionType.WheelUp, 0);
            SetMouse(p, 17, "Click Central", MouseActionType.MiddleClick, 0);
            SetMouse(p, 18, "Scroll Abajo", MouseActionType.WheelDown, 0);

            return p;
        }

        private static void SetKey(LayerProfile p, byte slotId, string alias, byte mod, byte hidCode, string label)
        {
            ButtonConfig slot = p.GetSlot(slotId);
            slot.Clear();
            slot.Category = ActionCategory.Keyboard;
            slot.CustomAlias = alias;
            slot.Steps.Add(new KeyStrokeStep(mod, hidCode, label));
        }

        private static void SetMedia(LayerProfile p, byte slotId, string alias, MultimediaAction action)
        {
            ButtonConfig slot = p.GetSlot(slotId);
            slot.Clear();
            slot.Category = ActionCategory.Multimedia;
            slot.CustomAlias = alias;
            slot.MediaAction = action;
        }

        private static void SetMouse(LayerProfile p, byte slotId, string alias, MouseActionType action, byte wheelMod)
        {
            ButtonConfig slot = p.GetSlot(slotId);
            slot.Clear();
            slot.Category = ActionCategory.Mouse;
            slot.CustomAlias = alias;
            slot.MouseAction = action;
            slot.MouseWheelModifier = wheelMod;
        }
    }

    public class HidKeyDefinition
    {
        public byte HidCode { get; set; }
        public string Label { get; set; }
        public string DisplayText { get; set; }
        public float WidthWeight { get; set; }

        public HidKeyDefinition(byte hidCode, string label, string displayText = null, float widthWeight = 1.0f)
        {
            HidCode = hidCode;
            Label = label;
            DisplayText = displayText ?? label;
            WidthWeight = widthWeight;
        }
    }

    public static class HidKeyMap
    {
        // Modifier masks matching hardware bitmask:
        // LCtrl=0x01, LShift=0x02, LAlt=0x04, LWin=0x08, RCtrl=0x10, RShift=0x20, RAlt=0x40, RWin=0x80
        public const byte MOD_LCTRL = 0x01;
        public const byte MOD_LSHIFT = 0x02;
        public const byte MOD_LALT = 0x04;
        public const byte MOD_LWIN = 0x08;
        public const byte MOD_RCTRL = 0x10;
        public const byte MOD_RSHIFT = 0x20;
        public const byte MOD_RALT = 0x40;
        public const byte MOD_RWIN = 0x80;

        public static bool TryMapWinFormsKey(Keys keyCode, out byte hidCode, out string label)
        {
            hidCode = 0;
            label = "";

            if (keyCode >= Keys.A && keyCode <= Keys.Z)
            {
                hidCode = (byte)(4 + (keyCode - Keys.A));
                label = keyCode.ToString();
                return true;
            }
            if (keyCode >= Keys.D1 && keyCode <= Keys.D9)
            {
                hidCode = (byte)(30 + (keyCode - Keys.D1));
                label = ((int)(keyCode - Keys.D0)).ToString();
                return true;
            }
            if (keyCode == Keys.D0)
            {
                hidCode = 39;
                label = "0";
                return true;
            }
            if (keyCode >= Keys.F1 && keyCode <= Keys.F12)
            {
                hidCode = (byte)(58 + (keyCode - Keys.F1));
                label = keyCode.ToString();
                return true;
            }
            if (keyCode >= Keys.NumPad1 && keyCode <= Keys.NumPad9)
            {
                hidCode = (byte)(89 + (keyCode - Keys.NumPad1));
                label = "Num" + ((int)(keyCode - Keys.NumPad0));
                return true;
            }
            if (keyCode == Keys.NumPad0) { hidCode = 98; label = "Num0"; return true; }

            switch (keyCode)
            {
                case Keys.Return: hidCode = 40; label = "Enter"; return true;
                case Keys.Escape: hidCode = 41; label = "ESC"; return true;
                case Keys.Back: hidCode = 42; label = "BackSpace"; return true;
                case Keys.Tab: hidCode = 43; label = "Tab"; return true;
                case Keys.Space: hidCode = 44; label = "Space"; return true;
                case Keys.OemMinus: hidCode = 45; label = "-"; return true;
                case Keys.Oemplus: hidCode = 46; label = "="; return true;
                case Keys.OemOpenBrackets: hidCode = 47; label = "["; return true;
                case Keys.Oem6: hidCode = 48; label = "]"; return true;
                case Keys.Oem5: hidCode = 49; label = "\\"; return true;
                case Keys.Oem1: hidCode = 51; label = ";"; return true;
                case Keys.Oem7: hidCode = 52; label = "'"; return true;
                case Keys.Oemtilde: hidCode = 53; label = "`"; return true;
                case Keys.Oemcomma: hidCode = 54; label = ","; return true;
                case Keys.OemPeriod: hidCode = 55; label = "."; return true;
                case Keys.OemQuestion: hidCode = 56; label = "/"; return true;
                case Keys.Capital: hidCode = 57; label = "CapsLock"; return true;
                case Keys.PrintScreen: hidCode = 70; label = "PrtSc"; return true;
                case Keys.Scroll: hidCode = 71; label = "ScLock"; return true;
                case Keys.Pause: hidCode = 72; label = "Pause"; return true;
                case Keys.Insert: hidCode = 73; label = "Ins"; return true;
                case Keys.Home: hidCode = 74; label = "Home"; return true;
                case Keys.PageUp: hidCode = 75; label = "PageUp"; return true;
                case Keys.Delete: hidCode = 76; label = "Delete"; return true;
                case Keys.End: hidCode = 77; label = "End"; return true;
                case Keys.PageDown: hidCode = 78; label = "PageDown"; return true;
                case Keys.Right: hidCode = 79; label = "→"; return true;
                case Keys.Left: hidCode = 80; label = "←"; return true;
                case Keys.Down: hidCode = 81; label = "↓"; return true;
                case Keys.Up: hidCode = 82; label = "↑"; return true;
                case Keys.NumLock: hidCode = 83; label = "NumLock"; return true;
                case Keys.Divide: hidCode = 84; label = "Num/"; return true;
                case Keys.Multiply: hidCode = 85; label = "Num*"; return true;
                case Keys.Subtract: hidCode = 86; label = "Num-"; return true;
                case Keys.Add: hidCode = 87; label = "Num+"; return true;
                case Keys.Decimal: hidCode = 99; label = "Num."; return true;
                case Keys.Apps: hidCode = 101; label = "Menu"; return true;
                default: return false;
            }
        }
    }
}
