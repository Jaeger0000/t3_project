using T3.Application.Common.Paging;

namespace T3.Application.Features.Assistant.Chat.ListConversations;

/// <summary>Süzgeç yok: liste zaten tek bir kullanıcının kendi sohbetleri.</summary>
public sealed record ListConversationsRequest : PagedRequest;

public sealed record ConversationListItemResponse(
    Guid Id,
    string Title,
    DateTimeOffset LastMessageAt,
    int MessageCount);
