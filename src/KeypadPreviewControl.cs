using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace MiniKeyboardCyber
{
    public class SlotSelectedEventArgs : EventArgs
    {
        public byte SlotId { get; private set; }
        public SlotSelectedEventArgs(byte slotId)
        {
            SlotId = slotId;
        }
    }

    public class KeypadPreviewControl : Control
    {
        private LayerProfile _profile;
        private byte _selectedSlotId = 1;
        private byte _hoveredSlotId = 0;
        private readonly Dictionary<byte, RectangleF> _hitRegions = new Dictionary<byte, RectangleF>();

        public event EventHandler<SlotSelectedEventArgs> SlotSelected;

        public LayerProfile Profile
        {
            get { return _profile; }
            set
            {
                _profile = value;
                Invalidate();
            }
        }

        public byte SelectedSlotId
        {
            get { return _selectedSlotId; }
            set
            {
                if (_selectedSlotId != value)
                {
                    _selectedSlotId = value;
                    Invalidate();
                }
            }
        }

        public KeypadPreviewControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.Selectable,
                true);
            BackColor = CyberTheme.BgPanel;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = ClientRectangle;
            g.Clear(CyberTheme.BgDeep);
            CyberTheme.DrawTechGrid(g, rect, 24);

            _hitRegions.Clear();
            if (_profile == null) return;

            float pad = 12f;
            RectangleF chassisRect = new RectangleF(pad, pad + 26f, rect.Width - pad * 2f, rect.Height - pad * 2f - 26f);
            if (chassisRect.Width < 100 || chassisRect.Height < 100) return;

            // Top HUD Header bar above chassis
            using (Font hudFont = new Font("Consolas", 8.2f, FontStyle.Bold))
            using (SolidBrush cyanBrush = new SolidBrush(CyberTheme.NeonCyan))
            using (SolidBrush secBrush = new SolidBrush(CyberTheme.TextSecondary))
            {
                g.DrawString("◈ VISTA PREVIA INTERACTIVA (12 TECLAS + 2 KNOBS)", hudFont, cyanBrush, pad, 6f);
                string countText = string.Format("CONFIGURADOS: {0}/18 · CLIC PARA EDITAR", _profile.GetConfiguredCount());
                SizeF sz = g.MeasureString(countText, hudFont);
                g.DrawString(countText, hudFont, secBrush, rect.Width - pad - sz.Width, 6f);
            }

            // Underglow based on LED mode
            Color ledGlowColor = _profile.LedMode == 0
                ? Color.FromArgb(40, 90, 110)
                : (_profile.LedMode == 1 ? CyberTheme.NeonCyan : CyberTheme.NeonMagenta);

            for (int i = 3; i >= 1; i--)
            {
                RectangleF glowRect = RectangleF.Inflate(chassisRect, i * 2.5f, i * 2.5f);
                using (GraphicsPath glowPath = CyberTheme.CreateChamferedRect(glowRect, 18f))
                using (Pen glowPen = new Pen(Color.FromArgb(24 * (4 - i), ledGlowColor), 2f))
                {
                    g.DrawPath(glowPen, glowPath);
                }
            }

            // Acrylic Chassis Body
            using (GraphicsPath chassisPath = CyberTheme.CreateChamferedRect(chassisRect, 16f))
            using (LinearGradientBrush chassisBr = new LinearGradientBrush(
                chassisRect,
                Color.FromArgb(18, 23, 35),
                Color.FromArgb(10, 13, 21),
                125f))
            using (Pen borderPen = new Pen(Color.FromArgb(180, ledGlowColor), 1.8f))
            {
                g.FillPath(chassisBr, chassisPath);
                g.DrawPath(borderPen, chassisPath);
            }

            // 4 Corner Hex Screws
            float screwOffset = 14f;
            CyberTheme.DrawHexScrew(g, chassisRect.Left + screwOffset, chassisRect.Top + screwOffset, 6f);
            CyberTheme.DrawHexScrew(g, chassisRect.Right - screwOffset, chassisRect.Top + screwOffset, 6f);
            CyberTheme.DrawHexScrew(g, chassisRect.Left + screwOffset, chassisRect.Bottom - screwOffset, 6f);
            CyberTheme.DrawHexScrew(g, chassisRect.Right - screwOffset, chassisRect.Bottom - screwOffset, 6f);

            // Layout geometry inside chassis:
            // Left 65% width: 4x3 grid of mechanical keys (1..12)
            // Right 35% width: 2 vertical rotary encoders (Knob 1 & Knob 2)
            float innerMarginX = 24f;
            float innerMarginY = 22f;
            RectangleF innerRect = new RectangleF(
                chassisRect.Left + innerMarginX,
                chassisRect.Top + innerMarginY,
                chassisRect.Width - innerMarginX * 2f,
                chassisRect.Height - innerMarginY * 2f);

            float keysAreaWidth = innerRect.Width * 0.63f;
            float knobsAreaWidth = innerRect.Width - keysAreaWidth - 16f;

            RectangleF keysRect = new RectangleF(innerRect.Left, innerRect.Top, keysAreaWidth, innerRect.Height);
            RectangleF knobsRect = new RectangleF(keysRect.Right + 16f, innerRect.Top, knobsAreaWidth, innerRect.Height);

            DrawKeysGrid(g, keysRect);
            DrawKnobsColumn(g, knobsRect);
        }

        private void DrawKeysGrid(Graphics g, RectangleF area)
        {
            int cols = 4;
            int rows = 3;
            float gapX = 10f;
            float gapY = 10f;

            float keyW = (area.Width - gapX * (cols - 1)) / cols;
            float keyH = (area.Height - gapY * (rows - 1)) / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    byte slotId = (byte)(r * cols + c + 1);
                    float kx = area.Left + c * (keyW + gapX);
                    float ky = area.Top + r * (keyH + gapY);
                    RectangleF keyRect = new RectangleF(kx, ky, keyW, keyH);
                    _hitRegions[slotId] = keyRect;

                    ButtonConfig slot = _profile.GetSlot(slotId);
                    DrawKeyCap(g, keyRect, slotId, slot);
                }
            }
        }

        private void DrawKeyCap(Graphics g, RectangleF rect, byte slotId, ButtonConfig slot)
        {
            bool isSelected = (_selectedSlotId == slotId);
            bool isHovered = (_hoveredSlotId == slotId);
            bool isConfigured = slot != null && slot.IsConfigured;

            Color catColor = isConfigured ? CyberTheme.GetCategoryColor(slot.Category) : CyberTheme.TextMuted;
            Color borderColor = isSelected
                ? CyberTheme.NeonCyan
                : (isHovered ? Color.FromArgb(220, CyberTheme.NeonCyan) : (isConfigured ? Color.FromArgb(165, catColor) : Color.FromArgb(65, 85, 115)));

            // Selection glow
            if (isSelected)
            {
                RectangleF glow = RectangleF.Inflate(rect, 3f, 3f);
                using (GraphicsPath gp = CyberTheme.CreateChamferedRect(glow, 8f))
                using (Pen glowPen = new Pen(Color.FromArgb(95, CyberTheme.NeonCyan), 3f))
                {
                    g.DrawPath(glowPen, gp);
                }
            }

            using (GraphicsPath path = CyberTheme.CreateChamferedRect(rect, 8f))
            using (LinearGradientBrush fillBr = new LinearGradientBrush(
                rect,
                isSelected ? Color.FromArgb(26, 48, 72) : (isHovered ? CyberTheme.BgCardHover : CyberTheme.BgKeyCap),
                isSelected ? Color.FromArgb(14, 28, 46) : Color.FromArgb(12, 16, 26),
                90f))
            using (Pen pen = new Pen(borderColor, isSelected ? 2.2f : 1.3f))
            {
                g.FillPath(fillBr, path);
                g.DrawPath(pen, path);
            }

            // Inner mechanical keycap bevel
            RectangleF innerCap = RectangleF.Inflate(rect, -5f, -5f);
            using (GraphicsPath innerPath = CyberTheme.CreateRoundedRect(innerCap, 4f))
            using (Pen bevelPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
            {
                g.DrawPath(bevelPen, innerPath);
            }

            // Top row: Key Number (01..12) and Category pill
            using (Font numFont = new Font("Consolas", 9f, FontStyle.Bold))
            using (SolidBrush numBr = new SolidBrush(isSelected ? CyberTheme.NeonCyan : CyberTheme.TextSecondary))
            {
                g.DrawString(string.Format("#{0:00}", slotId), numFont, numBr, rect.Left + 7f, rect.Top + 6f);
            }

            string badge = slot != null ? slot.GetCategoryBadge() : "VACÍO";
            using (Font badgeFont = new Font("Consolas", 7f, FontStyle.Bold))
            {
                SizeF bSize = g.MeasureString(badge, badgeFont);
                RectangleF badgeRect = new RectangleF(rect.Right - bSize.Width - 11f, rect.Top + 6f, bSize.Width + 5f, 14f);
                using (SolidBrush badgeBg = new SolidBrush(Color.FromArgb(isConfigured ? 45 : 20, catColor)))
                using (SolidBrush badgeTxt = new SolidBrush(isConfigured ? catColor : CyberTheme.TextMuted))
                {
                    g.FillRectangle(badgeBg, badgeRect);
                    g.DrawString(badge, badgeFont, badgeTxt, badgeRect.Left + 2f, badgeRect.Top + 1f);
                }
            }

            // Center Primary Label
            string primary = slot != null ? slot.GetPrimaryLabel() : "VACÍO";
            string tech = (slot != null && slot.IsConfigured) ? slot.GetTechnicalSummary() : "";
            bool hasDistinctTech = !string.IsNullOrEmpty(tech) && !string.Equals(primary, tech, StringComparison.OrdinalIgnoreCase);

            RectangleF textBounds = new RectangleF(
                rect.Left + 6f,
                rect.Top + 23f,
                rect.Width - 12f,
                rect.Height - (hasDistinctTech ? 40f : 27f));

            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
            using (Font mainFont = new Font("Segoe UI", 9.2f, isConfigured ? FontStyle.Bold : FontStyle.Regular))
            using (SolidBrush mainBr = new SolidBrush(isConfigured ? CyberTheme.TextPrimary : CyberTheme.TextMuted))
            {
                g.DrawString(primary, mainFont, mainBr, textBounds, sf);
            }

            if (hasDistinctTech)
            {
                RectangleF subBounds = new RectangleF(rect.Left + 3f, rect.Bottom - 19f, rect.Width - 6f, 15f);
                using (StringFormat sf = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                using (Font subFont = new Font("Consolas", 6.8f, FontStyle.Regular))
                using (SolidBrush subBr = new SolidBrush(catColor))
                {
                    g.DrawString(tech, subFont, subBr, subBounds, sf);
                }
            }
        }

        private void DrawKnobsColumn(Graphics g, RectangleF area)
        {
            float gapY = 12f;
            float knobBoxH = (area.Height - gapY) / 2f;

            RectangleF knob1Box = new RectangleF(area.Left, area.Top, area.Width, knobBoxH);
            RectangleF knob2Box = new RectangleF(area.Left, area.Top + knobBoxH + gapY, area.Width, knobBoxH);

            DrawSingleKnobModule(g, knob1Box, "KNOB 01 (SUPERIOR)", 13, 14, 15, CyberTheme.NeonMagenta);
            DrawSingleKnobModule(g, knob2Box, "KNOB 02 (INFERIOR)", 16, 17, 18, CyberTheme.CyberYellow);
        }

        private void DrawSingleKnobModule(Graphics g, RectangleF box, string title, byte leftSlotId, byte centerSlotId, byte rightSlotId, Color knobAccent)
        {
            bool anySelected = (_selectedSlotId == leftSlotId || _selectedSlotId == centerSlotId || _selectedSlotId == rightSlotId);

            using (GraphicsPath path = CyberTheme.CreateChamferedRect(box, 10f))
            using (SolidBrush bgBr = new SolidBrush(Color.FromArgb(14, 19, 30)))
            using (Pen borderPen = new Pen(anySelected ? knobAccent : Color.FromArgb(90, knobAccent), anySelected ? 1.8f : 1.1f))
            {
                g.FillPath(bgBr, path);
                g.DrawPath(borderPen, path);
            }

            // Header + Rotary Dial on top row of module
            float dialRadius = 15f;
            float dialCx = box.Right - dialRadius - 10f;
            float dialCy = box.Top + 20f;

            using (Font titleFont = new Font("Consolas", 8.2f, FontStyle.Bold))
            using (SolidBrush titleBr = new SolidBrush(knobAccent))
            {
                g.DrawString("⚙ " + title, titleFont, titleBr, box.Left + 8f, box.Top + 7f);
            }

            using (Font subFont = new Font("Consolas", 7f, FontStyle.Regular))
            using (SolidBrush subBr = new SolidBrush(CyberTheme.TextSecondary))
            {
                g.DrawString("Giro infinito + Click", subFont, subBr, box.Left + 10f, box.Top + 22f);
            }

            CyberTheme.DrawRotaryEncoderDial(g, dialCx, dialCy, dialRadius, knobAccent, anySelected);
            using (Font dialFont = new Font("Consolas", 6.8f, FontStyle.Bold))
            using (SolidBrush dialBr = new SolidBrush(CyberTheme.TextPrimary))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(leftSlotId == 13 ? "K1" : "K2", dialFont, dialBr, new RectangleF(dialCx - dialRadius, dialCy - dialRadius, dialRadius * 2, dialRadius * 2), sf);
            }

            // 3 full-width stacked interactive action zones (Giro Izq, Pulsar Centro, Giro Der)
            float zonesLeft = box.Left + 8f;
            float zonesWidth = box.Width - 16f;
            float zonesTop = box.Top + 40f;
            float zonesTotalH = box.Height - 48f;
            float zoneGap = 5f;
            float zoneH = (zonesTotalH - zoneGap * 2f) / 3f;

            DrawKnobActionZone(g, new RectangleF(zonesLeft, zonesTop, zonesWidth, zoneH), leftSlotId, "⟲ IZQ", knobAccent);
            DrawKnobActionZone(g, new RectangleF(zonesLeft, zonesTop + zoneH + zoneGap, zonesWidth, zoneH), centerSlotId, "● CLICK", knobAccent);
            DrawKnobActionZone(g, new RectangleF(zonesLeft, zonesTop + (zoneH + zoneGap) * 2f, zonesWidth, zoneH), rightSlotId, "⟳ DER", knobAccent);
        }

        private void DrawKnobActionZone(Graphics g, RectangleF rect, byte slotId, string prefix, Color accent)
        {
            _hitRegions[slotId] = rect;
            ButtonConfig slot = _profile.GetSlot(slotId);
            bool isSelected = (_selectedSlotId == slotId);
            bool isHovered = (_hoveredSlotId == slotId);
            bool isConfigured = slot != null && slot.IsConfigured;

            Color borderCol = isSelected
                ? CyberTheme.NeonCyan
                : (isHovered ? accent : (isConfigured ? Color.FromArgb(160, accent) : Color.FromArgb(60, 80, 105)));

            using (GraphicsPath p = CyberTheme.CreateRoundedRect(rect, 4f))
            using (SolidBrush fill = new SolidBrush(
                isSelected ? Color.FromArgb(30, 56, 82) : (isHovered ? CyberTheme.BgCardHover : Color.FromArgb(20, 26, 40))))
            using (Pen pen = new Pen(borderCol, isSelected ? 1.8f : 1f))
            {
                g.FillPath(fill, p);
                g.DrawPath(pen, p);
            }

            using (Font prefixFont = new Font("Consolas", 7.8f, FontStyle.Bold))
            using (SolidBrush prefixBr = new SolidBrush(isSelected ? CyberTheme.NeonCyan : accent))
            {
                g.DrawString(prefix, prefixFont, prefixBr, rect.Left + 6f, rect.Top + (rect.Height - 13f) * 0.5f);
            }

            string label = slot != null ? slot.GetPrimaryLabel() : "VACÍO";
            RectangleF valRect = new RectangleF(rect.Left + 64f, rect.Top + 1f, rect.Width - 68f, rect.Height - 2f);
            using (Font valFont = new Font("Segoe UI", 8.2f, isConfigured ? FontStyle.Bold : FontStyle.Regular))
            using (SolidBrush valBr = new SolidBrush(isConfigured ? CyberTheme.TextPrimary : CyberTheme.TextMuted))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
            {
                g.DrawString(label, valFont, valBr, valRect, sf);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            byte found = 0;
            foreach (var kv in _hitRegions)
            {
                if (kv.Value.Contains(e.Location))
                {
                    found = kv.Key;
                    break;
                }
            }
            if (found != _hoveredSlotId)
            {
                _hoveredSlotId = found;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredSlotId != 0)
            {
                _hoveredSlotId = 0;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            foreach (var kv in _hitRegions)
            {
                if (kv.Value.Contains(e.Location))
                {
                    SelectedSlotId = kv.Key;
                    if (SlotSelected != null)
                    {
                        SlotSelected(this, new SlotSelectedEventArgs(kv.Key));
                    }
                    break;
                }
            }
        }
    }
}
