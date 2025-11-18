# Vonage x SSW - Workshop

A 2-hour hands-on workshop on building an intelligent voicemail system that records missed calls, generates AI-powered summaries, and sends them via SMS to the call recipient.

## 📋 Overview

This workshop guides you through creating a modern voicemail application that leverages Vonage telecommunications APIs, cloud storage, and AI to provide smart call management. When a call is missed, the system records a voicemail, transcribes it, generates a concise summary using AI, and delivers it to the intended recipient via SMS. Optionally, we will implement communication with Supabase storage to store transcripts, summaries, and audio in a storage bucket that we will link in our SMS.
For the purpose of this workshop, the workflow is a bit different (see "Application Flow" section above) but the principle remains the same.Retry

## 🎯 Learning Objectives

- Build a telecommunications application using Vonage API
- Implement Clean Architecture principles in .NET
- Orchestrate distributed applications with .NET Aspire
- Create custom MCP (Model Context Protocol) servers and clients
- Integrate AI-powered summarization with local LLM using Ollama
- Apply clean code principles
- Manage cloud storage with Supabase

## 🛠️ Technologies Used

- **Vonage API** - Voice calls, SMS messaging, and call recording
- **.NET 9+** - Application framework
- **.NET Aspire** - Cloud-native orchestration and observability
- **Clean Architecture** - Separation of concerns and maintainability
- **MCP (Model Context Protocol)** - Custom server/client for AI integration
- **Local LLM - Ollama** - AI-powered code assistance and summarization
- **Claude Code local MCP** - Using our local MCP with Claude Code
- **Supabase** - Storage for transcripts, audio files, and summaries

## 🔄 Application Flow

1. **Incoming Call** → We are using our API to simulate a call to a number
2. **Voicemail Recording** → The called person will leave a voice message
3. **Transcription** → Audio converted to text
4. **AI Summarization** → MCP client requests Local LLM to summarize
5. **Storage** → Transcript, audio, and summary saved to Supabase
6. **SMS Notification** → Summary sent to call recipient via Vonage SMS API

## 🚀 Getting Started

### 1. Prerequisites

- .NET SDK installed with recent version (.NET 8+)
- Git installed
- Vonage - Go to https://developer.vonage.com/ and create an account
- Ngrok - Go to https://ngrok.com/, create an account and install ngrok client
- Ollama - Go to https://ollama.com/ and install Ollama on your respective OS
- Supabase - Go to https://supabase.com/ and create an account

### 2. Configuration

#### Vonage

In Vonage Dashboard:
- Go to "Application"
- Create a new application
  - Give it a name
  - Generate Authentication key and keep it for now
  - Add "Voice" and "Messages" as capabilities

#### Ollama

- Launch a terminal
- Check if Ollama is installed correctly: `ollama -v`
- Install a local LLM:
  - You should pick one that fits your GPU and CPU power
  - For this workshop, we choose "gemma3:4b" (3GB) which is a good compromise between power and intelligence
  - Install the model locally: `ollama pull gemma3:4b`
  - Check if the model is installed: `ollama list`
- To launch your Ollama local server, just run `ollama start`

#### Supabase

- In your dashboard, create a basic project (no need for fancy configuration)
- Go to your fresh project:
  - Go to "Storage"
  - Create a new bucket
  - Give it a name, and choose if it should be public or private

### The Project

Our workshop follows steps that are reflected by our branches. If you want to launch the full working app, go to the last branch - "10--Adding_storage_audio_link_to_SMS"

#### Steps of the Workshop:

- Master - Minimal Clean Architecture template setup with Aspire
- Step 1 - Dependencies & config setup
- Step 2 - First call endpoint
- Step 3 - Get transcript from the call
- Step 4 - Sending transcript via SMS
- Step 5 - Creating MCP Server project and basic configuration
- Step 6 - Creating summary using MCP Server with HTTP request (from our app)
- Step 7 - Creating summary using MCP Server stdio that can be used by desktop LLM (e.g., Claude)
- Step 8 - MCP Client that communicates with our MCP Server and SMS update with the summary
- Step 9 - Uploading transcript/summary/audio to Supabase storage
- Step 10 - Adding Supabase audio link to our SMS

### How to Run the Project

#### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/custom-voicemail-workshop.git
cd custom-voicemail-workshop
```

#### 2. Configure Environment Variables

Create an `appsettings.Development.json` file in the WebAPI project:

```json
{
  "SupabaseStorage": {
    "BucketName": "recordings", // Your Supabase bucket name
    "ProjectUrl": "your-supabase-project-url", // You'll find it at Settings | Data API 
    "ServiceRoleKey": "your-supabase-public-api-key" // You'll find it at Settings | API Keys
  },
  "Workshop": {
    "FromNumber": "+447418366408", // Vonage Application number
    "WebhookBaseUrl": "https://d33fb7bbaab6.ngrok-free.app" // Tunnelized URL generated by Ngrok
  },
  "vonage": {
    "Application.Id": "2b19346e-cd5e-487c-a55f-ea7731eaad8d", // Vonage application ID 
    "Application.Key": "your-vonage-key" // That you should have kept from the previous step
  }
}
```

#### 3. Ngrok

- Run ngrok server:
  - `ngrok http your-api-endpoint` (by default it would be localhost:7255)
  - Grab the tunnelized URL and paste it in your `appsettings.Development.json` as shown above

#### 4. Ollama

- If you choose another local LLM model than `gemma3:4b`:
  - Go to `src/MCPServer/Program.cs`
  - Change this line `var _ = new OllamaService("http://localhost:11434", "gemma3:4b");` with your corresponding LLM model
- Run ollama server: `ollama start`

#### 5. Run with Aspire

- Use your IDE to point to the AppHost project and start
- In command line:

```bash
cd src/VoicemailApp.AppHost
dotnet run
```

The Aspire dashboard will open, showing all service endpoints and telemetry.

#### 6. Initiate the Call

- Go to your Scalar API endpoint dashboard - https://localhost:7255/scalar/v1
- Click on the InitiateCall endpoint
- Test the endpoint by adding the number you want to call in the parameter - start with country telephone code (e.g., `+33612984534`)

**Happy Coding! 🚀**