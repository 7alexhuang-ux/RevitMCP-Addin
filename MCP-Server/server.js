require('dotenv').config();
const express = require('express');
const cors = require('cors');
const { GoogleGenerativeAI } = require('@google/generative-ai');
const fs = require('fs');
const path = require('path');

const app = express();
const port = 3005;

app.use(cors());
app.use(express.json());

// Initialize Gemini
const genAI = new GoogleGenerativeAI(process.env.GEMINI_API_KEY);

const MODELS = {
    "auto": "gemini-flash-latest", // Default balanced model
    "gemini-flash-latest": "gemini-flash-latest",
    "gemini-2.0-flash": "gemini-2.0-flash"
};

// Define Tools
// Define Tools
// Define Tools
const tools = [
    {
        function_declarations: [
            // --- Query Tools ---
            {
                name: "get_project_info",
                description: "Get basic information about the current Revit project.",
            },
            {
                name: "get_all_levels",
                description: "Get a list of all levels in the project.",
            },
            {
                name: "get_current_view_info",
                description: "Get information about the currently active view.",
            },
            {
                name: "get_current_view_elements",
                description: "Get a list of elements in the current view.",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        category: { type: "STRING", description: "Optional: Filter by category name (e.g., 'OST_Walls')." }
                    }
                }
            },
            {
                name: "get_selected_elements",
                description: "Get a list of elements currently selected by the user.",
            },
            {
                name: "get_available_family_types",
                description: "Get a list of available family types (symbols) in the project.",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        category: { type: "STRING", description: "Optional: Filter by category name." }
                    }
                }
            },
            {
                name: "get_element_parameters",
                description: "Get all parameters for a specific element.",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        elementId: { type: "STRING", description: "The ElementId of the element." }
                    },
                    required: ["elementId"]
                }
            },
            {
                name: "get_current_view_rooms",
                description: "Get a list of all rooms in the current view with their name, number, and area.",
            },
            {
                name: "get_room_contained_elements",
                description: "Get a list of rooms in the current view and the doors/windows contained within them.",
            },

            // --- Creation Tools ---
            {
                name: "create_wall",
                description: "Create a wall on a specific level.",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        levelName: { type: "STRING", description: "The name of the level." },
                        startX: { type: "NUMBER", description: "Start X." },
                        startY: { type: "NUMBER", description: "Start Y." },
                        endX: { type: "NUMBER", description: "End X." },
                        endY: { type: "NUMBER", description: "End Y." }
                    },
                    required: ["levelName", "startX", "startY", "endX", "endY"]
                }
            },
            {
                name: "create_point_based_element",
                description: "Create a point-based element (e.g., Door, Window, Furniture).",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        symbolName: { type: "STRING", description: "The name of the Family Symbol to place." },
                        levelName: { type: "STRING", description: "The level to place it on." },
                        x: { type: "NUMBER", description: "X coordinate." },
                        y: { type: "NUMBER", description: "Y coordinate." },
                        familyName: { type: "STRING", description: "Optional: Family Name to disambiguate." }
                    },
                    required: ["symbolName", "levelName", "x", "y"]
                }
            },
            {
                name: "create_line_based_element",
                description: "Create a line-based element (e.g., Wall, Beam).",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        category: { type: "STRING", description: "The category (e.g., 'OST_Walls', 'OST_StructuralFraming')." },
                        levelName: { type: "STRING", description: "The level name." },
                        startX: { type: "NUMBER", description: "Start X." },
                        startY: { type: "NUMBER", description: "Start Y." },
                        endX: { type: "NUMBER", description: "End X." },
                        endY: { type: "NUMBER", description: "End Y." }
                    },
                    required: ["category", "levelName", "startX", "startY", "endX", "endY"]
                }
            },
            {
                name: "create_surface_based_element",
                description: "Create a surface-based element (e.g., Floor).",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        levelName: { type: "STRING", description: "The level name." },
                        points: {
                            type: "ARRAY",
                            description: "List of points {x, y}.",
                            items: {
                                type: "OBJECT",
                                properties: {
                                    x: { type: "NUMBER" },
                                    y: { type: "NUMBER" }
                                }
                            }
                        }
                    },
                    required: ["levelName", "points"]
                }
            },

            // --- Modification Tools ---
            {
                name: "delete_elements",
                description: "Delete multiple elements by ID.",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        elementIds: {
                            type: "ARRAY",
                            items: { type: "STRING" },
                            description: "List of ElementIds to delete."
                        }
                    },
                    required: ["elementIds"]
                }
            },
            {
                name: "delete_element",
                description: "Delete a single element by ID.",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        elementId: { type: "STRING", description: "The ElementId to delete." }
                    },
                    required: ["elementId"]
                }
            },
            {
                name: "modify_element",
                description: "Modify a parameter of an element.",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        elementId: { type: "STRING", description: "The ElementId." },
                        parameterName: { type: "STRING", description: "The parameter name." },
                        value: { type: "STRING", description: "The new value." }
                    },
                    required: ["elementId", "parameterName", "value"]
                }
            },
            {
                name: "reset_model",
                description: "Delete all elements in the current view (Reset).",
            },

            // --- Action Tools ---
            {
                name: "color_splash",
                description: "Color elements based on a parameter value.",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        parameterName: { type: "STRING", description: "The parameter to use for coloring." }
                    },
                    required: ["parameterName"]
                }
            },
            {
                name: "tag_walls",
                description: "Tag all walls in the current view.",
            },

            // --- Advanced Tools ---
            {
                name: "send_code_to_revit",
                description: "Send dynamic code to Revit (Not supported yet).",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        code: { type: "STRING", description: "The code to execute." }
                    }
                }
            },
            {
                name: "search_modules",
                description: "Search for external modules.",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        query: { type: "STRING", description: "Search query." }
                    }
                }
            },
            {
                name: "use_module",
                description: "Use an external module.",
                parameters: {
                    type: "OBJECT",
                    properties: {
                        moduleName: { type: "STRING", description: "The module name." }
                    }
                }
            }
        ]
    }
];

