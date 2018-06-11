using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace USTC.Software.hanyizhao.NetSpeedMonitor
{
    class WinAPIWrapper
    {
        /// <summary>
        /// window的扩展样式  
        /// </summary>  
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x80;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        /// <summary>
        /// 设置窗体的样式     
        /// </summary>     
        /// <param name="handle">操作窗体的句柄</param>     
        /// <param name="oldStyle">进行设置窗体的样式类型.</param>     
        /// <param name="newStyle">新样式</param>     
        [DllImport("User32.dll")]
        //[DllImport("User32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]   
        //  public static extern void SetWindowLong(IntPtr handle, int oldStyle, long newStyle);  
        public static extern long SetWindowLong(IntPtr handle, int oldStyle, IntPtr newStyle);

        /// <summary>  
        /// 获取窗体指定的样式.  
        /// </summary>  
        /// <param name="handle">操作窗体的句柄</param>  
        /// <param name="style">要进行返回的样式</param>  
        /// <returns>当前window的样式</returns>     
        [DllImport("User32.dll")]
        //   [DllImport("User32.dll", EntryPoint = "GetWindowLong",CallingConvention = CallingConvention.Cdecl)]  
        public static extern long GetWindowLong(IntPtr handle, int style);

        [DllImport("SHELL32", CallingConvention = CallingConvention.StdCall)]
        public static extern uint SHAppBarMessage(int dwMessage, ref APPBARDATA pData);
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string msg);
        //取得Shell窗口句柄函数 
        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();
        //Get Process ID by window handle.
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        //取得桌面窗口句柄函数 
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();
        //取得前台窗口句柄函数 
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        // The GetExtendedTcpTable function retrieves a table that contains a list of
        // TCP endpoints available to the application. Decorating the function with
        // DllImport attribute indicates that the attributed method is exposed by an
        // unmanaged dynamic-link library 'iphlpapi.dll' as a static entry point.
        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize,
            bool bOrder, int ulAf, TcpTableClass tableClass, uint reserved = 0);

        // The GetExtendedUdpTable function retrieves a table that contains a list of
        // UDP endpoints available to the application. Decorating the function with
        // DllImport attribute indicates that the attributed method is exposed by an
        // unmanaged dynamic-link library 'iphlpapi.dll' as a static entry point.
        [DllImport("iphlpapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int pdwSize,
            bool bOrder, int ulAf, UdpTableClass tableClass, uint reserved = 0);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public override string ToString()
        {
            return "[" + left + "," + top + "," + right + "," + bottom + "]";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uCallbackMessage;
        public int uEdge;
        public RECT rc;
        public IntPtr lParam;
    }
    public enum ABMsg : int
    {
        ABM_NEW = 0,
        ABM_REMOVE,
        ABM_QUERYPOS,
        ABM_SETPOS,
        ABM_GETSTATE,
        ABM_GETTASKBARPOS,
        ABM_ACTIVATE,
        ABM_GETAUTOHIDEBAR,
        ABM_SETAUTOHIDEBAR,
        ABM_WINDOWPOSCHANGED,
        ABM_SETSTATE
    }
    public enum ABNotify : int
    {
        ABN_STATECHANGE = 0,
        ABN_POSCHANGED,
        ABN_FULLSCREENAPP,
        ABN_WINDOWARRANGE
    }
    public enum ABEdge : int
    {
        ABE_LEFT = 0,
        ABE_TOP,
        ABE_RIGHT,
        ABE_BOTTOM
    }

    // Enum to define the set of values used to indicate the type of table returned by 
    // calls made to the function 'GetExtendedTcpTable'.
    public enum TcpTableClass
    {
        TCP_TABLE_BASIC_LISTENER,
        TCP_TABLE_BASIC_CONNECTIONS,
        TCP_TABLE_BASIC_ALL,
        TCP_TABLE_OWNER_PID_LISTENER,
        TCP_TABLE_OWNER_PID_CONNECTIONS,
        TCP_TABLE_OWNER_PID_ALL,
        TCP_TABLE_OWNER_MODULE_LISTENER,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS,
        TCP_TABLE_OWNER_MODULE_ALL
    }

    // Enum to define the set of values used to indicate the type of table returned by calls
    // made to the function GetExtendedUdpTable.
    public enum UdpTableClass
    {
        UDP_TABLE_BASIC,
        UDP_TABLE_OWNER_PID,
        UDP_TABLE_OWNER_MODULE
    }

    /// <summary>
    /// Window handles (HWND) used for hWndInsertAfter
    /// </summary>
    public enum SpecialWindowHandles : int
    {
        NoTopMost = -2,
        TopMost = -1,
        Top = 0,
        Bottom = 1
    }

    /// <summary>
    /// SetWindowPos Flags
    /// </summary>
    public enum SetWindowPosFlags: uint
    {
        NOSIZE = 0x0001,
        NOMOVE = 0x0002,
        NOZORDER = 0x0004,
        NOREDRAW = 0x0008,
        NOACTIVATE = 0x0010,
        DRAWFRAME = 0x0020,
        FRAMECHANGED = 0x0020,
        SHOWWINDOW = 0x0040,
        HIDEWINDOW = 0x0080,
        NOCOPYBITS = 0x0100,
        NOOWNERZORDER = 0x0200,
        NOREPOSITION = 0x0200,
        NOSENDCHANGING = 0x0400,
        DEFERERASE = 0x2000,
        ASYNCWINDOWPOS = 0x4000
    }
}