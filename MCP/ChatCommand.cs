using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AlexMCP.Views;
using AlexMCP.Tools;
using AlexMCP.Utils;

namespace AlexMCP
{
    [Transaction(TransactionMode.Manual)]
    public class ChatCommand : IExternalCommand
    {
        public static ChatWindow _chatWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_chatWindow == null || !_chatWindow.IsLoaded)
                {
                    UIApplication uiapp = commandData.Application;
                    RevitEvent revitEvent = new RevitEvent();
                    ToolHandler toolHandler = new ToolHandler(uiapp);

                    _chatWindow = new ChatWindow(revitEvent, toolHandler);
                    _chatWindow.Show();
                }
                else
                {
                    _chatWindow.Activate();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
