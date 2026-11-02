using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace MiniKeyboardCyber
{
    public static class ReferenceImageGenerator
    {
        public static Bitmap RenderProfileCheatSheet(LayerProfile profile)
        {
            int width = 1920;
            int height = 1180;
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                // 1. Deep Cyber-Noir Background + Gradient Vignette
                using (LinearGradientBrush bgBr = new LinearGradientBrush(
                    new Rectangle(0, 0, width, height),
                    Color.FromArgb(6, 8, 14),
                    Color.FromArgb(14, 18, 30),
                    135f))
                {
                    g.FillRectangle(bgBr, 0, 0, width, height);
                }

                CyberTheme.DrawTechGrid(g, new Rectangle(0, 0, width, height), 32);

                // Subtle decorative cyber scanlines / corner HUD frame
                DrawOuterHudFrame(g, width, height, profile);

                // 2. Main Visual Hardware Keypad Diagram (Top Section)
                RectangleF boardArea = new RectangleF(55f, 120f, width - 110f, 585f);
                DrawStylizedHardwareBoard(g, boardArea, profile);

                // 3. Complete Quick-Reference Legend Table (Bottom Section)
                RectangleF tableArea = new RectangleF(55f, 725f, width - 110f, 390f);
                DrawQuickReferenceTable(g, tableArea, profile);

                // 4. Footer
                using (Font footerFont = new Font("Consolas", 10f, FontStyle.Bold))
                using (SolidBrush cyanBr = new SolidBrush(CyberTheme.NeonCyan))
                using (SolidBrush mutedBr = new SolidBrush(CyberTheme.TextSecondary))
                {
                    string leftFooter = "⚡ MINI KEYBOARD CYBERPUNK STUDIO // USB HID MACRO PAD (12 TECLAS + 2 KNOBS ROTATORIOS)";
                    string rightFooter = string.Format("GENERADO: {0:dd/MM/yyyy HH:mm}  |  MEMORIA FLASH INDEPENDIENTE", DateTime.Now);
                    g.DrawString(leftFooter, footerFont, cyanBr, 60f, height - 38f);
                    SizeF rSize = g.MeasureString(rightFooter, footerFont);
                    g.DrawString(rightFooter, footerFont, mutedBr, width - 60f - rSize.Width, height - 38f);
                }
            }

            return bmp;
        }

        public static void SaveProfileCheatSheet(LayerProfile profile, string filePath)
        {
            using (Bitmap bmp = RenderProfileCheatSheet(profile))
            {
                bmp.Save(filePath, ImageFormat.Png);
            }
        }

        private static void DrawOuterHudFrame(Graphics g, int width, int height, LayerProfile profile)
        {
            RectangleF headerRect = new RectangleF(55f, 26f, width - 110f, 76f);
            using (GraphicsPath hp = CyberTheme.CreateChamferedRect(headerRect, 14f))
            using (LinearGradientBrush hBr = new LinearGradientBrush(
                headerRect,
                Color.FromArgb(20, 32, 52),
                Color.FromArgb(12, 18, 30),
                0f))
            using (Pen hPen = new Pen(CyberTheme.NeonCyan, 2f))
            {
                g.FillPath(hBr, hp);
                g.DrawPath(hPen, hp);
            }

            // Neon magenta accent bar on left of header
            using (SolidBrush magBr = new SolidBrush(CyberTheme.NeonMagenta))
            {
                g.FillRectangle(magBr, 68f, 40f, 8f, 48f);
            }

            using (Font titleFont = new Font("Segoe UI", 22f, FontStyle.Bold))
            using (Font subFont = new Font("Consolas", 11f, FontStyle.Bold))
            using (SolidBrush whiteBr = new SolidBrush(CyberTheme.TextPrimary))
            using (SolidBrush cyanBr = new SolidBrush(CyberTheme.NeonCyan))
            using (SolidBrush yellowBr = new SolidBrush(CyberTheme.CyberYellow))
            {
                string title = string.Format("MAPA DE REFERENCIA // {0}", (profile.ProfileName ?? "PERFIL PERSONALIZADO").ToUpperInvariant());
                g.DrawString(title, titleFont, whiteBr, 88f, 32f);

                string sub = string.Format("CAPA ACTIVA: CAPA {0:00}   |   ILUMINACIÓN: {1}   |   BOTONES CONFIGURADOS: {2}/18",
                    profile.LayerIndex,
                    profile.GetLedModeDescription().ToUpperInvariant(),
                    profile.GetConfiguredCount());
                g.DrawString(sub, subFont, cyanBr, 90f, 73f);

                // Right badge
                RectangleF badgeRect = new RectangleF(width - 355f, 40f, 280f, 48f);
                using (GraphicsPath bp = CyberTheme.CreateChamferedRect(badgeRect, 8f))
                using (SolidBrush bFill = new SolidBrush(Color.FromArgb(35, 255, 0, 95)))
                using (Pen bPen = new Pen(CyberTheme.NeonMagenta, 1.5f))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.FillPath(bFill, bp);
                    g.DrawPath(bPen, bp);
                    g.DrawString("GUÍA RÁPIDA DE TECLADO", subFont, yellowBr, badgeRect, sf);
                }
            }
        }

        private static void DrawStylizedHardwareBoard(Graphics g, RectangleF boardArea, LayerProfile profile)
        {
            // Outer glow
            for (int i = 4; i >= 1; i--)
            {
                RectangleF glowRect = RectangleF.Inflate(boardArea, i * 3f, i * 3f);
                using (GraphicsPath gp = CyberTheme.CreateChamferedRect(glowRect, 24f))
                using (Pen gpPen = new Pen(Color.FromArgb(18 * (5 - i), CyberTheme.NeonCyan), 2.5f))
                {
                    g.DrawPath(gpPen, gp);
                }
            }

            // Board acrylic body
            using (GraphicsPath path = CyberTheme.CreateChamferedRect(boardArea, 22f))
            using (LinearGradientBrush boardBr = new LinearGradientBrush(
                boardArea,
                Color.FromArgb(18, 24, 38),
                Color.FromArgb(10, 14, 22),
                120f))
            using (Pen borderPen = new Pen(CyberTheme.NeonCyan, 2.2f))
            {
                g.FillPath(boardBr, path);
                g.DrawPath(borderPen, path);
            }

            // 4 Corner Hex Screws
            float sOff = 22f;
            CyberTheme.DrawHexScrew(g, boardArea.Left + sOff, boardArea.Top + sOff, 9f);
            CyberTheme.DrawHexScrew(g, boardArea.Right - sOff, boardArea.Top + sOff, 9f);
            CyberTheme.DrawHexScrew(g, boardArea.Left + sOff, boardArea.Bottom - sOff, 9f);
            CyberTheme.DrawHexScrew(g, boardArea.Right - sOff, boardArea.Bottom - sOff, 9f);

            // Split into 12 Keys Matrix (Left 63%) and 2 Knobs Panel (Right 37%)
            float padX = 42f;
            float padY = 34f;
            RectangleF inner = new RectangleF(boardArea.Left + padX, boardArea.Top + padY, boardArea.Width - padX * 2f, boardArea.Height - padY * 2f);

            float keysW = inner.Width * 0.62f;
            float knobsW = inner.Width - keysW - 28f;

            RectangleF keysRect = new RectangleF(inner.Left, inner.Top, keysW, inner.Height);
            RectangleF knobsRect = new RectangleF(keysRect.Right + 28f, inner.Top, knobsW, inner.Height);

            // Render 3x4 Keys
            int cols = 4;
            int rows = 3;
            float gapX = 16f;
            float gapY = 16f;
            float keyW = (keysRect.Width - gapX * (cols - 1)) / cols;
            float keyH = (keysRect.Height - gapY * (rows - 1)) / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    byte slotId = (byte)(r * cols + c + 1);
                    RectangleF kr = new RectangleF(
                        keysRect.Left + c * (keyW + gapX),
                        keysRect.Top + r * (keyH + gapY),
                        keyW,
                        keyH);
                    DrawPosterKeyCap(g, kr, slotId, profile.GetSlot(slotId));
                }
            }

            // Render 2 Knobs on Right
            float knobGapY = 20f;
            float knobH = (knobsRect.Height - knobGapY) / 2f;
            RectangleF knob1Rect = new RectangleF(knobsRect.Left, knobsRect.Top, knobsRect.Width, knobH);
            RectangleF knob2Rect = new RectangleF(knobsRect.Left, knobsRect.Top + knobH + knobGapY, knobsRect.Width, knobH);

            DrawPosterKnobModule(g, knob1Rect, "KNOB 01 // SUPERIOR", 13, 14, 15, profile, CyberTheme.NeonMagenta);
            DrawPosterKnobModule(g, knob2Rect, "KNOB 02 // INFERIOR", 16, 17, 18, profile, CyberTheme.CyberYellow);
        }

        private static void DrawPosterKeyCap(Graphics g, RectangleF rect, byte slotId, ButtonConfig slot)
        {
            bool isConfigured = slot != null && slot.IsConfigured;
            Color accent = isConfigured ? CyberTheme.GetCategoryColor(slot.Category) : CyberTheme.TextMuted;

            using (GraphicsPath path = CyberTheme.CreateChamferedRect(rect, 12f))
            using (LinearGradientBrush fillBr = new LinearGradientBrush(
                rect,
                isConfigured ? Color.FromArgb(24, 34, 52) : Color.FromArgb(16, 20, 30),
                Color.FromArgb(12, 16, 25),
                90f))
            using (Pen borderPen = new Pen(isConfigured ? accent : Color.FromArgb(70, 90, 120), isConfigured ? 2.2f : 1.2f))
            {
                g.FillPath(fillBr, path);
                g.DrawPath(borderPen, path);
            }

            // Inner keycap rim
            RectangleF innerRim = RectangleF.Inflate(rect, -8f, -8f);
            using (GraphicsPath rp = CyberTheme.CreateRoundedRect(innerRim, 6f))
            using (Pen rimPen = new Pen(Color.FromArgb(35, 255, 255, 255), 1f))
            {
                g.DrawPath(rimPen, rp);
            }

            // Key number top-left
            using (Font numFont = new Font("Consolas", 13f, FontStyle.Bold))
            using (SolidBrush numBr = new SolidBrush(isConfigured ? CyberTheme.NeonCyan : CyberTheme.TextSecondary))
            {
                g.DrawString(string.Format("TECLA {0:00}", slotId), numFont, numBr, rect.Left + 12f, rect.Top + 10f);
            }

            // Category badge top-right
            string badge = slot != null ? slot.GetCategoryBadge() : "VACÍO";
            using (Font badgeFont = new Font("Consolas", 9.5f, FontStyle.Bold))
            {
                SizeF bSz = g.MeasureString(badge, badgeFont);
                RectangleF bRect = new RectangleF(rect.Right - bSz.Width - 18f, rect.Top + 10f, bSz.Width + 8f, 20f);
                using (SolidBrush bFill = new SolidBrush(Color.FromArgb(isConfigured ? 55 : 25, accent)))
                using (SolidBrush bTxt = new SolidBrush(accent))
                {
                    g.FillRectangle(bFill, bRect);
                    g.DrawString(badge, badgeFont, bTxt, bRect.Left + 4f, bRect.Top + 2f);
                }
            }

            // Center Alias / Action Title
            string primary = slot != null ? slot.GetPrimaryLabel() : "SIN ASIGNAR";
            string tech = (slot != null && slot.IsConfigured) ? slot.GetTechnicalSummary() : "Disponible";

            RectangleF centerBox = new RectangleF(rect.Left + 12f, rect.Top + 36f, rect.Width - 24f, rect.Height - 76f);
            using (Font mainFont = new Font("Segoe UI", 13.5f, FontStyle.Bold))
            using (SolidBrush mainBr = new SolidBrush(isConfigured ? CyberTheme.TextPrimary : CyberTheme.TextMuted))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
            {
                g.DrawString(primary, mainFont, mainBr, centerBox, sf);
            }

            // Bottom Technical Pill
            RectangleF pillRect = new RectangleF(rect.Left + 12f, rect.Bottom - 36f, rect.Width - 24f, 25f);
            using (GraphicsPath pp = CyberTheme.CreateRoundedRect(pillRect, 5f))
            using (SolidBrush pFill = new SolidBrush(Color.FromArgb(isConfigured ? 38 : 18, accent)))
            using (Pen pPen = new Pen(Color.FromArgb(isConfigured ? 140 : 50, accent), 1f))
            using (Font techFont = new Font("Consolas", 10f, FontStyle.Bold))
            using (SolidBrush techBr = new SolidBrush(isConfigured ? accent : CyberTheme.TextMuted))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
            {
                g.FillPath(pFill, pp);
                g.DrawPath(pPen, pp);
                g.DrawString(tech, techFont, techBr, pillRect, sf);
            }
        }

        private static void DrawPosterKnobModule(Graphics g, RectangleF box, string title, byte leftId, byte centerId, byte rightId, LayerProfile profile, Color accent)
        {
            using (GraphicsPath path = CyberTheme.CreateChamferedRect(box, 14f))
            using (LinearGradientBrush fillBr = new LinearGradientBrush(
                box,
                Color.FromArgb(20, 26, 42),
                Color.FromArgb(12, 16, 26),
                90f))
            using (Pen borderPen = new Pen(accent, 2f))
            {
                g.FillPath(fillBr, path);
                g.DrawPath(borderPen, path);
            }

            using (Font titleFont = new Font("Consolas", 12.5f, FontStyle.Bold))
            using (SolidBrush titleBr = new SolidBrush(accent))
            {
                g.DrawString("⚙ " + title + " (ROTACIÓN INFINITA + CLICK)", titleFont, titleBr, box.Left + 16f, box.Top + 10f);
            }

            // Rotary Encoder Graphic on Left
            float dialRadius = 56f;
            float dialCx = box.Left + 80f;
            float dialCy = box.Top + 42f + (box.Height - 48f) * 0.5f;
            CyberTheme.DrawRotaryEncoderDial(g, dialCx, dialCy, dialRadius, accent, true);

            using (Font kFont = new Font("Segoe UI", 13f, FontStyle.Bold))
            using (SolidBrush wBr = new SolidBrush(CyberTheme.TextPrimary))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(leftId == 13 ? "KNOB 1" : "KNOB 2", kFont, wBr, new RectangleF(dialCx - dialRadius, dialCy - dialRadius, dialRadius * 2, dialRadius * 2), sf);
            }

            // 3 Action Callout Cards on Right
            float cardsLeft = dialCx + dialRadius + 24f;
            float cardsW = box.Right - cardsLeft - 16f;
            float cardsTop = box.Top + 40f;
            float cardsTotalH = box.Height - 52f;
            float gap = 8f;
            float cardH = (cardsTotalH - gap * 2f) / 3f;

            DrawPosterKnobCard(g, new RectangleF(cardsLeft, cardsTop, cardsW, cardH), "⟲ GIRO IZQUIERDA", profile.GetSlot(leftId), accent);
            DrawPosterKnobCard(g, new RectangleF(cardsLeft, cardsTop + cardH + gap, cardsW, cardH), "● CLICK CENTRAL", profile.GetSlot(centerId), accent);
            DrawPosterKnobCard(g, new RectangleF(cardsLeft, cardsTop + (cardH + gap) * 2f, cardsW, cardH), "⟳ GIRO DERECHA", profile.GetSlot(rightId), accent);
        }

        private static void DrawPosterKnobCard(Graphics g, RectangleF rect, string actionLabel, ButtonConfig slot, Color accent)
        {
            bool isConfigured = slot != null && slot.IsConfigured;
            Color catColor = isConfigured ? CyberTheme.GetCategoryColor(slot.Category) : CyberTheme.TextMuted;

            using (GraphicsPath p = CyberTheme.CreateRoundedRect(rect, 6f))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(24, 32, 48)))
            using (Pen pen = new Pen(isConfigured ? catColor : Color.FromArgb(65, 85, 110), 1.5f))
            {
                g.FillPath(fill, p);
                g.DrawPath(pen, p);
            }

            using (Font actFont = new Font("Consolas", 10.5f, FontStyle.Bold))
            using (SolidBrush actBr = new SolidBrush(accent))
            {
                g.DrawString(actionLabel, actFont, actBr, rect.Left + 12f, rect.Top + (rect.Height - 16f) * 0.5f);
            }

            string primary = slot != null ? slot.GetPrimaryLabel() : "SIN ASIGNAR";
            string tech = (slot != null && slot.IsConfigured) ? slot.GetTechnicalSummary() : "";
            string combined = (!string.IsNullOrEmpty(tech) && !string.Equals(primary, tech, StringComparison.OrdinalIgnoreCase))
                ? string.Format("{0}  [{1}]", primary, tech)
                : primary;

            RectangleF valRect = new RectangleF(rect.Left + 168f, rect.Top + 2f, rect.Width - 178f, rect.Height - 4f);
            using (Font valFont = new Font("Segoe UI", 11.5f, FontStyle.Bold))
            using (SolidBrush valBr = new SolidBrush(isConfigured ? CyberTheme.TextPrimary : CyberTheme.TextMuted))
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
            {
                g.DrawString(combined, valFont, valBr, valRect, sf);
            }
        }

        private static void DrawQuickReferenceTable(Graphics g, RectangleF tableArea, LayerProfile profile)
        {
            using (GraphicsPath path = CyberTheme.CreateChamferedRect(tableArea, 16f))
            using (SolidBrush fill = new SolidBrush(Color.FromArgb(14, 19, 30)))
            using (Pen pen = new Pen(Color.FromArgb(160, CyberTheme.NeonCyan), 1.6f))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            using (Font hdrFont = new Font("Consolas", 12f, FontStyle.Bold))
            using (SolidBrush cyanBr = new SolidBrush(CyberTheme.NeonCyan))
            {
                g.DrawString("◈ TABLA DETALLADA DE ASIGNACIONES DEL PERFIL (12 TECLAS + 6 ACCIONES DE KNOBS)", hdrFont, cyanBr, tableArea.Left + 20f, tableArea.Top + 12f);
            }

            int cols = 3;
            int rowsPerCol = 6;
            float padX = 20f;
            float topY = tableArea.Top + 42f;
            float colGap = 18f;
            float colW = (tableArea.Width - padX * 2f - colGap * (cols - 1)) / cols;
            float rowH = (tableArea.Height - 54f) / rowsPerCol;

            for (int i = 0; i < 18; i++)
            {
                int col = i / rowsPerCol;
                int row = i % rowsPerCol;
                RectangleF cell = new RectangleF(
                    tableArea.Left + padX + col * (colW + colGap),
                    topY + row * rowH,
                    colW,
                    rowH - 5f);

                ButtonConfig slot = profile.Slots[i];
                bool isConfigured = slot.IsConfigured;
                Color catCol = isConfigured ? CyberTheme.GetCategoryColor(slot.Category) : CyberTheme.TextMuted;

                using (GraphicsPath cp = CyberTheme.CreateRoundedRect(cell, 5f))
                using (SolidBrush cFill = new SolidBrush(Color.FromArgb(20, 27, 42)))
                using (Pen cPen = new Pen(Color.FromArgb(isConfigured ? 120 : 45, catCol), 1f))
                {
                    g.FillPath(cFill, cp);
                    g.DrawPath(cPen, cp);
                }

                // Slot name badge
                RectangleF tagRect = new RectangleF(cell.Left + 8f, cell.Top + 6f, 175f, cell.Height - 12f);
                using (Font slotFont = new Font("Consolas", 9.5f, FontStyle.Bold))
                using (SolidBrush slotBr = new SolidBrush(slot.SlotId <= 12 ? CyberTheme.NeonCyan : (slot.SlotId <= 15 ? CyberTheme.NeonMagenta : CyberTheme.CyberYellow)))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(slot.SlotName, slotFont, slotBr, tagRect, sf);
                }

                // Assigned function description
                string desc = isConfigured
                    ? (!string.IsNullOrEmpty(slot.CustomAlias)
                        ? string.Format("{0}  ({1})", slot.CustomAlias, slot.GetTechnicalSummary())
                        : slot.GetTechnicalSummary())
                    : "— Sin asignar —";

                RectangleF descRect = new RectangleF(cell.Left + 188f, cell.Top + 4f, cell.Width - 196f, cell.Height - 8f);
                using (Font descFont = new Font("Segoe UI", 10f, isConfigured ? FontStyle.Bold : FontStyle.Italic))
                using (SolidBrush descBr = new SolidBrush(isConfigured ? CyberTheme.TextPrimary : CyberTheme.TextMuted))
                using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter })
                {
                    g.DrawString(desc, descFont, descBr, descRect, sf);
                }
            }
        }
    }
}
