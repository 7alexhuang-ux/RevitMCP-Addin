using System;
using System.Reflection;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;

namespace AlexMCP
{
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Create Ribbon Tab
                string tabName = "AlexMCP Tools";
                application.CreateRibbonTab(tabName);

                // Create Ribbon Panel
                RibbonPanel panel = application.CreateRibbonPanel(tabName, "Alex Connect");

                // Create Connect Button
                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                PushButtonData connectButtonData = new PushButtonData(
                    "AlexMCPConnect",
                    "Connect",
                    assemblyPath,
                    "AlexMCP.ConnectCommand");

                PushButton connectButton = panel.AddItem(connectButtonData) as PushButton;
                connectButton.ToolTip = "Connect to MCP Server (Gemini)";
                connectButton.LargeImage = GetEmbeddedImage("AlexMCP.Resources.Connect32.png");
                connectButton.Image = GetEmbeddedImage("AlexMCP.Resources.Connect32.png");

                // Create Chat Button
                PushButtonData chatButtonData = new PushButtonData(
                    "AlexMCPChat",
                    "Chat",
                    assemblyPath,
                    "AlexMCP.ChatCommand");

                PushButton chatButton = panel.AddItem(chatButtonData) as PushButton;
                chatButton.ToolTip = "Open AI Chat Window";
                chatButton.LargeImage = GetEmbeddedImage("AlexMCP.Resources.Chat32.png");
                chatButton.Image = GetEmbeddedImage("AlexMCP.Resources.Chat32.png");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("MCP Error", "Failed to create ribbon: " + ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private System.Windows.Media.ImageSource GetEmbeddedImage(string name)
        {
            try
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                System.IO.Stream stream = assembly.GetManifestResourceStream(name);
                
                if (stream == null)
                {
                    // Debug: List all resources to find the correct name
                    string[] resources = assembly.GetManifestResourceNames();
                    string resourceList = string.Join("\n", resources);
                    TaskDialog.Show("Resource Error", $"Could not find resource '{name}'.\nAvailable resources:\n{resourceList}");
                    return null;
                }

                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Image Load Error", $"Failed to load image '{name}': {ex.Message}");
                return null;
            }
        }
    }
}
