namespace Backend.BL.IBL
{
    /// <summary>
    /// Client HTTP vers un fournisseur LLM compatible OpenAI (chat/completions) :
    /// Groq, OpenAI, xAI (Grok), Gemini, OpenRouter... pour les fonctionnalités IA :
    /// assistant conversationnel et résumé automatique de dossier.
    /// </summary>
    public interface ILlmApiService
    {
        /// <summary>
        /// Envoie une complétion de chat et retourne la réponse texte.
        /// Retourne null si l'API est indisponible ou non configurée.
        /// </summary>
        Task<string?> CompleteAsync(
            string systemPrompt,
            string userPrompt,
            double temperature = 0.5,
            int maxTokens = 1024,
            CancellationToken cancellationToken = default);
    }
}
