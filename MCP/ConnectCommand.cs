using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

namespace AlexMCP
{
    [Transaction(TransactionMode.Manual)]
    public class ConnectCommand : IExternalCommand
    {
        private static readonly HttpClient client = new HttpClient();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Simple prompt for connection test
                string prompt = "Hello! Just say 'Connection Successful' if you can hear me.";
                
                // Fix: Run in a separate task to avoid UI thread deadlock
                string response = Task.Run(() => SendRequestToMCPServer(prompt)).GetAwaiter().GetResult();

                TaskDialog.Show("MCP Response", response);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private async Task<string> SendRequestToMCPServer(string prompt)
        {
            try
            {
                var payload = new { command = prompt };
                string json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Fix: Use ConfigureAwait(false) to avoid capturing the sync context
                HttpResponseMessage response = await client.PostAsync("http://localhost:3005/revit-command", content).ConfigureAwait(false);
                
                if (response.IsSuccessStatusCode)
                {
                    string resultJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    dynamic result = JsonConvert.DeserializeObject(resultJson);
                    
                    if (result.tool_call != null)
                    {
                        return $"Connection Successful! (Server tried to call tool: {result.tool_call.name}. Use 'Chat' feature for tool execution.)";
                    }
                    
                    return result.result.ToString();
                }
                else
                {
                    return $"Error: Server returned {response.StatusCode}";
                }
            }
            catch (HttpRequestException)
            {
                return "Error: Could not connect to MCP Server. Is it running at http://localhost:3005?";
            }
        }
    }
}
