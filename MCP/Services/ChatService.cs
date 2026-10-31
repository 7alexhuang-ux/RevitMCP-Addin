using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AlexMCP.Tools;
using AlexMCP.Utils;

namespace AlexMCP.Services
{
    public class ChatSession
    {
        public string id { get; set; }
        public string title { get; set; }
        public long timestamp { get; set; }
        public List<ChatMessage> messages { get; set; }
    }

    public class ChatMessage
    {
        public string role { get; set; }
        public List<MessagePart> parts { get; set; }
    }

    public class MessagePart
    {
        public string text { get; set; }
    }

    public class ChatService
    {
        private readonly HttpClient _client;
        private readonly RevitEvent _revitEvent;
        private readonly ToolHandler _toolHandler;
        private const string BaseUrl = "http://localhost:3005";

        public ChatService(RevitEvent revitEvent, ToolHandler toolHandler)
        {
            _client = new HttpClient();
            _revitEvent = revitEvent;
            _toolHandler = toolHandler;
        }

        public async Task<List<string>> GetOllamaModels()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync($"{BaseUrl}/models/ollama").ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return new List<string>();
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                JObject data = JObject.Parse(json);
                List<string> models = new List<string>();
                foreach (var model in data["models"])
                {
                    models.Add(model["name"].ToString());
                }
                return models;
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task<List<ChatSession>> GetChatHistory()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync($"{BaseUrl}/history").ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return new List<ChatSession>();
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<List<ChatSession>>(json) ?? new List<ChatSession>();
            }
            catch
            {
                return new List<ChatSession>();
            }
        }

        public async Task<ChatSession> CreateNewChat()
        {
            try
            {
                HttpResponseMessage response = await _client.PostAsync($"{BaseUrl}/history", null).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<ChatSession>(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create chat: {ex.Message}");
            }
        }

        public async Task<ChatSession> LoadChatSession(string id)
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync($"{BaseUrl}/history/{id}").ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return null;
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonConvert.DeserializeObject<ChatSession>(json);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> SendMessageAsync(string message, string provider, string model, string sessionId)
        {
            try
            {
                var payload = new { command = message, provider = provider, model = model, sessionId = sessionId };
                string json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // 1. Send User Message to Server
                HttpResponseMessage response = await _client.PostAsync($"{BaseUrl}/revit-command", content).ConfigureAwait(false);
                
                if (!response.IsSuccessStatusCode) 
                {
                    string errorJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    try 
                    {
                        dynamic errorObj = JsonConvert.DeserializeObject(errorJson);
                        throw new Exception(errorObj.result.ToString());
                    }
                    catch
                    {
                        throw new Exception($"Server returned {response.StatusCode}: {errorJson}");
                    }
                }
                
                string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                dynamic result = JsonConvert.DeserializeObject(responseJson);

                // 2. Check if Server wants to call a Tool
                if (result.tool_call != null)
                {
                    string toolName = result.tool_call.name;
                    JObject args = result.tool_call.args;

                    // 3. Execute Tool on Revit Thread
                    string toolResult = await ExecuteToolOnRevitThread(toolName, args);

                    // 4. Send Tool Result back to Server
                    var resultPayload = new { tool_result = toolResult, sessionId = sessionId };
                    string resultJsonPayload = JsonConvert.SerializeObject(resultPayload);
                    var resultContent = new StringContent(resultJsonPayload, Encoding.UTF8, "application/json");

                    HttpResponseMessage finalResponse = await _client.PostAsync($"{BaseUrl}/revit-command/tool-result", resultContent).ConfigureAwait(false);
                    if (!finalResponse.IsSuccessStatusCode) throw new Exception($"Server returned {finalResponse.StatusCode} on tool result");
                    
                    string finalJson = await finalResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    dynamic finalResult = JsonConvert.DeserializeObject(finalJson);
                    
                    return finalResult.result.ToString();
                }

                return result.result.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception($"Communication error: {ex.Message}");
            }
        }

        private async Task<string> ExecuteToolOnRevitThread(string toolName, JObject args)
        {
            string result = "Error: Tool execution failed";
            
            try 
            {
                await _revitEvent.RunAsync(app =>
                {
                    try
                    {
                        result = _toolHandler.ExecuteTool(toolName, args);
                    }
                    catch (Exception ex)
                    {
                        result = $"Error executing tool {toolName}: {ex.Message}";
                    }
                });
            }
            catch (Exception ex)
            {
                result = $"Error scheduling tool execution: {ex.Message}";
            }

            return result;
        }
    }
}
