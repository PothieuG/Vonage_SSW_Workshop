using OllamaSharp;
using OllamaSharp.Models;
using System.Text;

namespace MCPServer.Services;

public class OllamaService
{
    private readonly OllamaApiClient _client;

    public OllamaService(string url, string model)
    {
        _client = new OllamaApiClient(url, model);
    }

    public async Task<string> DetectLanguage(string text)
    {
        var prompt = $"""
        Détecte la langue du texte suivant et réponds uniquement avec le nom de la langue (ex: "français", "anglais", "espagnol").

        Texte: {text}

        Langue:
        """;

        return await Generate(prompt);
    }

    public async Task<string> Summarize(string input)
    {
        var prompt = $"""
        Tu es un assistant chargé de reformuler des messages vocaux en résumés professionnels à la troisième personne.
        MESSAGE VOCAL D’ORIGINE : 
        {input}

        TÂCHE :  
        Créer un résumé clair et professionnel en français, rédigé à la troisième personne (2 à 3 phrases maximum).  

        RÈGLES :  
        - Utiliser la troisième personne (ex. : « L’appelant a mentionné… », « Une personne a appelé pour… », « Le client souhaite… »)  
        - Extraire UNIQUEMENT les informations clés et l’intention principale  
        - Supprimer les mots de remplissage, répétitions et artefacts de transcription  
        - Rédiger dans un français naturel et professionnel, comme si l’on rapportait l’appel à un tiers  
        - NE PAS copier le texte original mot pour mot  
        - NE PAS mentionner les termes « transcription » ou « résumé »  
        - Se concentrer sur : QUI fait QUOI, QUAND, OÙ, et POURQUOI  

        EXEMPLE :  
        Entrée : « Bonjour, c’est bien la boulangerie de la boustifaille ? Si oui, je voudrais commander 1450 baguettes pour ce weekend, merci. »  
        Sortie : « Une personne a appelé pour confirmer qu’il s’agissait bien de la boulangerie de la boustifaille et souhaite commander 1450 baguettes pour ce weekend. »  

        RÉSUMÉ PROFESSIONNEL EN FRANÇAIS :
        """;

        return await Generate(prompt, new RequestOptions
        {
            Temperature = 0.3f,
            TopP = 0.9f,
            RepeatPenalty = 1.2f,
            NumPredict = 150
        });
    }

    public async Task<string> Translate(string input, string targetLanguage)
    {
        var prompt = $"""
        Tu es un assistant chargé de reformuler des messages vocaux dans un style narratif naturel à la troisième personne.
        ORIGINAL VOICE MESSAGE (transcribed):
        {input}

        TÂCHE :  
        Reformuler ce message vocal en {targetLanguage} en adoptant une perspective naturelle à la troisième personne.  

        RÈGLES :  
        - Utiliser la troisième personne (ex. : « Quelqu’un a appelé pour… », « L’appelant a demandé… », « Une personne a mentionné… »)  
        - Fournir du contexte et de la clarté  
        - Conserver les mêmes informations tout en donnant l’impression d’un résumé destiné à une autre personne  
        - Supprimer les mots de remplissage et les artefacts de transcription  
        - Rédiger dans un {targetLanguage} clair et professionnel  
        - NE PAS traduire mot à mot, mais reformuler de manière naturelle  

        EXEMPLE :  
        Entrée : « Bonjour, c’est bien la bonne boulangerie ? J’ai besoin de 100 baguettes. »  
        Sortie : « Quelqu’un a appelé pour vérifier qu’il s’agissait bien de la bonne boulangerie et souhaite commander 100 baguettes. »  

        MESSAGE REFORMULÉ EN {targetLanguage} :
        """;

        return await Generate(prompt, new RequestOptions
        {
            Temperature = 0.4f,
            TopP = 0.9f,
            RepeatPenalty = 1.2f
        });
    }

    private async Task<string> Generate(string prompt, RequestOptions? options = null)
    {
        var responseBuilder = new StringBuilder();
        var request = new GenerateRequest { Prompt = prompt };

        if (options != null)
            request.Options = options;

        var responseStream = _client.GenerateAsync(request);

        await foreach (var response in responseStream)
        {
            if (response?.Response != null)
                responseBuilder.Append(response.Response);
        }

        return responseBuilder.ToString().Trim();
    }
}