// Store conversation history (simple in-memory for MVP)
// In a real app, use a database or pass history from client
// Store conversation history in a file
const HISTORY_FILE = path.join(__dirname, 'chats.json');

function getHistory() {
    if (!fs.existsSync(HISTORY_FILE)) return [];
    try {
        return JSON.parse(fs.readFileSync(HISTORY_FILE, 'utf8'));
    } catch (e) {
        return [];
    }
}

function saveHistory(history) {
    fs.writeFileSync(HISTORY_FILE, JSON.stringify(history, null, 2));
}

function getSession(sessionId) {
    const history = getHistory();
    let session = history.find(h => h.id === sessionId);
    if (!session) {
        session = { id: sessionId, timestamp: Date.now(), messages: [] };
        // We don't save yet, wait for interaction
    }
    return session;
}

function saveSession(session) {
    const history = getHistory();
    const index = history.findIndex(h => h.id === session.id);
    session.timestamp = Date.now();
    if (index >= 0) history[index] = session;
    else history.push(session);
    saveHistory(history);
}

// --- New Endpoints ---

// GET /models/ollama
app.get('/models/ollama', async (req, res) => {
    try {
        const response = await fetch('http://localhost:11434/api/tags');
        if (!response.ok) throw new Error('Failed to fetch from Ollama');
        const data = await response.json();
        res.json(data);
    } catch (error) {
        console.error('Error fetching Ollama models:', error);
        res.status(500).json({ error: "Could not connect to Ollama. Is it running?" });
    }
});

// GET /history
app.get('/history', (req, res) => {
    const history = getHistory();
    // Sort by timestamp desc
    history.sort((a, b) => b.timestamp - a.timestamp);
    const summary = history.map(h => ({
        id: h.id,
        title: h.messages.length > 0 && h.messages[0].parts && h.messages[0].parts[0].text
            ? h.messages[0].parts[0].text.substring(0, 50)
            : "New Chat",
        timestamp: h.timestamp
    }));
    res.json(summary);
});

// GET /history/:id
app.get('/history/:id', (req, res) => {
    const session = getSession(req.params.id);
    res.json(session);
});

// POST /history (Create new chat)
app.post('/history', (req, res) => {
    const newSession = {
        id: Date.now().toString(),
        timestamp: Date.now(),
        messages: []
    };
    saveSession(newSession);
    res.json(newSession);
});

// Helper function to call Ollama
async function callOllama(command, model, session) {
    console.log(`Calling Ollama with model: ${model}`);
    const ollamaMessages = session.messages.map(m => ({
        role: m.role === 'model' ? 'assistant' : m.role,
        content: m.parts[0].text
    }));
    ollamaMessages.push({ role: 'user', content: command });

    const response = await fetch('http://localhost:11434/api/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            model: model,
            messages: ollamaMessages,
            stream: false
        })
    });

    if (!response.ok) throw new Error(`Ollama API error: ${response.statusText}`);
    const data = await response.json();
    return data.message.content;
}

