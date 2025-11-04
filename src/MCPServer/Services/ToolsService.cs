using System.Text.Json.Nodes;

namespace MCPServer.Services;

public class ToolsService
{
    private readonly OllamaService _ollama;

    public ToolsService(OllamaService ollama)
    {
        _ollama = ollama;
    }

    public async Task<string> Execute(string toolName, JsonNode? arguments)
    {
        return toolName switch
        {
            "process_transcript" => await ProcessTranscript(
                arguments?["transcript"]?.ToString() ?? ""),

            "summarize_text" => await _ollama.Summarize(
                arguments?["text"]?.ToString() ?? ""),

            "translate_text" => await _ollama.Translate(
                arguments?["text"]?.ToString() ?? "",
                arguments?["target_language"]?.ToString() ?? "français"),
        };
    }

    public async Task<string> ProcessTranscript(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            return "Erreur: Transcript vide";

        Console.WriteLine($"[ProcessTranscript] Démarrage du traitement du transcript...");

        var detectedLanguage = await _ollama.DetectLanguage(transcript);
        Console.WriteLine($"[ProcessTranscript] Langue détectée: {detectedLanguage}");

        var processedText = transcript;

        if (!detectedLanguage.Contains("français", StringComparison.OrdinalIgnoreCase) &&
            !detectedLanguage.Contains("french", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[ProcessTranscript] Traduction en français...");
            processedText = await _ollama.Translate(processedText, "français");
        }

        Console.WriteLine("[ProcessTranscript] Génération du résumé...");
        processedText = await _ollama.Summarize(processedText);

        Console.WriteLine($"[ProcessTranscript] - {processedText}");

        return processedText;
    }
}
