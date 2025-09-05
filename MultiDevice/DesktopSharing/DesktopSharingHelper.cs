using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace MultiDevice
{
    #region 屏幕抓图
    public static class ScreenCapture
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public POINTAPI ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTAPI
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyHeight, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

        [DllImport("Shcore.dll")]
        public static extern int GetScaleFactorForMonitor(IntPtr hMonitor, out int scaleFactor);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDesktopWindow();
        // Windows 10 及更高版本提供的 API
        [DllImport("Shcore.dll", CharSet = CharSet.Auto)]
        public static extern int GetDpiForMonitor(IntPtr hMonitor, int dpiType, out uint dpiX, out uint dpiY);


        private const Int32 CURSOR_SHOWING = 0x0001;
        private const Int32 DI_NORMAL = 0x0003;



        public static byte[] CapturePrimaryScreen(bool captureMouse)
        {
            // 获取实际的物理分辨率
            int screenWidth  = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;

            // 获取桌面窗口句柄
            IntPtr desktopWindow = GetDesktopWindow();

            // 获取桌面窗口所在的主屏监视器句柄
            IntPtr hMonitor = MonitorFromWindow(desktopWindow, 2); 

            // 使用 GetDpiForMonitor 获取主屏幕的 DPI
            uint dpiX, dpiY;
            int result = GetDpiForMonitor(hMonitor, 0, out dpiX, out dpiY); 

            if (result == 0) // S_OK
            {
                // 计算缩放倍数
                float scaleFactor = dpiX / 96.0f;
                LogHelper.Log.LogDebug($"主屏幕缩放倍数: {scaleFactor * 100}%");

                //if(scaleFactor > 1.0)
                //{
                //    screenWidth  = (int)(screenWidth * scaleFactor);
                //    screenHeight = (int)(screenHeight * scaleFactor);
                //}
            }

            LogHelper.Log.LogDebug($"获取到屏幕分辨：{screenWidth},{screenHeight}");

            // 截取屏幕内容
            Rectangle bounds = new Rectangle(0, 0, screenWidth, screenHeight);

            var bitmap = CaptureScreenEx(bounds, captureMouse);
            if (bitmap == null)
            {
                LogHelper.Log.LogDebug("未获取到屏幕数据");
                return null;
            }

            LogHelper.Log.LogDebug($"Image Data : {bitmap.Size}");

            // 将截图保存到内存流中并转换为字节数组
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }


        public static Bitmap CaptureScreenEx(Rectangle bounds, bool captureMouse)
        {
            Bitmap result = new Bitmap(bounds.Width, bounds.Height);

            try
            {
                using (Graphics g = Graphics.FromImage(result))
                {
                    g.CopyFromScreen(bounds.Location, System.Drawing.Point.Empty, bounds.Size);

                    if (captureMouse)
                    {
                        CURSORINFO pci;
                        pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

                        if (GetCursorInfo(out pci))
                        {
                            if (pci.flags == CURSOR_SHOWING)
                            {
                                var hdc = g.GetHdc();
                                DrawIconEx(hdc, pci.ptScreenPos.x - bounds.X, pci.ptScreenPos.y - bounds.Y, pci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
                                g.ReleaseHdc();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log.LogError($"屏幕截图发生异常:{ex.ToString()}");
                result = null;
            }

            return result;
        }

        // 将 Bitmap 转换为 BitmapSource，以便在 WPF 中使用
        public static BitmapSource BitmapToBitmapSource(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }

            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource result;

            try
            {
                result = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return result;
        }

        public static byte[] CaptureScreen()
        {
            // 获取屏幕的宽度和高度
            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            // 使用 GDI+ 进行屏幕截图
            using (Bitmap bitmap = new Bitmap(screenWidth, screenHeight))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight));
                }

                // 将截图保存到内存流中并转换为字节数组
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Jpeg);
                    return ms.ToArray();
                }
            }
        }

        public static byte[] Compress(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return ms.ToArray();
            }
        }
    }
    #endregion

    #region CRC 校验
    public static class CRC32
    {
        private static readonly uint[] table;

        static CRC32()
        {
            table = new uint[256];
            const uint polynomial = 0xedb88320;
            for (uint i = 0; i < table.Length; ++i)
            {
                uint crc = i;
                for (uint j = 8; j > 0; --j)
                {
                    if ((crc & 1) == 1)
                    {
                        crc = (crc >> 1) ^ polynomial;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
                table[i] = crc;
            }
        }

        public static uint ComputeChecksum(byte[] bytes)
        {
            uint crc = 0xffffffff;
            foreach (byte b in bytes)
            {
                byte tableIndex = (byte)((crc & 0xff) ^ b);
                crc = (crc >> 8) ^ table[tableIndex];
            }
            return ~crc;
        }
    }
    #endregion

    public class DesktopSharingHelper
    {
        private const int MaxPacketSize = 1024;  // 设置最大包大小为 64 KB
        private UdpClient udpClient;
        private IPEndPoint serverEndpoint;
        private int packetId = 0;
        private CancellationTokenSource cts;
        public DesktopSharingHelper()
        {

        }

        public void Start(string serverIp, int serverPort)
        {
            cts = new CancellationTokenSource();
            udpClient = new UdpClient();
            serverEndpoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            // 启动自动发送任务
            Task.Run(() => StartSendingLoop(cts.Token));
        }

        private async Task StartSendingLoop(CancellationToken token)
        {
            LogHelper.Log.LogDebug("开始屏幕共享...");
            while (!token.IsCancellationRequested)
            {
                try
                {
                    DateTime time = DateTime.Now;
                    // 捕获屏幕并发送
                    byte[] imageBytes = ScreenCapture.CapturePrimaryScreen(true);
                    if (imageBytes == null)
                    {
                        continue;
                    }
                    await SendScreenShotAsync(imageBytes);
                }
                catch (Exception ex)
                {
                    LogHelper.Log.LogError($"发送过程中发生错误: {ex.ToString()}");
                }

                // 每秒发送一次
                await Task.Delay(25);
            }
        }

        public void Stop()
        {
            cts.Cancel();
            LogHelper.Log.LogDebug("停止屏幕共享");
        }

        #region 数据发送

        public async Task SendScreenShotAsync(byte[] imageBytes)
        {
            byte[] compressedBytes = ScreenCapture.Compress(imageBytes);
            uint checksum = CRC32.ComputeChecksum(compressedBytes);
            packetId++;
            // 将数据分成多个小包
            int totalPackets = (int)Math.Ceiling(compressedBytes.Length / (double)MaxPacketSize);

            for (int i = 0; i < totalPackets; i++)
            {
                int offset = i * MaxPacketSize;
                int packetSize = Math.Min(MaxPacketSize, compressedBytes.Length - offset);
                byte[] packetData = new byte[packetSize];
                Array.Copy(compressedBytes, offset, packetData, 0, packetSize);

                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(packetId);                // 写入包 ID
                    writer.Write(totalPackets);            // 写入总包数
                    writer.Write(i);                       // 写入当前包序号
                    writer.Write(packetData.Length);       // 写入当前包数据长度
                    writer.Write(checksum);                // 写入校验和
                    writer.Write(packetData);              // 写入压缩数据片段

                    byte[] packet = ms.ToArray();
                    await udpClient.SendAsync(packet, packet.Length, serverEndpoint);
                }
            }
        }
        #endregion
    }
}
