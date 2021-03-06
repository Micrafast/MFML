﻿using MetroFramework;
using MFML.Core;
using MFML.Download;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MFML.UI
{
    public partial class DownloadWindow : Form
    {
        private const int CS_DropSHADOW = 0x20000;
        private const int GCL_STYLE = (-26);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SetClassLong(IntPtr hwnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassLong(IntPtr hwnd, int nIndex);
        Downloader Provider;
        List<DownloadItem> Items;
        bool CanBeClosed = true;

        public DownloadWindow(Downloader Provider)
        {
            this.Provider = Provider;
            InitializeComponent();
        }

        private bool DragMouse;
        private Point MouseDragPoint;
        private Color ThemeColor1 = Color.DeepSkyBlue;

        public Color ThemeColor
        {
            get { return ThemeColor1; }
            set
            {
                ThemeColor1 = value;
                CloseButton.BackColor = value;
                MinimizeButton.BackColor = value;
                BackColor = value;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Y <= 30 && e.Button == MouseButtons.Left)
            {
                DragMouse = true;
                MouseDragPoint = e.Location;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (DragMouse)
            {
                SetDesktopLocation
                    (
                    MousePosition.X - MouseDragPoint.X,
                    MousePosition.Y - MouseDragPoint.Y
                    );
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            DragMouse = false;
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = CreateGraphics();
            Font UsedFont = MetroFonts.Label(MetroLabelSize.Medium, MetroLabelWeight.Regular);
            float h = UsedFont.GetHeight();
            RectangleF textRect = new RectangleF(10, 15 - (h / 2), Width - 60, 15 + (h / 2));
            Brush b = new SolidBrush(ThemeColor);
            g.FillRectangle(b, textRect);
            b.Dispose();
            g.DrawString(Text, UsedFont, Brushes.White, textRect);
            g.DrawLine(Pens.Black, 0, 0, Width - 1, 0);                  // Draw black border
            g.DrawLine(Pens.Black, Width - 1, 0, Width - 1, Height - 1);
            g.DrawLine(Pens.Black, Width - 1, Height - 1, 0, Height - 1);
            g.DrawLine(Pens.Black, 0, Height - 1, 0, 0);
            g.Dispose();
        }

        private void TopBarButton_MouseEnter(object sender, EventArgs e)
        {
            ((Button)sender).BackColor = Color.Red;
        }

        private void TopBarButton_MouseLeave(object sender, EventArgs e)
        {
            ((Button)sender).BackColor = ThemeColor;
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        private void DownloadWindow_Load(object sender, EventArgs e)
        {
            SetClassLong(this.Handle, GCL_STYLE, GetClassLong(this.Handle, GCL_STYLE) | CS_DropSHADOW);
            ThemeColor = LauncherMain.Instance.Settings.ThemeColor;
            listBox1.Enabled = false;
            CloseButton.Enabled = false;
            CanBeClosed = false;
            SetProgress("加载所有可下载版本中。。。", 0);
            downloader.RunWorkerAsync("init");
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            CloseButton.Enabled = false;
            listBox1.Enabled = false;
            CanBeClosed = false;
            downloader.RunWorkerAsync(listBox1.SelectedItem);
        }

        private void downloader_DoWork(object sender, DoWorkEventArgs e)
        {
            if (e.Argument is string && (string)e.Argument == "init")
            {
                Items = Provider.GetAllItemsToDownload();
                e.Result = 0;
            }
            else
            {
#pragma warning disable IDE0019 // 使用模式匹配
                var item = e.Argument as DownloadItem;
#pragma warning restore IDE0019 // 使用模式匹配
                if (item == null)
                {
                    throw new ArgumentException("Background worker's argument must be a DownloadItem");
                }
                item.OnProgressChanged += (a, b) => this.Invoke(new Action(() => SetProgress(a, b)));
                item.Download();
                e.Result = 1;
            }
        }

        private void SetProgress(string status, int progress)
        {
            if (status != null)
            {
                textBox1.Text += status + Environment.NewLine;
                textBox1.SelectionStart = textBox1.TextLength;
                textBox1.ScrollToCaret();
            }
            progressBar1.Value = progress;
        }

        private void downloader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            listBox1.Enabled = true;
            CloseButton.Enabled = true;
            CanBeClosed = true;
            SetProgress("已完成！", 100);
            if ((int)e.Result == 0) 
            {
                listBox1.Items.AddRange(Items.ToArray());
            }
            else
            {
                MFMLMessageBox.ShowMessageBox(this, "提示", "已完成", MessageBoxButtons.OK);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!CanBeClosed)
            {
                e.Cancel = true;
                return;
            }
            base.OnFormClosing(e);
        }
    }
}
