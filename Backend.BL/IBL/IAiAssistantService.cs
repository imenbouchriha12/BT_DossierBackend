using Backend.DTO.Ai;

namespace Backend.BL.IBL
{
    /// <summary>
    /// Assistant IA conversationnel (type RAG) : l'agent pose une question en langage
    /// naturel, l'IA identifie l'intention, interroge les données applicatives puis
    /// génère une réponse en français via un LLM compatible OpenAI (avec repli déterministe).
    /// Fournit également le résumé automatique de dossier client (5 lignes).
    /// </summary>
    public interface IAiAssistantService
    {
        Task<ChatResponseDto> ChatAsync(ChatRequestDto request);
        Task<ClientSummaryDto> ResumerClientAsync(int idClient);
    }
}
