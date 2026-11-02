using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace MiniKeyboardCyber
{
    public class MainForm : Form
    {
        private readonly HidDeviceController _hid = new HidDeviceController();
        private readonly LayerProfile[] _layers = new LayerProfile[3];
        private int _currentLayerIdx = 0; // 0 -> Layer 1, 1 -> Layer 2, 2 -> Layer 3
        private byte _selectedSlotId = 1;
        private byte _activeModifierMask = 0;
        private bool _isCapturingPhysicalKey = false;
        private bool _suppressAliasEvent = false;

        // UI Controls
        private Panel _headerPanel;
        private Label _lblUsbStatus;
        private Button[] _btnLayers = new Button[3];
        private TextBox _txtProfileName;

        private KeypadPreviewControl _keypadPreview;
        private Label _lblStatusLog;
        private ProgressBar _progressBar;

        // Editor Panel Controls
        private Label _lblEditingSlotTitle;
        private Label _lblEditingSlotCategory;
        private TextBox _txtSlotAlias;
        private Label _lblSequencePreview;
        private Button _btnCapturePhysical;
        private readonly Dictionary<byte, Button> _modifierButtons = new Dictionary<byte, Button>();

        // Custom Tab Pages
        private Button[] _tabButtons = new Button[5];
        private Panel[] _tabPages = new Panel[5];
        private int _activeTabIdx = 0;

        // Mouse Wheel modifier radio buttons
        private byte _selectedWheelMod = 0;
        private readonly Dictionary<byte, Button> _wheelModButtons = new Dictionary<byte, Button>();

        // LED Mode buttons
        private readonly Dictionary<byte, Button> _ledModeButtons = new Dictionary<byte, Button>();

        private System.Windows.Forms.Timer _usbTimer;

        public LayerProfile CurrentProfile
        {
            get { return _layers[_currentLayerIdx]; }
        }

        public ButtonConfig SelectedSlot
        {
            get { return CurrentProfile.GetSlot(_selectedSlotId); }
        }

        public MainForm()
        {
            Text = "MINI KEYBOARD // Cyberpunk Control Studio (Español)";
            Size = new Size(1380, 880);
            MinimumSize = new Size(1240, 800);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = CyberTheme.BgDeep;
            ForeColor = CyberTheme.TextPrimary;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            KeyPreview = true;
            DoubleBuffered = true;
            try
            {
                Icon = BrandLogoRenderer.CreateRuntimeWindowIcon();
            }
            catch
            {
            }

            // Initialize 3 layers: Layer 1 pre-loaded with Productivity preset so user sees a rich example immediately
            _layers[0] = LayerProfile.CreatePresetProductivity(1);
            _layers[1] = LayerProfile.CreatePresetStreaming(2);
            _layers[2] = LayerProfile.CreateEmpty(3, "Capa 3 Personalizada");

            BuildUserInterface();
            SelectLayer(0);
            SelectSlot(1);
            SwitchTab(0);

            _usbTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _usbTimer.Tick += UsbTimer_Tick;
            _usbTimer.Start();
            UpdateUsbStatusBadge();
        }

        private void BuildUserInterface()
        {
            // 1. TOP CYBERPUNK HEADER BAR
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(12, 16, 26),
                Padding = new Padding(16, 8, 16, 8)
            };
            _headerPanel.Paint += (s, e) =>
            {
                using (Pen cyanLine = new Pen(CyberTheme.NeonCyan, 2f))
                {
                    e.Graphics.DrawLine(cyanLine, 0, _headerPanel.Height - 1, _headerPanel.Width, _headerPanel.Height - 1);
                }
                BrandLogoRenderer.DrawAcKeyboardEmblem(e.Graphics, new RectangleF(14f, 7f, 52f, 52f), false, true);
            };

            Label lblTitle = new Label
            {
                Text = "AC // MINI KEYBOARD STUDIO",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = CyberTheme.TextPrimary,
                Location = new Point(74, 8),
                AutoSize = true
            };

            Label lblSubtitle = new Label
            {
                Text = "ALAN CANTO · CONTROLADOR DE MACROS USB HID (12 TECLAS + 2 KNOBS)",
                Font = new Font("Consolas", 8.2f, FontStyle.Bold),
                ForeColor = CyberTheme.NeonCyan,
                Location = new Point(76, 39),
                AutoSize = true
            };

            _lblUsbStatus = new Label
            {
                Text = "◌ VERIFICANDO USB...",
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                ForeColor = CyberTheme.CyberYellow,
                BackColor = Color.FromArgb(22, 28, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(310, 28),
                Location = new Point(545, 19),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Layer selector buttons on top-right
            Label lblCapas = new Label
            {
                Text = "CAPA ACTIVA:",
                Font = new Font("Consolas", 8.5f, FontStyle.Bold),
                ForeColor = CyberTheme.TextSecondary,
                Location = new Point(870, 25),
                AutoSize = true
            };

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Button btnLayer = CreateCyberButton(
                    string.Format("CAPA {0}", i + 1),
                    new Point(965 + i * 82, 16),
                    new Size(76, 34),
                    CyberTheme.NeonCyan);
                btnLayer.Click += (s, e) => SelectLayer(idx);
                _btnLayers[i] = btnLayer;
                _headerPanel.Controls.Add(btnLayer);
            }

            Button btnSaveProfile = CreateCyberButton("⊕ GUARDAR", new Point(1205, 16), new Size(76, 34), CyberTheme.NeonGreen);
            btnSaveProfile.Font = new Font("Consolas", 7.8f, FontStyle.Bold);
            btnSaveProfile.Click += BtnSaveProfile_Click;

            Button btnLoadProfile = CreateCyberButton("▣ CARGAR", new Point(1285, 16), new Size(72, 34), CyberTheme.CyberYellow);
            btnLoadProfile.Font = new Font("Consolas", 7.8f, FontStyle.Bold);
            btnLoadProfile.Click += BtnLoadProfile_Click;

            _headerPanel.Controls.AddRange(new Control[] { lblTitle, lblSubtitle, _lblUsbStatus, lblCapas, btnSaveProfile, btnLoadProfile });
            Controls.Add(_headerPanel);

            // 2. MAIN SPLIT CONTAINER (LEFT = PREVIEW + BATCH DOWNLOAD, RIGHT = SLOT CONFIGURATION EDITOR)
            Panel mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 10, 14, 10),
                BackColor = CyberTheme.BgDeep
            };
            Controls.Add(mainContainer);
            mainContainer.BringToFront();

            // Left Panel (Width 615, initial height 774 matching client area)
            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Size = new Size(615, 774),
                BackColor = CyberTheme.BgDeep
            };

            // Profile Name & Preset Bar at top of Left Panel
            Panel profileBar = CreateStyledCardPanel(new Point(0, 0), new Size(615, 48));
            profileBar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Label lblProfName = new Label
            {
                Text = "NOMBRE DEL PERFIL:",
                Font = new Font("Consolas", 8.5f, FontStyle.Bold),
                ForeColor = CyberTheme.NeonCyan,
                Location = new Point(12, 16),
                AutoSize = true
            };
            _txtProfileName = new TextBox
            {
                Location = new Point(148, 12),
                Size = new Size(185, 24),
                BackColor = Color.FromArgb(12, 16, 26),
                ForeColor = CyberTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _txtProfileName.TextChanged += (s, e) =>
            {
                if (CurrentProfile != null) CurrentProfile.ProfileName = _txtProfileName.Text;
            };

            Button btnPresetProd = CreateCyberButton("⚡ Preset Dev", new Point(342, 9), new Size(124, 30), CyberTheme.NeonCyan);
            btnPresetProd.Font = new Font("Consolas", 7.8f, FontStyle.Bold);
            btnPresetProd.Click += (s, e) =>
            {
                _layers[_currentLayerIdx] = LayerProfile.CreatePresetProductivity((byte)(_currentLayerIdx + 1));
                RefreshAllUI("Plantilla 'Productividad & Dev Pro' cargada en memoria.");
            };

            Button btnPresetStream = CreateCyberButton("♫ Preset Stream", new Point(472, 9), new Size(132, 30), CyberTheme.NeonMagenta);
            btnPresetStream.Font = new Font("Consolas", 7.8f, FontStyle.Bold);
            btnPresetStream.Click += (s, e) =>
            {
                _layers[_currentLayerIdx] = LayerProfile.CreatePresetStreaming((byte)(_currentLayerIdx + 1));
                RefreshAllUI("Plantilla 'Streamer & Creator Deck' cargada en memoria.");
            };

            profileBar.Controls.AddRange(new Control[] { lblProfName, _txtProfileName, btnPresetProd, btnPresetStream });
            leftPanel.Controls.Add(profileBar);

            // Interactive Stylized Keypad Preview Control
            _keypadPreview = new KeypadPreviewControl
            {
                Location = new Point(0, 54),
                Size = new Size(615, 492),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            _keypadPreview.SlotSelected += (s, e) => SelectSlot(e.SlotId);
            leftPanel.Controls.Add(_keypadPreview);

            // Bottom Action & Batch Download Panel on Left
            Panel downloadCard = CreateStyledCardPanel(new Point(0, 552), new Size(615, 220));
            downloadCard.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            Label lblBatchInfo = new Label
            {
                Text = "◈ DESCARGA COMPLETA DEL PERFIL Y GENERACIÓN DE MAPA DE REFERENCIA",
                Font = new Font("Consolas", 8.8f, FontStyle.Bold),
                ForeColor = CyberTheme.CyberYellow,
                Location = new Point(14, 10),
                AutoSize = true
            };

            Label lblBatchSub = new Label
            {
                Text = "Configura todas las teclas y knobs en memoria antes de transferir. Al descargar el perfil se grabará en el teclado y se generará tu lámina PNG estilizada de referencia.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = CyberTheme.TextSecondary,
                Location = new Point(14, 29),
                Size = new Size(588, 34)
            };

            Button btnDownloadProfile = CreateCyberButton(
                "⚡ DESCARGAR PERFIL COMPLETO AL TECLADO + GUARDAR IMAGEN DE REFERENCIA",
                new Point(14, 66),
                new Size(587, 46),
                CyberTheme.NeonGreen);
            btnDownloadProfile.Font = new Font("Segoe UI", 10.2f, FontStyle.Bold);
            btnDownloadProfile.BackColor = Color.FromArgb(16, 48, 38);
            btnDownloadProfile.Click += BtnDownloadFullProfile_Click;

            Button btnDownloadSingle = CreateCyberButton(
                "▼ DESCARGAR SOLO TECLA ACTUAL",
                new Point(14, 120),
                new Size(205, 36),
                CyberTheme.NeonCyan);
            btnDownloadSingle.Font = new Font("Consolas", 8f, FontStyle.Bold);
            btnDownloadSingle.Click += BtnDownloadSingleSlot_Click;

            Button btnExportImageOnly = CreateCyberButton(
                "▣ GENERAR IMAGEN DE REFERENCIA (PNG)",
                new Point(227, 120),
                new Size(242, 36),
                CyberTheme.NeonMagenta);
            btnExportImageOnly.Font = new Font("Consolas", 8f, FontStyle.Bold);
            btnExportImageOnly.Click += BtnExportReferenceImage_Click;

            Button btnClearProfile = CreateCyberButton(
                "✖ VACIAR CAPA",
                new Point(477, 120),
                new Size(124, 36),
                CyberTheme.NeonOrange);
            btnClearProfile.Font = new Font("Consolas", 8f, FontStyle.Bold);
            btnClearProfile.Click += BtnClearProfile_Click;

            _progressBar = new ProgressBar
            {
                Location = new Point(14, 168),
                Size = new Size(587, 12),
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Style = ProgressBarStyle.Continuous
            };

            _lblStatusLog = new Label
            {
                Text = "✔ Listo. Selecciona cualquier tecla (1-12) o acción de Knob en la vista previa para configurar.",
                Font = new Font("Consolas", 8.5f, FontStyle.Regular),
                ForeColor = CyberTheme.NeonCyan,
                Location = new Point(14, 186),
                Size = new Size(587, 28),
                AutoEllipsis = true
            };

            downloadCard.Controls.AddRange(new Control[]
            {
                lblBatchInfo, lblBatchSub, btnDownloadProfile, btnDownloadSingle,
                btnExportImageOnly, btnClearProfile, _progressBar, _lblStatusLog
            });
            leftPanel.Controls.Add(downloadCard);

            // Right Panel: Slot Editor + Virtual Keyboards/Tabs
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Size = new Size(730, 774),
                Padding = new Padding(14, 0, 0, 0),
                BackColor = CyberTheme.BgDeep
            };

            BuildRightEditorPanel(rightPanel);

            mainContainer.Controls.Add(rightPanel);
            mainContainer.Controls.Add(leftPanel);
        }

        private void BuildRightEditorPanel(Panel parent)
        {
            // 1. Selected Slot Inspector Card (Top of Right Panel)
            Panel inspectorCard = CreateStyledCardPanel(new Point(14, 0), new Size(710, 175));
            inspectorCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            _lblEditingSlotTitle = new Label
            {
                Text = "EDITANDO: TECLA 01",
                Font = new Font("Segoe UI", 13.5f, FontStyle.Bold),
                ForeColor = CyberTheme.NeonCyan,
                Location = new Point(14, 10),
                AutoSize = true
            };

            _lblEditingSlotCategory = new Label
            {
                Text = "[TECLADO]",
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                ForeColor = CyberTheme.CyberYellow,
                Location = new Point(280, 14),
                AutoSize = true
            };

            Button btnPrevSlot = CreateCyberButton("◀ ANTERIOR", new Point(495, 10), new Size(98, 28), CyberTheme.TextSecondary);
            btnPrevSlot.Font = new Font("Consolas", 8f, FontStyle.Bold);
            btnPrevSlot.Click += (s, e) => SelectSlot((byte)(_selectedSlotId > 1 ? _selectedSlotId - 1 : 18));

            Button btnNextSlot = CreateCyberButton("SIGUIENTE ▶", new Point(598, 10), new Size(98, 28), CyberTheme.TextSecondary);
            btnNextSlot.Font = new Font("Consolas", 8f, FontStyle.Bold);
            btnNextSlot.Click += (s, e) => SelectSlot((byte)(_selectedSlotId < 18 ? _selectedSlotId + 1 : 1));

            Label lblAlias = new Label
            {
                Text = "ETIQUETA / NOMBRE EN MAPA:",
                Font = new Font("Consolas", 8.5f, FontStyle.Bold),
                ForeColor = CyberTheme.TextSecondary,
                Location = new Point(14, 48),
                AutoSize = true
            };

            _txtSlotAlias = new TextBox
            {
                Location = new Point(205, 45),
                Size = new Size(255, 24),
                BackColor = Color.FromArgb(10, 14, 22),
                ForeColor = CyberTheme.CyberYellow,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _txtSlotAlias.TextChanged += (s, e) =>
            {
                if (_suppressAliasEvent) return;
                if (SelectedSlot != null)
                {
                    SelectedSlot.CustomAlias = _txtSlotAlias.Text.Trim();
                    _keypadPreview.Invalidate();
                }
            };

            _btnCapturePhysical = CreateCyberButton("🎙️ CAPTURAR TECLADO FÍSICO: OFF", new Point(470, 43), new Size(226, 28), CyberTheme.NeonMagenta);
            _btnCapturePhysical.Font = new Font("Consolas", 8f, FontStyle.Bold);
            _btnCapturePhysical.Click += (s, e) => TogglePhysicalCapture(!_isCapturingPhysicalKey);

            // Current Sequence Display Box
            Label lblSeqTitle = new Label
            {
                Text = "ACCIÓN / SECUENCIA (MÁX 5 PASOS):",
                Font = new Font("Consolas", 8.5f, FontStyle.Bold),
                ForeColor = CyberTheme.TextSecondary,
                Location = new Point(14, 83),
                AutoSize = true
            };

            _lblSequencePreview = new Label
            {
                Text = "Ctrl + C",
                Font = new Font("Consolas", 11f, FontStyle.Bold),
                ForeColor = CyberTheme.NeonGreen,
                BackColor = Color.FromArgb(10, 14, 22),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 8, 0),
                Location = new Point(14, 102),
                Size = new Size(446, 32)
            };

            Button btnBackspaceStep = CreateCyberButton("⌫ BORRAR PASO", new Point(470, 102), new Size(112, 32), CyberTheme.CyberYellow);
            btnBackspaceStep.Font = new Font("Consolas", 8f, FontStyle.Bold);
            btnBackspaceStep.Click += BtnBackspaceStep_Click;

            Button btnClearSlot = CreateCyberButton("✖ LIMPIAR TECLA", new Point(586, 102), new Size(110, 32), CyberTheme.NeonOrange);
            btnClearSlot.Font = new Font("Consolas", 8f, FontStyle.Bold);
            btnClearSlot.Click += (s, e) =>
            {
                SelectedSlot.Clear();
                RefreshSlotInspector("Tecla limpiada en memoria.");
            };

            // Modifiers Row inside Inspector Card
            Label lblMods = new Label
            {
                Text = "MODIFICADORES:",
                Font = new Font("Consolas", 8.2f, FontStyle.Bold),
                ForeColor = CyberTheme.NeonCyan,
                Location = new Point(14, 145),
                AutoSize = true
            };
            inspectorCard.Controls.Add(lblMods);

            var modDefs = new[]
            {
                new { Mask = HidKeyMap.MOD_LCTRL, Name = "Ctrl" },
                new { Mask = HidKeyMap.MOD_LSHIFT, Name = "Shift" },
                new { Mask = HidKeyMap.MOD_LALT, Name = "Alt" },
                new { Mask = HidKeyMap.MOD_LWIN, Name = "Win" },
                new { Mask = HidKeyMap.MOD_RCTRL, Name = "R-Ctrl" },
                new { Mask = HidKeyMap.MOD_RSHIFT, Name = "R-Shift" },
                new { Mask = HidKeyMap.MOD_RALT, Name = "R-Alt" },
                new { Mask = HidKeyMap.MOD_RWIN, Name = "R-Win" }
            };

            int modX = 122;
            foreach (var md in modDefs)
            {
                byte mask = md.Mask;
                Button btnMod = CreateCyberButton(md.Name, new Point(modX, 140), new Size(56, 26), CyberTheme.NeonCyan);
                btnMod.Font = new Font("Consolas", 7.8f, FontStyle.Bold);
                btnMod.Click += (s, e) => ToggleModifierMask(mask);
                _modifierButtons[mask] = btnMod;
                inspectorCard.Controls.Add(btnMod);
                modX += 59;
            }

            Button btnApplyPureMod = CreateCyberButton("➕ Solo Mod.", new Point(modX + 4, 140), new Size(96, 26), CyberTheme.NeonMagenta);
            btnApplyPureMod.Font = new Font("Consolas", 7.8f, FontStyle.Bold);
            btnApplyPureMod.Click += (s, e) =>
            {
                if (_activeModifierMask == 0)
                {
                    SetStatus("Activa al menos un modificador (Ctrl, Shift, Alt, Win) primero.");
                    return;
                }
                SelectedSlot.Category = ActionCategory.Keyboard;
                SelectedSlot.Steps.Clear();
                SelectedSlot.StandaloneModifierMask = _activeModifierMask;
                RefreshSlotInspector("Asignado modificador puro a " + SelectedSlot.SlotName);
            };
            inspectorCard.Controls.Add(btnApplyPureMod);

            inspectorCard.Controls.AddRange(new Control[]
            {
                _lblEditingSlotTitle, _lblEditingSlotCategory, btnPrevSlot, btnNextSlot,
                lblAlias, _txtSlotAlias, _btnCapturePhysical, lblSeqTitle,
                _lblSequencePreview, btnBackspaceStep, btnClearSlot
            });
            parent.Controls.Add(inspectorCard);

            // 2. Cyber Tab Selector Bar
            string[] tabTitles = new[]
            {
                "⌨ TECLADO VIRTUAL",
                "⚡ ATAJOS Y SÍMBOLOS",
                "♫ MULTIMEDIA",
                "◉ RATÓN Y RUEDA",
                "☼ ILUMINACIÓN LED"
            };

            for (int i = 0; i < 5; i++)
            {
                int tabIdx = i;
                Button btnTab = CreateCyberButton(
                    tabTitles[i],
                    new Point(14 + i * 143, 184),
                    new Size(139, 36),
                    CyberTheme.NeonCyan);
                btnTab.Font = new Font("Consolas", 8.2f, FontStyle.Bold);
                btnTab.Click += (s, e) => SwitchTab(tabIdx);
                _tabButtons[i] = btnTab;
                parent.Controls.Add(btnTab);

                Panel page = CreateStyledCardPanel(new Point(14, 224), new Size(710, 553));
                page.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                page.Visible = (i == 0);
                _tabPages[i] = page;
                parent.Controls.Add(page);
            }

            PopulateVirtualKeyboardTab(_tabPages[0]);
            PopulateShortcutsAndSymbolsTab(_tabPages[1]);
            PopulateMultimediaTab(_tabPages[2]);
            PopulateMouseTab(_tabPages[3]);
            PopulateLedTab(_tabPages[4]);
        }

        private void PopulateVirtualKeyboardTab(Panel page)
        {
            Label lblHint = new Label
            {
                Text = "Haz clic en cualquier tecla para añadirla a la secuencia (combínala con los botones Ctrl / Shift / Alt / Win superiores):",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                ForeColor = CyberTheme.TextSecondary,
                Location = new Point(12, 10),
                AutoSize = true
            };
            page.Controls.Add(lblHint);

            // Row 1: ESC + F1..F12
            var rowF = new[]
            {
                new HidKeyDefinition(41, "ESC", "ESC", 1.15f),
                new HidKeyDefinition(58, "F1"), new HidKeyDefinition(59, "F2"), new HidKeyDefinition(60, "F3"), new HidKeyDefinition(61, "F4"),
                new HidKeyDefinition(62, "F5"), new HidKeyDefinition(63, "F6"), new HidKeyDefinition(64, "F7"), new HidKeyDefinition(65, "F8"),
                new HidKeyDefinition(66, "F9"), new HidKeyDefinition(67, "F10"), new HidKeyDefinition(68, "F11"), new HidKeyDefinition(69, "F12")
            };
            AddKeyboardRow(page, rowF, 12, 36, 48, 34, 4, CyberTheme.NeonMagenta);

            // Row 2: ` 1 2 3 4 5 6 7 8 9 0 - = Backspace
            var rowDigits = new[]
            {
                new HidKeyDefinition(53, "`", "~ `", 0.9f),
                new HidKeyDefinition(30, "1"), new HidKeyDefinition(31, "2"), new HidKeyDefinition(32, "3"), new HidKeyDefinition(33, "4"),
                new HidKeyDefinition(34, "5"), new HidKeyDefinition(35, "6"), new HidKeyDefinition(36, "7"), new HidKeyDefinition(37, "8"),
                new HidKeyDefinition(38, "9"), new HidKeyDefinition(39, "0"),
                new HidKeyDefinition(45, "-", "_ -"), new HidKeyDefinition(46, "=", "+ ="),
                new HidKeyDefinition(42, "BackSpace", "⌫ Borrar", 1.65f)
            };
            AddKeyboardRow(page, rowDigits, 12, 78, 43, 36, 4, CyberTheme.NeonCyan);

            // Row 3: Tab Q W E R T Y U I O P [ ] \
            var rowQwerty = new[]
            {
                new HidKeyDefinition(43, "Tab", "Tab ⇥", 1.3f),
                new HidKeyDefinition(20, "Q"), new HidKeyDefinition(26, "W"), new HidKeyDefinition(8, "E"), new HidKeyDefinition(21, "R"),
                new HidKeyDefinition(23, "T"), new HidKeyDefinition(28, "Y"), new HidKeyDefinition(24, "U"), new HidKeyDefinition(12, "I"),
                new HidKeyDefinition(18, "O"), new HidKeyDefinition(19, "P"),
                new HidKeyDefinition(47, "[", "{ ["), new HidKeyDefinition(48, "]", "} ]"), new HidKeyDefinition(49, "\\", "| \\", 1.2f)
            };
            AddKeyboardRow(page, rowQwerty, 12, 120, 43, 36, 4, CyberTheme.NeonCyan);

            // Row 4: Caps A S D F G H J K L ; ' Enter
            var rowAsdf = new[]
            {
                new HidKeyDefinition(57, "CapsLock", "Mayús", 1.5f),
                new HidKeyDefinition(4, "A"), new HidKeyDefinition(22, "S"), new HidKeyDefinition(7, "D"), new HidKeyDefinition(9, "F"),
                new HidKeyDefinition(10, "G"), new HidKeyDefinition(11, "H"), new HidKeyDefinition(13, "J"), new HidKeyDefinition(14, "K"),
                new HidKeyDefinition(15, "L"), new HidKeyDefinition(51, ";", ": ;"), new HidKeyDefinition(52, "'", "\" '"),
                new HidKeyDefinition(40, "Enter", "↵ Enter", 1.85f)
            };
            AddKeyboardRow(page, rowAsdf, 12, 162, 43, 36, 4, CyberTheme.NeonCyan);

            // Row 5: Z X C V B N M , . / Space Menu
            var rowZxcv = new[]
            {
                new HidKeyDefinition(29, "Z"), new HidKeyDefinition(27, "X"), new HidKeyDefinition(6, "C"), new HidKeyDefinition(25, "V"),
                new HidKeyDefinition(5, "B"), new HidKeyDefinition(17, "N"), new HidKeyDefinition(16, "M"),
                new HidKeyDefinition(54, ",", "< ,"), new HidKeyDefinition(55, ".", "> ."), new HidKeyDefinition(56, "/", "? /"),
                new HidKeyDefinition(44, "Space", "⎵ ESPACIO", 2.95f),
                new HidKeyDefinition(101, "Menu", "☰ Menú", 1.35f)
            };
            AddKeyboardRow(page, rowZxcv, 12, 204, 43, 36, 4, CyberTheme.NeonCyan);

            // Divider: Navigation & Numpad Cluster
            Label lblNav = new Label
            {
                Text = "◈ NAVEGACIÓN, EDICIÓN Y TECLADO NUMÉRICO (NUMPAD)",
                Font = new Font("Consolas", 8.8f, FontStyle.Bold),
                ForeColor = CyberTheme.CyberYellow,
                Location = new Point(12, 254),
                AutoSize = true
            };
            page.Controls.Add(lblNav);

            // Navigation cluster on Left (x=12..320)
            var navRow1 = new[]
            {
                new HidKeyDefinition(70, "PrtSc", "ImpPnt", 1.2f),
                new HidKeyDefinition(71, "ScLock", "BloqDes", 1.2f),
                new HidKeyDefinition(72, "PaBk", "Pausa", 1.2f)
            };
            AddKeyboardRow(page, navRow1, 12, 278, 72, 36, 6, CyberTheme.NeonPurple);

            var navRow2 = new[]
            {
                new HidKeyDefinition(73, "Ins", "Insert", 1.2f),
                new HidKeyDefinition(74, "Home", "Inicio", 1.2f),
                new HidKeyDefinition(75, "PageUp", "RePág", 1.2f)
            };
            AddKeyboardRow(page, navRow2, 12, 320, 72, 36, 6, CyberTheme.NeonPurple);

            var navRow3 = new[]
            {
                new HidKeyDefinition(76, "Delete", "Supr", 1.2f),
                new HidKeyDefinition(77, "End", "Fin", 1.2f),
                new HidKeyDefinition(78, "PageDown", "AvPág", 1.2f)
            };
            AddKeyboardRow(page, navRow3, 12, 362, 72, 36, 6, CyberTheme.NeonPurple);

            // Arrow Keys
            AddSingleVirtualKey(page, new HidKeyDefinition(82, "↑", "▲ Arriba"), new Rectangle(104, 412, 84, 38), CyberTheme.NeonGreen);
            AddSingleVirtualKey(page, new HidKeyDefinition(80, "←", "◀ Izq"), new Rectangle(12, 456, 84, 38), CyberTheme.NeonGreen);
            AddSingleVirtualKey(page, new HidKeyDefinition(81, "↓", "▼ Abajo"), new Rectangle(104, 456, 84, 38), CyberTheme.NeonGreen);
            AddSingleVirtualKey(page, new HidKeyDefinition(79, "→", "▶ Der"), new Rectangle(196, 456, 84, 38), CyberTheme.NeonGreen);

            // Numpad cluster on Right (x=335..695)
            var numRow1 = new[]
            {
                new HidKeyDefinition(83, "NumLock", "NumLk"),
                new HidKeyDefinition(84, "Num/", "KP /"),
                new HidKeyDefinition(85, "Num*", "KP *"),
                new HidKeyDefinition(86, "Num-", "KP -")
            };
            AddKeyboardRow(page, numRow1, 345, 278, 80, 36, 6, CyberTheme.CyberYellow);

            var numRow2 = new[]
            {
                new HidKeyDefinition(95, "Num7", "KP 7"),
                new HidKeyDefinition(96, "Num8", "KP 8"),
                new HidKeyDefinition(97, "Num9", "KP 9"),
                new HidKeyDefinition(87, "Num+", "KP +")
            };
            AddKeyboardRow(page, numRow2, 345, 320, 80, 36, 6, CyberTheme.CyberYellow);

            var numRow3 = new[]
            {
                new HidKeyDefinition(92, "Num4", "KP 4"),
                new HidKeyDefinition(93, "Num5", "KP 5"),
                new HidKeyDefinition(94, "Num6", "KP 6"),
                new HidKeyDefinition(40, "Enter", "KP Intro")
            };
            AddKeyboardRow(page, numRow3, 345, 362, 80, 36, 6, CyberTheme.CyberYellow);

            var numRow4 = new[]
            {
                new HidKeyDefinition(89, "Num1", "KP 1"),
                new HidKeyDefinition(90, "Num2", "KP 2"),
                new HidKeyDefinition(91, "Num3", "KP 3"),
                new HidKeyDefinition(98, "Num0", "KP 0")
            };
            AddKeyboardRow(page, numRow4, 345, 404, 80, 36, 6, CyberTheme.CyberYellow);

            var numRow5 = new[]
            {
                new HidKeyDefinition(99, "Num.", "KP . (Decimal)", 2.0f)
            };
            AddKeyboardRow(page, numRow5, 345, 446, 80, 36, 6, CyberTheme.CyberYellow);
        }

        private void AddKeyboardRow(Panel parent, HidKeyDefinition[] keys, int startX, int y, int baseW, int h, int gap, Color accent)
        {
            int curX = startX;
            foreach (var k in keys)
            {
                int w = (int)Math.Round(baseW * k.WidthWeight);
                AddSingleVirtualKey(parent, k, new Rectangle(curX, y, w, h), accent);
                curX += w + gap;
            }
        }

        private void AddSingleVirtualKey(Panel parent, HidKeyDefinition k, Rectangle bounds, Color accent)
        {
            Button btn = CreateCyberButton(k.DisplayText, bounds.Location, bounds.Size, accent);
            btn.Font = new Font("Consolas", 8.2f, FontStyle.Bold);
            btn.Click += (s, e) => AppendKeyStep(_activeModifierMask, k.HidCode, k.Label);
            parent.Controls.Add(btn);
        }

        private void PopulateShortcutsAndSymbolsTab(Panel page)
        {
            Label lblHeader = new Label
            {
                Text = "◈ ATAJOS RÁPIDOS DE 1 CLIC (ASIGNAN LA COMBINACIÓN Y LA ETIQUETA AUTOMÁTICAMENTE)",
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                ForeColor = CyberTheme.NeonCyan,
                Location = new Point(14, 12),
                AutoSize = true
            };
            page.Controls.Add(lblHeader);

            var presets = new[]
            {
                new { Title = "◈ Copiar (Ctrl+C)", Alias = "Copiar", Mod = (byte)0x01, Code = (byte)6, Key = "C" },
                new { Title = "◈ Pegar (Ctrl+V)", Alias = "Pegar", Mod = (byte)0x01, Code = (byte)25, Key = "V" },
                new { Title = "✂ Cortar (Ctrl+X)", Alias = "Cortar", Mod = (byte)0x01, Code = (byte)27, Key = "X" },
                new { Title = "↩ Deshacer (Ctrl+Z)", Alias = "Deshacer", Mod = (byte)0x01, Code = (byte)29, Key = "Z" },
                new { Title = "↪ Rehacer (Ctrl+Y)", Alias = "Rehacer", Mod = (byte)0x01, Code = (byte)28, Key = "Y" },
                new { Title = "⊕ Guardar (Ctrl+S)", Alias = "Guardar", Mod = (byte)0x01, Code = (byte)22, Key = "S" },
                new { Title = "◉ Buscar (Ctrl+F)", Alias = "Buscar", Mod = (byte)0x01, Code = (byte)9, Key = "F" },
                new { Title = "✔ Seleccionar Todo", Alias = "Selec. Todo", Mod = (byte)0x01, Code = (byte)4, Key = "A" },
                new { Title = "▣ Captura (Win+Shift+S)", Alias = "Captura Pantalla", Mod = (byte)0x0A, Code = (byte)22, Key = "S" },
                new { Title = "⇄ Cambiar App (Alt+Tab)", Alias = "Cambiar App", Mod = (byte)0x04, Code = (byte)43, Key = "Tab" },
                new { Title = "✖ Cerrar App (Alt+F4)", Alias = "Cerrar Ventana", Mod = (byte)0x04, Code = (byte)61, Key = "F4" },
                new { Title = "⚡ Admin. Tareas", Alias = "Admin. Tareas", Mod = (byte)0x03, Code = (byte)41, Key = "ESC" },
                new { Title = "▣ Escritorio (Win+D)", Alias = "Escritorio", Mod = (byte)0x08, Code = (byte)7, Key = "D" },
                new { Title = "▤ Explorador (Win+E)", Alias = "Archivos", Mod = (byte)0x08, Code = (byte)8, Key = "E" },
                new { Title = "◈ Bloquear PC (Win+L)", Alias = "Bloquear PC", Mod = (byte)0x08, Code = (byte)15, Key = "L" },
                new { Title = "► Ejecutar (Win+R)", Alias = "Ejecutar", Mod = (byte)0x08, Code = (byte)21, Key = "R" },
                new { Title = "♫ Mute Discord (Ctrl+Shift+M)", Alias = "Mute Discord", Mod = (byte)0x03, Code = (byte)16, Key = "M" },
                new { Title = "➕ Nueva Pestaña (Ctrl+T)", Alias = "Nueva Pestaña", Mod = (byte)0x01, Code = (byte)23, Key = "T" },
                new { Title = "✖ Cerrar Pestaña (Ctrl+W)", Alias = "Cerrar Pestaña", Mod = (byte)0x01, Code = (byte)26, Key = "W" },
                new { Title = "◄ Reabrir Pestaña", Alias = "Reabrir Pestaña", Mod = (byte)0x03, Code = (byte)23, Key = "T" }
            };

            int cols = 4;
            int cardW = 164;
            int cardH = 42;
            int gapX = 8;
            int gapY = 8;

            for (int i = 0; i < presets.Length; i++)
            {
                var p = presets[i];
                int r = i / cols;
                int c = i % cols;
                Button btn = CreateCyberButton(
                    p.Title,
                    new Point(14 + c * (cardW + gapX), 38 + r * (cardH + gapY)),
                    new Size(cardW, cardH),
                    CyberTheme.NeonCyan);
                btn.Font = new Font("Segoe UI", 8.2f, FontStyle.Bold);
                btn.Click += (s, e) =>
                {
                    SelectedSlot.Clear();
                    SelectedSlot.Category = ActionCategory.Keyboard;
                    SelectedSlot.CustomAlias = p.Alias;
                    SelectedSlot.Steps.Add(new KeyStrokeStep(p.Mod, p.Code, p.Key));
                    RefreshSlotInspector("Atajo rápido '" + p.Alias + "' asignado a " + SelectedSlot.SlotName);
                };
                page.Controls.Add(btn);
            }

            // Shifted Symbols section (matching original FunKey form)
            Label lblSymbols = new Label
            {
                Text = "◈ SÍMBOLOS DIRECTOS CON SHIFT AUTOMÁTICO (AÑADEN EL SÍMBOLO A LA SECUENCIA)",
                Font = new Font("Consolas", 9f, FontStyle.Bold),
                ForeColor = CyberTheme.NeonMagenta,
                Location = new Point(14, 302),
                AutoSize = true
            };
            page.Controls.Add(lblSymbols);

            var shiftedSymbols = new[]
            {
                new { Sym = "~", Code = (byte)53 },
                new { Sym = "!", Code = (byte)30 },
                new { Sym = "@", Code = (byte)31 },
                new { Sym = "#", Code = (byte)32 },
                new { Sym = "$", Code = (byte)33 },
                new { Sym = "%", Code = (byte)34 },
                new { Sym = "^", Code = (byte)35 },
                new { Sym = "&", Code = (byte)36 },
                new { Sym = "*", Code = (byte)37 },
                new { Sym = "(", Code = (byte)38 },
                new { Sym = ")", Code = (byte)39 },
                new { Sym = "_", Code = (byte)45 },
                new { Sym = "+", Code = (byte)46 },
                new { Sym = "{", Code = (byte)47 },
                new { Sym = "}", Code = (byte)48 },
                new { Sym = "|", Code = (byte)49 },
                new { Sym = ":", Code = (byte)51 },
                new { Sym = "\"", Code = (byte)52 },
                new { Sym = "<", Code = (byte)54 },
                new { Sym = ">", Code = (byte)55 },
                new { Sym = "?", Code = (byte)56 }
            };

            int symCols = 7;
            int symW = 90;
            int symH = 42;
            for (int i = 0; i < shiftedSymbols.Length; i++)
            {
                var item = shiftedSymbols[i];
                int r = i / symCols;
                int c = i % symCols;
                Button btnSym = CreateCyberButton(
                    item.Sym,
                    new Point(14 + c * (symW + 8), 330 + r * (symH + 8)),
                    new Size(symW, symH),
                    CyberTheme.NeonMagenta);
                btnSym.Font = new Font("Consolas", 11f, FontStyle.Bold);
                btnSym.Click += (s, e) =>
                {
                    AppendKeyStep((byte)(_activeModifierMask | HidKeyMap.MOD_LSHIFT), item.Code, item.Sym);
                };
                page.Controls.Add(btnSym);
            }
        }

        private void PopulateMultimediaTab(Panel page)
        {
            Label lblInfo = new Label
            {
                Text = "◈ CONTROLES MULTIMEDIA Y DE AUDIO (IDEALES PARA LOS KNOBS ROTATORIOS O TECLAS DEDICADAS)",
                Font = new Font("Consolas", 9.2f, FontStyle.Bold),
                ForeColor = CyberTheme.NeonMagenta,
                Location = new Point(16, 16),
                AutoSize = true
            };
            page.Controls.Add(lblInfo);

            var mediaItems = new[]
            {
                new { Action = MultimediaAction.PlayPause, Title = "► REPRODUCIR / PAUSAR (PLAY/PAUSE)", Desc = "Alterna reproducción o pausa en Spotify, YouTube y reproductores del sistema.", Alias = "Play / Pausa" },
                new { Action = MultimediaAction.PrevTrack, Title = "◄◄ PISTA ANTERIOR (PREV)", Desc = "Salta a la canción o capítulo anterior inmediatamente.", Alias = "Pista Anterior" },
                new { Action = MultimediaAction.NextTrack, Title = "►► PISTA SIGUIENTE (NEXT)", Desc = "Avanza a la siguiente canción o pista multimedia.", Alias = "Pista Siguiente" },
                new { Action = MultimediaAction.Mute, Title = "✖ SILENCIAR AUDIO (MUTE)", Desc = "Silencia o reactiva el audio general de Windows al pulsar (ideal para Click de Knob).", Alias = "Silenciar Audio" },
                new { Action = MultimediaAction.VolumeUp, Title = "▲ SUBIR VOLUMEN (VOL +)", Desc = "Incrementa el volumen maestro del sistema (ideal para Giro Derecha de Knob).", Alias = "Subir Volumen" },
                new { Action = MultimediaAction.VolumeDown, Title = "▼ BAJAR VOLUMEN (VOL -)", Desc = "Reduce el volumen maestro del sistema (ideal para Giro Izquierda de Knob).", Alias = "Bajar Volumen" }
            };

            int cols = 2;
            int cardW = 330;
            int cardH = 115;
            for (int i = 0; i < mediaItems.Length; i++)
            {
                var m = mediaItems[i];
                int r = i / cols;
                int c = i % cols;
                Button btn = CreateCyberButton(
                    m.Title + "\n\n" + m.Desc,
                    new Point(16 + c * (cardW + 16), 52 + r * (cardH + 16)),
                    new Size(cardW, cardH),
                    CyberTheme.NeonMagenta);
                btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.Padding = new Padding(12);
                btn.Click += (s, e) =>
                {
                    SelectedSlot.Clear();
                    SelectedSlot.Category = ActionCategory.Multimedia;
                    SelectedSlot.MediaAction = m.Action;
                    SelectedSlot.CustomAlias = m.Alias;
                    RefreshSlotInspector("Función multimedia '" + m.Alias + "' asignada a " + SelectedSlot.SlotName);
                };
                page.Controls.Add(btn);
            }
        }

        private void PopulateMouseTab(Panel page)
        {
            Label lblMouse = new Label
            {
                Text = "◈ ACCIONES DE RATÓN (CLICS Y DESPLAZAMIENTO DE RUEDA / ZOOM)",
                Font = new Font("Consolas", 9.2f, FontStyle.Bold),
                ForeColor = CyberTheme.CyberYellow,
                Location = new Point(16, 16),
                AutoSize = true
            };
            page.Controls.Add(lblMouse);

            // Wheel Modifier Selector
            Label lblWheelMod = new Label
            {
                Text = "MODIFICADOR PARA GIRO DE RUEDA (SCROLL / ZOOM):",
                Font = new Font("Consolas", 8.8f, FontStyle.Bold),
                ForeColor = CyberTheme.NeonCyan,
                Location = new Point(16, 48),
                AutoSize = true
            };
            page.Controls.Add(lblWheelMod);

            var wheelMods = new[]
            {
                new { Mod = (byte)0, Label = "Sin Modificador (Scroll Normal)" },
                new { Mod = (byte)1, Label = "Ctrl + Rueda (Zoom + / -)" },
                new { Mod = (byte)2, Label = "Shift + Rueda (Scroll Horiz.)" },
                new { Mod = (byte)4, Label = "Alt + Rueda" }
            };

            for (int i = 0; i < wheelMods.Length; i++)
            {
                var wm = wheelMods[i];
                Button btnWm = CreateCyberButton(
                    wm.Label,
                    new Point(16 + i * 170, 74),
                    new Size(162, 34),
                    CyberTheme.NeonCyan);
                btnWm.Font = new Font("Consolas", 7.8f, FontStyle.Bold);
                btnWm.Click += (s, e) => SelectWheelModifier(wm.Mod);
                _wheelModButtons[wm.Mod] = btnWm;
                page.Controls.Add(btnWm);
            }
            SelectWheelModifier(0);

            var mouseActions = new[]
            {
                new { Act = MouseActionType.LeftClick, Title = "🖱 CLICK IZQUIERDO", Desc = "Dispara un clic izquierdo estándar de ratón.", Alias = "Click Izquierdo" },
                new { Act = MouseActionType.RightClick, Title = "🖱 CLICK DERECHO", Desc = "Abre el menú contextual (clic secundario).", Alias = "Click Derecho" },
                new { Act = MouseActionType.MiddleClick, Title = "🖱 CLICK CENTRAL (RUEDA)", Desc = "Clic central para abrir enlaces o mover lienzos 3D/CAD.", Alias = "Click Central" },
                new { Act = MouseActionType.WheelUp, Title = "🔼 RUEDA ARRIBA (SCROLL +)", Desc = "Desplaza hacia arriba o acerca el Zoom si Ctrl está activo.", Alias = "Rueda Arriba" },
                new { Act = MouseActionType.WheelDown, Title = "🔽 RUEDA ABAJO (SCROLL -)", Desc = "Desplaza hacia abajo o aleja el Zoom si Ctrl está activo.", Alias = "Rueda Abajo" }
            };

            for (int i = 0; i < mouseActions.Length; i++)
            {
                var ma = mouseActions[i];
                int r = i / 2;
                int c = i % 2;
                Button btnMa = CreateCyberButton(
                    ma.Title + "\n\n" + ma.Desc,
                    new Point(16 + c * 342, 128 + r * 115),
                    new Size(328, 102),
                    CyberTheme.CyberYellow);
                btnMa.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                btnMa.TextAlign = ContentAlignment.MiddleLeft;
                btnMa.Padding = new Padding(12);
                btnMa.Click += (s, e) =>
                {
                    SelectedSlot.Clear();
                    SelectedSlot.Category = ActionCategory.Mouse;
                    SelectedSlot.MouseAction = ma.Act;
                    SelectedSlot.MouseWheelModifier = (ma.Act == MouseActionType.WheelUp || ma.Act == MouseActionType.WheelDown) ? _selectedWheelMod : (byte)0;

                    string alias = ma.Alias;
                    if (SelectedSlot.MouseWheelModifier == 1 && ma.Act == MouseActionType.WheelUp) alias = "Zoom + (Acercar)";
                    if (SelectedSlot.MouseWheelModifier == 1 && ma.Act == MouseActionType.WheelDown) alias = "Zoom - (Alejar)";
                    SelectedSlot.CustomAlias = alias;

                    RefreshSlotInspector("Acción de ratón '" + alias + "' asignada a " + SelectedSlot.SlotName);
                };
                page.Controls.Add(btnMa);
            }
        }

        private void PopulateLedTab(Panel page)
        {
            Label lblLed = new Label
            {
                Text = "◈ CONFIGURACIÓN DE ILUMINACIÓN LED DEL MINITECLADO",
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                ForeColor = CyberTheme.NeonPurple,
                Location = new Point(16, 16),
                AutoSize = true
            };

            Label lblLedDesc = new Label
            {
                Text = "Selecciona el modo de retroiluminación RGB del hardware. Se descargará junto con el perfil o puedes aplicarlo de inmediato:",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = CyberTheme.TextSecondary,
                Location = new Point(16, 40),
                Size = new Size(660, 26)
            };
            page.Controls.AddRange(new Control[] { lblLed, lblLedDesc });

            var modes = new[]
            {
                new { Mode = (byte)0, Title = "🌑 MODO 0 · ILUMINACIÓN LED APAGADA", Desc = "Desactiva todos los LEDs del miniteclado para entornos oscuros o ahorro de energía." },
                new { Mode = (byte)1, Title = "💠 MODO 1 · ILUMINACIÓN FIJA / REACTIVA", Desc = "Mantiene los LEDs encendidos con respuesta reactiva al presionar las teclas." },
                new { Mode = (byte)2, Title = "🌈 MODO 2 · EFECTO RESPIRACIÓN DINÁMICA", Desc = "Ciclo continuo de iluminación estilo Cyberpunk en todo el chasis acrílico." }
            };

            for (int i = 0; i < modes.Length; i++)
            {
                var m = modes[i];
                Button btnMode = CreateCyberButton(
                    m.Title + "\n" + m.Desc,
                    new Point(16, 76 + i * 96),
                    new Size(670, 82),
                    CyberTheme.NeonPurple);
                btnMode.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                btnMode.TextAlign = ContentAlignment.MiddleLeft;
                btnMode.Padding = new Padding(16, 8, 16, 8);
                btnMode.Click += (s, e) =>
                {
                    CurrentProfile.LedMode = m.Mode;
                    UpdateLedModeButtons();
                    _keypadPreview.Invalidate();
                    SetStatus("Modo LED seleccionado en el perfil: " + CurrentProfile.GetLedModeDescription());
                };
                _ledModeButtons[m.Mode] = btnMode;
                page.Controls.Add(btnMode);
            }

            Button btnApplyLedNow = CreateCyberButton(
                "⚡ ENVIAR MODO LED AL TECLADO FÍSICO AHORA",
                new Point(16, 376),
                new Size(670, 46),
                CyberTheme.NeonGreen);
            btnApplyLedNow.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            btnApplyLedNow.Click += (s, e) =>
            {
                if (_hid.DownloadLedMode(CurrentProfile.LayerIndex, CurrentProfile.LedMode))
                {
                    SetStatus("✔ Modo LED (" + CurrentProfile.GetLedModeDescription() + ") grabado en la memoria Flash del teclado.");
                    MessageBox.Show(
                        "El modo de iluminación LED (" + CurrentProfile.GetLedModeDescription() + ") se ha descargado correctamente al miniteclado.",
                        "Iluminación LED Actualizada",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    SetStatus("⚠ Teclado USB no detectado; el modo LED quedó guardado en el perfil actual.");
                    MessageBox.Show(
                        "El modo LED quedó guardado en tu perfil actual (" + CurrentProfile.GetLedModeDescription() + ").\n\nConecta el miniteclado por USB para grabarlo en el hardware.",
                        "Modo LED Guardado en Perfil",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            };
            page.Controls.Add(btnApplyLedNow);
        }

        private void SelectLayer(int layerIdx)
        {
            _currentLayerIdx = Math.Max(0, Math.Min(2, layerIdx));
            for (int i = 0; i < 3; i++)
            {
                bool active = (i == _currentLayerIdx);
                _btnLayers[i].BackColor = active ? Color.FromArgb(0, 75, 95) : CyberTheme.BgCard;
                _btnLayers[i].ForeColor = active ? CyberTheme.NeonCyan : CyberTheme.TextSecondary;
            }
            RefreshAllUI(string.Format("Capa activa cambiada a CAPA {0} ({1}).", _currentLayerIdx + 1, CurrentProfile.ProfileName));
        }

        public void SelectSlot(byte slotId)
        {
            _selectedSlotId = slotId;
            _keypadPreview.SelectedSlotId = slotId;
            RefreshSlotInspector("Editando " + SelectedSlot.SlotName);
        }

        public void SwitchTab(int tabIdx)
        {
            _activeTabIdx = tabIdx;
            for (int i = 0; i < _tabPages.Length; i++)
            {
                bool active = (i == tabIdx);
                _tabPages[i].Visible = active;
                _tabButtons[i].BackColor = active ? Color.FromArgb(24, 52, 78) : CyberTheme.BgCard;
                _tabButtons[i].ForeColor = active ? CyberTheme.NeonCyan : CyberTheme.TextSecondary;
            }
        }

        private void RefreshAllUI(string statusMessage = null)
        {
            _txtProfileName.Text = CurrentProfile.ProfileName;
            _keypadPreview.Profile = CurrentProfile;
            UpdateLedModeButtons();
            RefreshSlotInspector(statusMessage);
        }

        private void RefreshSlotInspector(string statusMessage = null)
        {
            ButtonConfig slot = SelectedSlot;
            if (slot == null) return;

            _lblEditingSlotTitle.Text = "EDITANDO: " + slot.SlotName.ToUpperInvariant();
            _lblEditingSlotCategory.Text = "[" + slot.GetCategoryBadge() + "]";
            _lblEditingSlotCategory.Left = _lblEditingSlotTitle.Right + 8;
            _lblEditingSlotCategory.ForeColor = slot.IsConfigured ? CyberTheme.GetCategoryColor(slot.Category) : CyberTheme.TextMuted;

            _suppressAliasEvent = true;
            _txtSlotAlias.Text = slot.CustomAlias ?? "";
            _suppressAliasEvent = false;

            _lblSequencePreview.Text = slot.GetTechnicalSummary();
            _lblSequencePreview.ForeColor = slot.IsConfigured ? CyberTheme.NeonGreen : CyberTheme.TextMuted;

            _keypadPreview.Invalidate();
            if (!string.IsNullOrEmpty(statusMessage))
            {
                SetStatus(statusMessage);
            }
        }

        private void ToggleModifierMask(byte mask)
        {
            _activeModifierMask ^= mask;
            UpdateModifierButtonsUI();
        }

        private void UpdateModifierButtonsUI()
        {
            foreach (var kv in _modifierButtons)
            {
                bool on = (_activeModifierMask & kv.Key) != 0;
                kv.Value.BackColor = on ? Color.FromArgb(0, 90, 110) : CyberTheme.BgCard;
                kv.Value.ForeColor = on ? CyberTheme.CyberYellow : CyberTheme.TextSecondary;
            }
        }

        private void SelectWheelModifier(byte mod)
        {
            _selectedWheelMod = mod;
            foreach (var kv in _wheelModButtons)
            {
                bool on = (kv.Key == mod);
                kv.Value.BackColor = on ? Color.FromArgb(0, 80, 100) : CyberTheme.BgCard;
                kv.Value.ForeColor = on ? CyberTheme.CyberYellow : CyberTheme.TextSecondary;
            }
        }

        private void UpdateLedModeButtons()
        {
            foreach (var kv in _ledModeButtons)
            {
                bool on = (CurrentProfile != null && CurrentProfile.LedMode == kv.Key);
                kv.Value.BackColor = on ? Color.FromArgb(48, 24, 78) : CyberTheme.BgCard;
                kv.Value.ForeColor = on ? CyberTheme.CyberYellow : CyberTheme.TextPrimary;
            }
        }

        private void AppendKeyStep(byte modifierMask, byte hidCode, string keyLabel)
        {
            ButtonConfig slot = SelectedSlot;
            if (slot.Category != ActionCategory.Keyboard)
            {
                slot.Category = ActionCategory.Keyboard;
                slot.Steps.Clear();
            }
            slot.StandaloneModifierMask = 0;

            if (slot.Steps.Count >= 5)
            {
                SetStatus("⚠ Máximo 5 pasos por macro alcanzado en esta tecla. Usa 'Borrar Paso' o 'Limpiar Tecla'.");
                return;
            }

            KeyStrokeStep step = new KeyStrokeStep(modifierMask, hidCode, keyLabel);
            slot.Steps.Add(step);

            if (slot.Steps.Count == 1 && string.IsNullOrEmpty(slot.CustomAlias))
            {
                slot.CustomAlias = step.ToDisplayString();
            }
            else if (slot.Steps.Count > 1)
            {
                // Keep custom alias if manually set, otherwise clear if it was auto-filled from step 1
            }

            // Reset one-shot modifier buttons after adding a key step for convenience
            _activeModifierMask = 0;
            UpdateModifierButtonsUI();

            RefreshSlotInspector(string.Format("Añadido paso [{0}] a {1} ({2}/5).", step.ToDisplayString(), slot.SlotName, slot.Steps.Count));
        }

        private void BtnBackspaceStep_Click(object sender, EventArgs e)
        {
            ButtonConfig slot = SelectedSlot;
            if (slot.Category == ActionCategory.Keyboard && slot.Steps.Count > 0)
            {
                slot.Steps.RemoveAt(slot.Steps.Count - 1);
                if (slot.Steps.Count == 0 && slot.StandaloneModifierMask == 0)
                {
                    slot.Category = ActionCategory.None;
                    slot.CustomAlias = "";
                }
                RefreshSlotInspector("Último paso eliminado de " + slot.SlotName);
            }
            else
            {
                slot.Clear();
                RefreshSlotInspector("Configuración de " + slot.SlotName + " borrada.");
            }
        }

        private void TogglePhysicalCapture(bool enable)
        {
            _isCapturingPhysicalKey = enable;
            _btnCapturePhysical.Text = enable ? "🔴 PULSA UNA TECLA... (ESC CANCELA)" : "🎙️ CAPTURAR TECLADO FÍSICO: OFF";
            _btnCapturePhysical.BackColor = enable ? Color.FromArgb(90, 15, 45) : CyberTheme.BgCard;
            _btnCapturePhysical.ForeColor = enable ? CyberTheme.CyberYellow : CyberTheme.NeonMagenta;
            if (enable)
            {
                SetStatus("🎙️ Modo captura activo: presiona cualquier combinación en tu teclado físico para asignarla a " + SelectedSlot.SlotName);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_isCapturingPhysicalKey)
            {
                Keys keyCode = keyData & Keys.KeyCode;
                if (keyCode == Keys.ControlKey || keyCode == Keys.ShiftKey || keyCode == Keys.Menu ||
                    keyCode == Keys.LWin || keyCode == Keys.RWin)
                {
                    return true;
                }

                byte modMask = 0;
                if ((keyData & Keys.Control) != 0) modMask |= HidKeyMap.MOD_LCTRL;
                if ((keyData & Keys.Shift) != 0) modMask |= HidKeyMap.MOD_LSHIFT;
                if ((keyData & Keys.Alt) != 0) modMask |= HidKeyMap.MOD_LALT;

                byte hidCode;
                string label;
                if (HidKeyMap.TryMapWinFormsKey(keyCode, out hidCode, out label))
                {
                    AppendKeyStep(modMask, hidCode, label);
                    TogglePhysicalCapture(false);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BtnDownloadSingleSlot_Click(object sender, EventArgs e)
        {
            if (!SelectedSlot.IsConfigured)
            {
                MessageBox.Show(
                    "La tecla seleccionada (" + SelectedSlot.SlotName + ") está vacía. Asígnale una función primero.",
                    "Tecla sin configurar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_hid.DownloadSingleSlot(CurrentProfile.LayerIndex, SelectedSlot))
            {
                SetStatus("✔ " + SelectedSlot.SlotName + " descargada exitosamente a la memoria Flash del miniteclado.");
                MessageBox.Show(
                    SelectedSlot.SlotName + " (" + SelectedSlot.GetPrimaryLabel() + ") se grabó correctamente en el miniteclado.",
                    "Descarga Exitosa",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                SetStatus("⚠ Miniteclado USB no detectado. La tecla sigue guardada en el perfil en memoria.");
                MessageBox.Show(
                    "No se detectó el miniteclado conectado por USB (VID:1189 / PID:8890).\n\nTu configuración para " + SelectedSlot.SlotName + " está guardada en el perfil en memoria.",
                    "Dispositivo USB no conectado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void BtnDownloadFullProfile_Click(object sender, EventArgs e)
        {
            int count = CurrentProfile.GetConfiguredCount();
            if (count == 0)
            {
                MessageBox.Show(
                    "No hay ninguna tecla ni knob configurado en esta capa todavía.\nConfigura al menos un botón antes de descargar.",
                    "Perfil Vacío",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _progressBar.Value = 0;
            string errorMsg = null;
            bool hardwareConnected = _hid.CheckAndConnect();
            int downloadedCount = 0;

            if (hardwareConnected)
            {
                downloadedCount = _hid.DownloadProfile(
                    CurrentProfile,
                    (step, total, text) =>
                    {
                        _progressBar.Value = Math.Min(100, (int)((step * 100.0) / Math.Max(1, total)));
                        SetStatus(text);
                        Application.DoEvents();
                    },
                    out errorMsg);
            }

            _progressBar.Value = 100;

            if (hardwareConnected && string.IsNullOrEmpty(errorMsg))
            {
                SetStatus(string.Format("✔ Perfil '{0}' ({1} botones + LED) descargado al miniteclado. Selecciona dónde guardar la imagen de referencia.", CurrentProfile.ProfileName, downloadedCount));
                MessageBox.Show(
                    string.Format("¡Descarga completada con éxito!\n\nSe programaron {0} asignaciones y el modo LED en la CAPA {1} del miniteclado.\n\nA continuación, elige dónde deseas guardar la imagen estilizada de referencia con el mapa de todas tus teclas.", downloadedCount, CurrentProfile.LayerIndex),
                    "Descarga al Miniteclado Completada",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                SetStatus("ℹ Miniteclado físico no detectado por USB. Generando imagen de referencia del perfil configurado...");
                DialogResult dr = MessageBox.Show(
                    "El miniteclado físico no se encuentra conectado al puerto USB en este momento (o está en modo edición offline).\n\n" +
                    "¿Deseas generar y guardar ahora la imagen estilizada de referencia (PNG) con todas las funciones configuradas en este perfil?",
                    "Modo Offline · Generar Mapa de Referencia",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (dr != DialogResult.Yes)
                {
                    return;
                }
            }

            PromptAndSaveReferenceImage();
        }

        private void BtnExportReferenceImage_Click(object sender, EventArgs e)
        {
            PromptAndSaveReferenceImage();
        }

        private void PromptAndSaveReferenceImage()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Guardar Imagen de Referencia del Miniteclado (PNG)";
                sfd.Filter = "Imagen PNG de Alta Resolución (*.png)|*.png";
                string safeName = (CurrentProfile.ProfileName ?? "Perfil").Replace(" ", "_").Replace("/", "-").Replace("\\", "-");
                sfd.FileName = string.Format("Referencia_MiniKeyboard_Capa{0}_{1}.png", CurrentProfile.LayerIndex, safeName);

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        ReferenceImageGenerator.SaveProfileCheatSheet(CurrentProfile, sfd.FileName);
                        SetStatus("✔ Imagen de referencia guardada en: " + sfd.FileName);
                        MessageBox.Show(
                            "La lámina de referencia estilizada del miniteclado se guardó correctamente en:\n\n" + sfd.FileName,
                            "Imagen de Referencia Generada",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al guardar la imagen: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnClearProfile_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                string.Format("¿Estás seguro de que deseas vaciar las 18 asignaciones de la CAPA {0} en memoria?", CurrentProfile.LayerIndex),
                "Confirmar Limpieza de Perfil",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (dr == DialogResult.Yes)
            {
                _layers[_currentLayerIdx] = LayerProfile.CreateEmpty((byte)(_currentLayerIdx + 1), "Capa " + (_currentLayerIdx + 1) + " Limpia");
                RefreshAllUI("Se limpiaron todas las teclas de la Capa " + (_currentLayerIdx + 1));
            }
        }

        private void BtnSaveProfile_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Guardar Perfil de Configuración del Miniteclado";
                sfd.Filter = "Perfil de Mini Keyboard (*.mkp;*.xml)|*.mkp;*.xml";
                sfd.FileName = string.Format("Perfil_Capa{0}.mkp", CurrentProfile.LayerIndex);
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        CurrentProfile.SaveToFile(sfd.FileName);
                        SetStatus("✔ Perfil guardado en disco: " + sfd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al guardar el perfil: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnLoadProfile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Cargar Perfil de Configuración del Miniteclado";
                ofd.Filter = "Perfil de Mini Keyboard (*.mkp;*.xml)|*.mkp;*.xml|Todos los archivos (*.*)|*.*";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        LayerProfile loaded = LayerProfile.LoadFromFile(ofd.FileName);
                        loaded.LayerIndex = (byte)(_currentLayerIdx + 1);
                        _layers[_currentLayerIdx] = loaded;
                        RefreshAllUI("✔ Perfil cargado desde: " + ofd.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al cargar el perfil: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void UsbTimer_Tick(object sender, EventArgs e)
        {
            UpdateUsbStatusBadge();
        }

        private void UpdateUsbStatusBadge()
        {
            bool connected = _hid.CheckAndConnect();
            if (connected)
            {
                _lblUsbStatus.Text = string.Format("● USB CONECTADO [VID:1189 PID:8890 · ID:{0}]", _hid.ActiveReportId);
                _lblUsbStatus.ForeColor = CyberTheme.NeonGreen;
                _lblUsbStatus.BackColor = Color.FromArgb(14, 38, 28);
            }
            else
            {
                _lblUsbStatus.Text = "◌ USB DESCONECTADO · MODO EDICIÓN / PREVIEW";
                _lblUsbStatus.ForeColor = CyberTheme.CyberYellow;
                _lblUsbStatus.BackColor = Color.FromArgb(32, 28, 16);
            }
        }

        private void SetStatus(string text)
        {
            if (_lblStatusLog != null)
            {
                _lblStatusLog.Text = "▸ " + text;
            }
        }

        private static Panel CreateStyledCardPanel(Point location, Size size)
        {
            Panel p = new Panel
            {
                Location = location,
                Size = size,
                BackColor = CyberTheme.BgPanel
            };
            p.Paint += (s, e) =>
            {
                Rectangle r = new Rectangle(0, 0, p.Width - 1, p.Height - 1);
                using (Pen borderPen = new Pen(Color.FromArgb(75, CyberTheme.NeonCyan), 1f))
                {
                    e.Graphics.DrawRectangle(borderPen, r);
                }
                using (SolidBrush cornerBr = new SolidBrush(CyberTheme.NeonCyan))
                {
                    e.Graphics.FillRectangle(cornerBr, 0, 0, 10, 2);
                    e.Graphics.FillRectangle(cornerBr, 0, 0, 2, 10);
                }
            };
            return p;
        }

        private static Button CreateCyberButton(string text, Point location, Size size, Color accentColor)
        {
            Button btn = new Button
            {
                Text = text,
                Location = location,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                BackColor = CyberTheme.BgCard,
                ForeColor = CyberTheme.TextPrimary,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(140, accentColor);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(32, 46, 70);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 80, 105);
            return btn;
        }

        public void GenerateDocumentationAssets(string outputDir)
        {
            Directory.CreateDirectory(outputDir);
            Show();
            Refresh();
            Application.DoEvents();

            // 1. Main view with Virtual Keyboard tab + Productivity profile
            SelectLayer(0);
            SelectSlot(8); // Captura de pantalla
            SwitchTab(0);
            Refresh();
            Application.DoEvents();
            CaptureFormBitmap(Path.Combine(outputDir, "interfaz_principal_cyberpunk.png"));

            // 2. Quick Shortcuts & Symbols tab
            SelectSlot(1);
            SwitchTab(1);
            Refresh();
            Application.DoEvents();
            CaptureFormBitmap(Path.Combine(outputDir, "pestana_atajos_rapidos.png"));

            // 3. Multimedia & Knobs configuration tab (Layer 2 Streamer profile)
            SelectLayer(1);
            SelectSlot(15); // Knob 1 Right
            SwitchTab(2);
            Refresh();
            Application.DoEvents();
            CaptureFormBitmap(Path.Combine(outputDir, "pestana_multimedia_knobs.png"));

            // 4. Generated Reference Cheat-Sheet Image (Productivity & Streamer) + Brand Logo Assets
            ReferenceImageGenerator.SaveProfileCheatSheet(_layers[0], Path.Combine(outputDir, "mapa_referencia_generado.png"));
            ReferenceImageGenerator.SaveProfileCheatSheet(_layers[1], Path.Combine(outputDir, "mapa_referencia_streamer.png"));

            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");
            BrandLogoRenderer.SaveApplicationLogoAssets(Path.Combine(outputDir, "logo_aplicacion_ac.png"), icoPath);
        }

        private void CaptureFormBitmap(string filePath)
        {
            using (Bitmap bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb))
            {
                DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
                bmp.Save(filePath, ImageFormat.Png);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_usbTimer != null) _usbTimer.Dispose();
                _hid.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