app.post('/revit-command', async (req, res) => {
    try {
        const { command, model: requestedModel, provider, sessionId } = req.body;
        console.log(`Received command: ${command} (Provider: ${provider}, Session: ${sessionId})`);

        let session = getSession(sessionId || "default");

        // --- OLLAMA PROVIDER ---
        if (provider === 'ollama') {
            const ollamaModel = requestedModel || 'llama3';
            try {
                const reply = await callOllama(command, ollamaModel, session);

                session.messages.push({ role: "user", parts: [{ text: command }] });
                session.messages.push({ role: "model", parts: [{ text: reply }] });
                saveSession(session);

                return res.json({ result: reply });
            } catch (error) {
                console.error('Ollama Error:', error);
                return res.status(500).json({ result: "Ollama Error: " + error.message });
            }
        }

        // --- GEMINI PROVIDER (Default) ---
        if (!process.env.GEMINI_API_KEY) {
            return res.status(500).json({ result: "Error: GEMINI_API_KEY is not set in .env file." });
        }

        let modelName = MODELS["auto"];
        if (requestedModel && MODELS[requestedModel]) modelName = MODELS[requestedModel];
        else if (requestedModel) modelName = requestedModel;

        try {
            const model = genAI.getGenerativeModel({ model: modelName, tools: tools });
            const chat = model.startChat({
                history: session.messages,
            });

            const result = await chat.sendMessage(command);
            const response = await result.response;

            // Check for function call
            const functionCalls = response.functionCalls();
            if (functionCalls && functionCalls.length > 0) {
                const call = functionCalls[0];
                console.log(`Gemini wants to call tool: ${call.name}`);

                session.messages.push({ role: "user", parts: [{ text: command }] });
                session.messages.push({ role: "model", parts: response.candidates[0].content.parts });
                saveSession(session);

                res.json({
                    result: "Executing tool...",
                    tool_call: {
                        name: call.name,
                        args: call.args
                    }
                });
            } else {
                const text = response.text();
                console.log(`Gemini response: ${text}`);

                session.messages.push({ role: "user", parts: [{ text: command }] });
                session.messages.push({ role: "model", parts: [{ text: text }] });
                saveSession(session);

                res.json({ result: text });
            }
        } catch (geminiError) {
            console.error('Gemini Error:', geminiError);

            // FALLBACK TO OLLAMA
            console.log("⚠️ Gemini failed. Falling back to Ollama (gpt-oss:20b)...");

            try {
                const fallbackModel = "gpt-oss:20b"; // Hardcoded fallback model as requested
                const reply = await callOllama(command, fallbackModel, session);

                // Append a note that this is a fallback response
                const fallbackReply = `[Gemini Error: ${geminiError.message}. Fallback to ${fallbackModel}]\n\n${reply}`;

                session.messages.push({ role: "user", parts: [{ text: command }] });
                session.messages.push({ role: "model", parts: [{ text: fallbackReply }] });
                saveSession(session);

                return res.json({ result: fallbackReply });

            } catch (ollamaError) {
                console.error('Fallback Ollama Error:', ollamaError);
                // If both fail, return the original Gemini error (or a combined one)
                if (geminiError.message.includes("429") || geminiError.message.includes("ResourceExhausted")) {
                    return res.status(429).json({ result: "⚠️ AI 忙碌中 (Rate Limit)，且本地備援失敗。" });
                }
                return res.status(500).json({ result: `Gemini Error: ${geminiError.message}. Fallback Error: ${ollamaError.message}` });
            }
        }

    } catch (error) {
        console.error('Unexpected Error:', error);
        res.status(500).json({ result: "Unexpected Error: " + error.message });
    }
});

app.post('/revit-command/tool-result', async (req, res) => {
    try {
        const { tool_result, sessionId } = req.body;
        console.log(`Received tool result: ${tool_result} (Session: ${sessionId})`);

        let session = getSession(sessionId || "default");

        // Re-initialize chat with history
        const model = genAI.getGenerativeModel({ model: MODELS["auto"], tools: tools });
        const chat = model.startChat({
            history: session.messages,
        });

        // Send tool result to Gemini
        const result = await chat.sendMessage([
            {
                functionResponse: {
                    name: session.messages[session.messages.length - 1].parts[0].functionCall.name,
                    response: { result: tool_result }
                }
            }
        ]);

        const response = await result.response;
        const text = response.text();

        console.log(`Gemini final response: ${text}`);

        // Update history
        session.messages.push({ role: "function", parts: [{ functionResponse: { name: "unknown", response: { result: tool_result } } }] });
        session.messages.push({ role: "model", parts: [{ text: text }] });
        saveSession(session);

        res.json({ result: text });

    } catch (error) {
        console.error('Error processing tool result:', error);
        res.status(500).json({ result: "Error processing tool result: " + error.message });
    }
});

app.listen(port, () => {
    console.log(`MCP Server listening at http://localhost:${port}`);
});
