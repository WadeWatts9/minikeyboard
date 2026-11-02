using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HidLibrary;

namespace MiniKeyboardCyber
{
    public class HidDeviceController : IDisposable
    {
        public const int VENDOR_ID = 0x1189;
        public const int PRODUCT_ID = 0x8890;

        private bool _devState;
        private HidDevice _wDevice;
        private byte _reportId = 3;

        public bool IsConnected
        {
            get { return _devState && _wDevice != null && _wDevice.IsConnected; }
        }

        public byte ActiveReportId
        {
            get { return _reportId; }
        }

        public string DevicePath
        {
            get { return _wDevice != null ? _wDevice.DevicePath : ""; }
        }

        public bool CheckAndConnect()
        {
            if (_devState && _wDevice != null)
            {
                if (_wDevice.IsConnected)
                {
                    return true;
                }
                CloseDevice();
            }

            try
            {
                IEnumerable<HidDevice> devices = HidDevices.Enumerate(VENDOR_ID);
                foreach (HidDevice device in devices)
                {
                    if (device.DevicePath != null && device.DevicePath.IndexOf("mi_01", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        _wDevice = device;
                        _wDevice.OpenDevice();
                        _wDevice.MonitorDeviceEvents = true;
                        _devState = true;
                        DetectKeyboardVersion();
                        return true;
                    }
                }
            }
            catch
            {
                _devState = false;
            }
            return false;
        }

        public void CloseDevice()
        {
            try
            {
                if (_wDevice != null)
                {
                    _wDevice.CloseDevice();
                    _wDevice = null;
                }
            }
            catch
            {
            }
            _devState = false;
        }

        public bool WriteDevice(byte id, byte[] arrayBuff)
        {
            if (_wDevice == null || !_devState) return false;
            try
            {
                HidReport report = _wDevice.CreateReport();
                report.ReportId = id;
                int len = Math.Min(8, Math.Min(arrayBuff.Length, report.Data.Length));
                for (int i = 0; i < len; i++)
                {
                    report.Data[i] = arrayBuff[i];
                }
                return _wDevice.WriteReport(report, 500);
            }
            catch
            {
                return false;
            }
        }

        public byte DetectKeyboardVersion()
        {
            byte[] array = new byte[65];
            if (WriteDevice(3, array))
            {
                _reportId = 3;
            }
            else if (WriteDevice(0, array))
            {
                _reportId = 0;
            }
            else if (WriteDevice(2, array))
            {
                _reportId = 2;
            }
            return _reportId;
        }

        private bool SendSwitchLayer(byte layer)
        {
            if (_reportId == 0) return true;
            byte[] buf = new byte[65];
            buf[0] = 0xA1; // 161
            buf[1] = layer == 0 ? (byte)1 : layer;
            return WriteDevice(_reportId, buf);
        }

        private bool SendWriteFlashCmd()
        {
            byte[] buf = new byte[65];
            buf[0] = 0xAA; // 170
            buf[1] = 0xAA; // 170
            return WriteDevice(_reportId, buf);
        }

        private bool SendWriteFlashLedCmd()
        {
            byte[] buf = new byte[65];
            buf[0] = 0xAA; // 170
            buf[1] = 0xA1; // 161
            return WriteDevice(_reportId, buf);
        }

        public bool DownloadLedMode(byte layer, byte ledMode)
        {
            if (!IsConnected && !CheckAndConnect()) return false;

            DetectKeyboardVersion();
            byte activeLayer = (layer < 1 || layer > 3) ? (byte)1 : layer;

            byte[] buf = new byte[65];
            buf[0] = 0xB0; // 176: LED target
            if (_reportId == 0)
            {
                buf[1] = (byte)(ActionCategory.Led);
            }
            else
            {
                SendSwitchLayer(activeLayer);
                buf[1] = (byte)((activeLayer << 4) | ((byte)ActionCategory.Led & 0x0F));
            }
            buf[2] = ledMode; // 0, 1, or 2

            if (!WriteDevice(_reportId, buf)) return false;
            return SendWriteFlashLedCmd();
        }

        public bool DownloadSingleSlot(byte layer, ButtonConfig slot)
        {
            if (slot == null || !slot.IsConfigured) return false;
            if (!IsConnected && !CheckAndConnect()) return false;

            DetectKeyboardVersion();
            byte activeLayer = (layer < 1 || layer > 3) ? (byte)1 : layer;

            byte[] buf = new byte[65];
            buf[0] = slot.SlotId;

            byte keyType = (byte)slot.Category;
            if (_reportId == 0)
            {
                buf[1] = (byte)(keyType & 0x0F);
            }
            else
            {
                if (!SendSwitchLayer(activeLayer)) return false;
                buf[1] = (byte)((activeLayer << 4) | (keyType & 0x0F));
            }

            switch (slot.Category)
            {
                case ActionCategory.Keyboard:
                {
                    byte charCount = (byte)Math.Min(5, slot.Steps.Count);
                    buf[2] = charCount;

                    // Packet b = 0: modifier of first key (or standalone modifier), keycode = 0
                    byte firstMod = charCount > 0 ? slot.Steps[0].ModifierMask : slot.StandaloneModifierMask;
                    for (byte b = 0; b <= charCount; b++)
                    {
                        buf[3] = b;
                        if (b == 0)
                        {
                            buf[4] = firstMod;
                            buf[5] = 0;
                        }
                        else
                        {
                            KeyStrokeStep step = slot.Steps[b - 1];
                            buf[4] = step.ModifierMask;
                            buf[5] = step.HidKeyCode;
                        }

                        if (!WriteDevice(_reportId, buf)) return false;
                    }
                    return SendWriteFlashCmd();
                }

                case ActionCategory.Multimedia:
                {
                    PopulateMultimediaBytes(_reportId, slot.MediaAction, out buf[2], out buf[3]);
                    buf[4] = 0;
                    buf[5] = 0;
                    if (!WriteDevice(_reportId, buf)) return false;
                    return SendWriteFlashCmd();
                }

                case ActionCategory.Mouse:
                {
                    PopulateMouseBytes(slot.MouseAction, slot.MouseWheelModifier, out buf[2], out buf[3], out buf[4], out buf[5], out buf[6]);
                    if (!WriteDevice(_reportId, buf)) return false;
                    return SendWriteFlashCmd();
                }

                default:
                    return false;
            }
        }

        public int DownloadProfile(LayerProfile profile, Action<int, int, string> progressCallback, out string errorMsg)
        {
            errorMsg = null;
            if (!IsConnected && !CheckAndConnect())
            {
                errorMsg = "El miniteclado USB (VID:1189 / PID:8890) no está conectado físicamente.";
                return 0;
            }

            List<ButtonConfig> configuredSlots = new List<ButtonConfig>();
            foreach (var s in profile.Slots)
            {
                if (s.IsConfigured) configuredSlots.Add(s);
            }

            int totalSteps = configuredSlots.Count + 1; // +1 for LED mode
            int currentStep = 0;
            int successCount = 0;

            foreach (var slot in configuredSlots)
            {
                currentStep++;
                if (progressCallback != null)
                {
                    progressCallback(currentStep, totalSteps, "Descargando " + slot.SlotName + " (" + slot.GetPrimaryLabel() + ")...");
                }

                if (DownloadSingleSlot(profile.LayerIndex, slot))
                {
                    successCount++;
                }
                else
                {
                    errorMsg = "Fallo de escritura USB HID en " + slot.SlotName;
                    return successCount;
                }
                Thread.Sleep(25);
            }

            // Also apply LED configuration for the profile
            currentStep++;
            if (progressCallback != null)
            {
                progressCallback(currentStep, totalSteps, "Configurando iluminación LED (" + profile.GetLedModeDescription() + ")...");
            }
            DownloadLedMode(profile.LayerIndex, profile.LedMode);

            return successCount;
        }

        private static void PopulateMultimediaBytes(byte reportId, MultimediaAction action, out byte b2, out byte b3)
        {
            b2 = 0;
            b3 = 0;
            switch (action)
            {
                case MultimediaAction.PlayPause:
                    if (reportId == 0) { b2 = 64; b3 = 0; }
                    else if (reportId == 2) { b2 = 0; b3 = 4; }
                    else { b2 = 0xCD; b3 = 0; }
                    break;
                case MultimediaAction.PrevTrack:
                    if (reportId == 0) { b2 = 128; b3 = 0; }
                    else if (reportId == 2) { b2 = 0; b3 = 11; }
                    else { b2 = 0xB6; b3 = 0; }
                    break;
                case MultimediaAction.NextTrack:
                    if (reportId == 0) { b2 = 0; b3 = 1; }
                    else if (reportId == 2) { b2 = 0; b3 = 10; }
                    else { b2 = 0xB5; b3 = 0; }
                    break;
                case MultimediaAction.Mute:
                    if (reportId == 0) { b2 = 4; b3 = 0; }
                    else if (reportId == 2) { b2 = 0; b3 = 1; }
                    else { b2 = 0xE2; b3 = 0; }
                    break;
                case MultimediaAction.VolumeUp:
                    if (reportId == 0) { b2 = 2; b3 = 0; }
                    else if (reportId == 2) { b2 = 64; b3 = 0; }
                    else { b2 = 0xE9; b3 = 0; }
                    break;
                case MultimediaAction.VolumeDown:
                    if (reportId == 0) { b2 = 1; b3 = 0; }
                    else if (reportId == 2) { b2 = 128; b3 = 0; }
                    else { b2 = 0xEA; b3 = 0; }
                    break;
            }
        }

        private static void PopulateMouseBytes(MouseActionType action, byte wheelModifier, out byte b2, out byte b3, out byte b4, out byte b5, out byte b6)
        {
            b2 = 0;
            b3 = 0;
            b4 = 0;
            b5 = 0;
            b6 = 0;
            switch (action)
            {
                case MouseActionType.LeftClick:
                    b2 = 1;
                    break;
                case MouseActionType.RightClick:
                    b2 = 2;
                    break;
                case MouseActionType.MiddleClick:
                    b2 = 4;
                    break;
                case MouseActionType.WheelUp:
                    b2 = 0;
                    b5 = 1;
                    b6 = wheelModifier;
                    break;
                case MouseActionType.WheelDown:
                    b2 = 0;
                    b5 = 0xFF;
                    b6 = wheelModifier;
                    break;
            }
        }

        public void Dispose()
        {
            CloseDevice();
        }
    }
}
