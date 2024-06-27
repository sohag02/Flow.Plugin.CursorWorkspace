// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Flow.Plugin.CursorWorkspaces.VSCodeHelper
{
    public static class VSCodeInstances
    {
        private static string _systemPath = string.Empty;

        private static readonly string _userAppDataPath = Environment.GetEnvironmentVariable("AppData");

        public static List<VSCodeInstance> Instances { get; set; } = new();

        private static BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }


        private static Bitmap BitmapOverlayToCenter(Bitmap bitmap1, Bitmap overlayBitmap)
        {
            int bitmap1Width = bitmap1.Width;
            int bitmap1Height = bitmap1.Height;

            Bitmap overlayBitmapResized = new Bitmap(overlayBitmap, new System.Drawing.Size(bitmap1Width / 2, bitmap1Height / 2));

            float marginLeft = (float)((bitmap1Width * 0.7) - (overlayBitmapResized.Width * 0.5));
            float marginTop = (float)((bitmap1Height * 0.7) - (overlayBitmapResized.Height * 0.5));

            Bitmap finalBitmap = new Bitmap(bitmap1Width, bitmap1Height);
            using (Graphics g = Graphics.FromImage(finalBitmap))
            {
                g.DrawImage(bitmap1, System.Drawing.Point.Empty);
                g.DrawImage(overlayBitmapResized, marginLeft, marginTop);
            }

            return finalBitmap;
        }

        // Gets the AppData foreach instance of VSCode
        public static void LoadVSCodeInstances()
        {
            if (_systemPath == Environment.GetEnvironmentVariable("PATH"))
                return;

            Instances = [];

            _systemPath = Environment.GetEnvironmentVariable("PATH") ?? "";

            var paths = _systemPath.Split(";").Where(path =>
                path.Contains("VS Code", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("codium", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("vscode", StringComparison.OrdinalIgnoreCase));

            foreach (var path in paths)
            {
                if (!Directory.Exists(path))
                    continue;

                var files = Directory.EnumerateFiles(path).Where(file =>
                     file.Contains("code", StringComparison.OrdinalIgnoreCase) ||
                    file.Contains("codium", StringComparison.OrdinalIgnoreCase)
                    && !file.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)).ToArray();

                var iconPath = Path.GetDirectoryName(path);

                if (files.Length <= 0)
                    continue;

                var vscodeExecFile = files[0];
                var version = string.Empty;

                var instance = new VSCodeInstance();
                // Main._context.API.LogInfo("CursorWorkspaces", "vscodeExecFile : " + vscodeExecFile + " : " + files.Length);

                if (vscodeExecFile.EndsWith("code"))
                {
                    if (vscodeExecFile.Contains("cursor"))
                        version = "Cursor";
                    else version = "Code";
                    instance.VSCodeVersion = VSCodeVersion.Stable;
                }
                else if (vscodeExecFile.EndsWith("code-insiders"))
                {
                    version = "Code - Insiders";
                    instance.VSCodeVersion = VSCodeVersion.Insiders;
                }
                else if (vscodeExecFile.EndsWith("code-exploration"))
                {
                    version = "Code - Exploration";
                    instance.VSCodeVersion = VSCodeVersion.Exploration;
                }
                else if (vscodeExecFile.EndsWith("codium"))
                {
                    version = "VSCodium";
                    instance.VSCodeVersion = VSCodeVersion.Stable;
                }

                if (version == string.Empty)
                    continue;

                var portableData = Path.Join(iconPath, "data");
                instance.AppData = Directory.Exists(portableData) ? Path.Join(portableData, "user-data") : Path.Combine(_userAppDataPath, version);
                var iconVSCode = Path.Join(iconPath, $"{version}.exe");

                var bitmapIconVscode = Icon.ExtractAssociatedIcon(iconVSCode)?.ToBitmap();

                // workspace
                var folderIcon = (Bitmap)Image.FromFile(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "//Images//folder.png");
                instance.WorkspaceIconBitMap = Bitmap2BitmapImage(BitmapOverlayToCenter(folderIcon, bitmapIconVscode));

                // remote
                var monitorIcon = (Bitmap)Image.FromFile(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "//Images//monitor.png");

                instance.RemoteIconBitMap = Bitmap2BitmapImage(BitmapOverlayToCenter(monitorIcon, bitmapIconVscode));

                Instances.Add(instance);
            }

            var cursorInstance = new VSCodeInstance
            {
                AppData = Path.Combine(_userAppDataPath, "Cursor"),
                VSCodeVersion = VSCodeVersion.Stable
            };
            var cursorAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs\\cursor");

            var cursorBitmapIcon = Icon.ExtractAssociatedIcon(Path.Join(cursorAppData, $"Cursor.exe"))?.ToBitmap();
            var cursorFolderIcon = (Bitmap)Image.FromFile(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "//Images//folder.png");
            var cursorMonitorIcon = (Bitmap)Image.FromFile(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "//Images//monitor.png");
            cursorInstance.WorkspaceIconBitMap = Bitmap2BitmapImage(BitmapOverlayToCenter(cursorFolderIcon, cursorBitmapIcon));
            cursorInstance.RemoteIconBitMap = Bitmap2BitmapImage(BitmapOverlayToCenter(cursorMonitorIcon, cursorBitmapIcon));

            Instances.Add(cursorInstance);
        }
    }
}
