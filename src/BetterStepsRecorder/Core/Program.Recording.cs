using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using FlaUI.Core.AutomationElements;
using static BetterStepsRecorder.WindowHelper;
using Size = BetterStepsRecorder.WindowHelper.Size;

namespace BetterStepsRecorder
{
    internal static partial class Program
    {
        private static IntPtr _hookID = IntPtr.Zero;
        private static LowLevelMouseProc _proc = HookCallback;
        public static bool IsRecording = false;
        private static readonly string _ownProcessName = Process.GetCurrentProcess().ProcessName;

        /// <summary>
        /// Sets up the mouse hook to start recording user interactions
        /// </summary>
        public static void HookMouseOperations()
        {
            _hookID = SetHook(_proc);
            IsRecording = true;
        }
        
        /// <summary>
        /// Removes the mouse hook to stop recording user interactions
        /// </summary>
        public static void UnHookMouseOperations()
        {
            UnhookWindowsHookEx(_hookID);
            IsRecording = false;
        }

        /// <summary>
        /// Sets up the Windows hook for capturing mouse events
        /// </summary>
        /// <param name="proc">The callback procedure for the hook</param>
        /// <returns>A handle to the hook</returns>
        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
                else
                {
                    // Handle the case where MainModule is null
                    throw new InvalidOperationException("The process does not have a main module.");
                }
            }
        }

        /// <summary>
        /// Delegate for the low-level mouse hook callback
        /// </summary>
        /// <param name="nCode">The hook code</param>
        /// <param name="wParam">The message identifier</param>
        /// <param name="lParam">A pointer to the message data</param>
        /// <returns>The result of the hook processing</returns>
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Callback function for processing mouse events
        /// </summary>
        /// <param name="nCode">The hook code</param>
        /// <param name="wParam">The message identifier</param>
        /// <param name="lParam">A pointer to the message data</param>
        /// <returns>The result of the hook processing</returns>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (!IsRecording)
                return CallNextHookEx(_hookID, nCode, wParam, lParam);

            if (nCode >= 0 && (MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam || MouseMessages.WM_RBUTTONUP == (MouseMessages)wParam))
            {
                POINT cursorPos;
                if (GetCursorPos(out cursorPos))
                {
                    IntPtr hwnd = WindowFromPoint(cursorPos);
                    if (hwnd != IntPtr.Zero)
                    {
                        // Capture cheap Win32 data synchronously so we return quickly
                        string? windowTitle     = GetTopLevelWindowTitle(hwnd);
                        string? applicationName = GetApplicationName(hwnd);
                        string  clickType       = MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam ? "Left Click" : "Right Click";

                        GetWindowRect(hwnd, out RECT UIrect);
                        RECT rect        = GetTopLevelWindowRect(hwnd);
                        int  windowWidth = rect.Right  - rect.Left;
                        int  windowHeight= rect.Bottom - rect.Top;
                        int  UIWidth     = UIrect.Right  - UIrect.Left;
                        int  UIHeight    = UIrect.Bottom - UIrect.Top;

                        // Grab the raw pixels immediately in the hook before CallNextHookEx
                        // delivers the event. For left-clicks this captures the dialog/button
                        // before it dismisses; for right-clicks it captures before the context
                        // menu disappears. CopyFromScreen is pure GDI and fast enough here.
                        Bitmap? preClickBitmap = null;
                        try
                        {
                            preClickBitmap = new Bitmap(windowWidth, windowHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            using (Graphics gfx = Graphics.FromImage(preClickBitmap))
                                gfx.CopyFromScreen(rect.Left, rect.Top, 0, 0,
                                    new System.Drawing.Size(windowWidth, windowHeight),
                                    CopyPixelOperation.SourceCopy);
                        }
                        catch
                        {
                            preClickBitmap?.Dispose();
                            preClickBitmap = null;
                        }

                        // Capture a snapshot of all values needed by the background thread
                        var snapshot = (
                            cursorPos, hwnd, windowTitle, applicationName, clickType,
                            UIrect, rect, windowWidth, windowHeight, UIWidth, UIHeight,
                            preClickBitmap
                        );

                        // Offload the slow work (FlaUI UI Automation + PNG encode) to a
                        // ThreadPool thread so the hook callback returns immediately.
                        // Windows unhooks any hook that blocks for too long (~300 ms).
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            var (cp, _, wt, appName, ct,
                                 uiRect, winRect, winW, winH, uiW, uiH,
                                 preBitmap) = snapshot;

                            // FlaUI call — can block for hundreds of ms on complex UIs
                            AutomationElement? element = GetElementFromPoint(
                                new System.Drawing.Point(cp.X, cp.Y));

                            string? elementName = null;
                            string? elementType = null;
                            if (element != null)
                            {
                                try { elementName = element.Properties.Name.IsSupported ? element.Name : null; }
                                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Could not read element Name: {ex.Message}"); }
                                try { elementType = element.Properties.ControlType.IsSupported ? element.ControlType.ToString() : null; }
                                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Could not read element ControlType: {ex.Message}"); }
                            }

                            // Skip if this click is to our own app
                            if (appName == _ownProcessName)
                            {
                                preBitmap?.Dispose();
                                return;
                            }

                            RecordEvent recordEvent;
                            lock (_recordEventsLock)
                            {
                                recordEvent = new RecordEvent
                                {
                                    WindowTitle        = wt,
                                    ApplicationName    = appName,
                                    WindowCoordinates  = new RECT { Left = winRect.Left, Top = winRect.Top, Bottom = winRect.Bottom, Right = winRect.Right },
                                    WindowSize         = new Size { Width = winW, Height = winH },
                                    UICoordinates      = new RECT { Left = uiRect.Left, Top = uiRect.Top, Bottom = uiRect.Bottom, Right = uiRect.Right },
                                    UISize             = new Size { Width = uiW, Height = uiH },
                                    UIElement          = null, // not needed after name/type extracted; releasing COM object
                                    ElementName        = elementName,
                                    ElementType        = elementType,
                                    MouseCoordinates   = new POINT { X = cp.X, Y = cp.Y },
                                    EventType          = ct,
                                    _StepText          = $"In {appName}, {ct} on {elementType} {elementName}",
                                    Step               = _recordEvents.Count + 1
                                };
                                _recordEvents.Add(recordEvent);
                            }

                            // Screen capture: use the pre-captured bitmap (taken synchronously before
                            // CallNextHookEx so dialogs/buttons are still visible), falling back to
                            // a live capture if the pre-capture failed for any reason.
                            // PNG bytes are spooled to disk immediately so they don't stay in RAM.
                            byte[]? pngBytes = null;
                            if (preBitmap != null)
                            {
                                using (preBitmap)
                                {
                                    using (Graphics gfx = Graphics.FromImage(preBitmap))
                                        DrawArrowAtCursor(gfx, winW, winH, winRect.Left, winRect.Top, cp);
                                    using (var ms = new System.IO.MemoryStream())
                                    {
                                        preBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                                        pngBytes = ms.ToArray();
                                    }
                                }
                            }
                            else
                            {
                                // Fall back to live capture — returns base64; convert to bytes
                                string? b64 = SaveScreenRegionScreenshot(
                                    winRect.Left, winRect.Top, winW, winH, recordEvent.ID, cp);
                                if (b64 != null)
                                    pngBytes = Convert.FromBase64String(b64);
                            }

                            if (pngBytes != null)
                            {
                                // Try to spool to disk; fall back to RAM if spool write fails
                                string? spoolPath = SpoolScreenshot(pngBytes, recordEvent.ID);
                                if (spoolPath != null)
                                    recordEvent.ScreenshotSpoolPath = spoolPath;
                                else
                                    recordEvent.Screenshotb64 = Convert.ToBase64String(pngBytes);

                                // Release the large byte array immediately — it was either written to
                                // disk or encoded into base64; either way we no longer need the raw bytes.
                                pngBytes = null;
                            }

                            // The PNG byte array is large (LOH eligible at >85KB). Nudge the GC to
                            // collect it promptly rather than waiting for the next scheduled Gen2.
                            GC.Collect(2, GCCollectionMode.Optimized, blocking: false);

                            // Marshal UI update back to the UI thread (non-blocking — don't Invoke)
                            _form1Instance?.BeginInvoke((Action)(() =>
                            {
                                _form1Instance.AddRecordEventToListBox(recordEvent);
                                _form1Instance.activityTimer.Stop();
                                _form1Instance.activityTimer.Start();
                            }));
                        });
                    }
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// P/Invoke declaration for the SetWindowsHookEx function
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);
    }
}