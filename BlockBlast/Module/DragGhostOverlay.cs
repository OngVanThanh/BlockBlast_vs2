using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace BlockBlast.Module
{
    /// <summary>
    /// Lớp phủ đồ họa siêu nhẹ kế thừa NativeWindow (Win32 thuần, KHÔNG phải Form).
    /// Dùng để vẽ khối gạch và dây ná bắn bám theo chuột mượt mà không bị cắt viền.
    /// Đảm bảo dự án chỉ có duy nhất 1 Form kế thừa System.Windows.Forms.Form.
    /// </summary>
    internal sealed class DragGhostOverlay : NativeWindow, IDisposable
    {
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        private const int WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;

        private const int ULW_ALPHA = 0x00000002;
        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        private const int SW_HIDE = 0;
        private const int SW_SHOWNOACTIVATE = 4;

        private bool isDisposed = false;
        public bool IsDisposed => isDisposed;
        public bool Visible { get; private set; } = false;

        public DragGhostOverlay(IWin32Window owner = null)
        {
            CreateParams cp = new CreateParams
            {
                Style = WS_POPUP,
                ExStyle = WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE,
                Parent = owner != null ? owner.Handle : IntPtr.Zero
            };
            CreateHandle(cp);
        }

        public void Show()
        {
            if (Handle != IntPtr.Zero && !isDisposed)
            {
                ShowWindow(Handle, SW_SHOWNOACTIVATE);
                Visible = true;
            }
        }

        public void Hide()
        {
            if (Handle != IntPtr.Zero && !isDisposed)
            {
                ShowWindow(Handle, SW_HIDE);
                Visible = false;
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                Visible = false;
                DestroyHandle();
            }
        }

        protected override void WndProc(ref Message m)
        {
            // Xuyên chuột tuyệt đối: không bao giờ nhận hit-test
            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = (IntPtr)HTTRANSPARENT;
                return;
            }
            base.WndProc(ref m);
        }

        /// <summary>Cập nhật hình ảnh (bitmap ARGB) và vị trí màn hình của lớp phủ.</summary>
        public void SetBitmap(Bitmap bitmap, Point screenTopLeft)
        {
            if (bitmap == null || Handle == IntPtr.Zero || isDisposed) return;

            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
                oldBitmap = SelectObject(memDc, hBitmap);

                SIZE size = new SIZE { cx = bitmap.Width, cy = bitmap.Height };
                POINT source = new POINT { x = 0, y = 0 };
                POINT topPos = new POINT { x = screenTopLeft.X, y = screenTopLeft.Y };
                BLENDFUNCTION blend = new BLENDFUNCTION
                {
                    BlendOp = AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255,
                    AlphaFormat = AC_SRC_ALPHA
                };

                UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memDc, ref source, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, screenDc);
                if (hBitmap != IntPtr.Zero)
                {
                    SelectObject(memDc, oldBitmap);
                    DeleteObject(hBitmap);
                }
                DeleteDC(memDc);
            }
        }

        /// <summary>Di chuyển lớp phủ (không vẽ lại bitmap) - gọi mỗi lần MouseMove.</summary>
        public void MoveTo(Point screenTopLeft)
        {
            if (Handle == IntPtr.Zero || isDisposed) return;
            SetWindowPos(Handle, IntPtr.Zero, screenTopLeft.X, screenTopLeft.Y, 0, 0,
                         SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }

        #region Win32 API

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE { public int cx; public int cy; }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
                                                       IntPtr hdcSrc, ref POINT pprSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        #endregion
    }
}