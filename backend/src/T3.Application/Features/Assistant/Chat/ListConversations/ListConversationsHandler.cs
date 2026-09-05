using Microsoft.EntityFrameworkCore;
using T3.Application.Common.Interfaces;
using T3.Application.Common.Paging;
using T3.Application.Common.Results;

namespace T3.Application.Features.Assistant.Chat.ListConversations;

/// <summary>
/// Çağıranın kendi sohbetleri. Rol süzgeci yok, olamaz da: sohbet metni
/// kullanıcının kendi serbest metni, SuperAdmin'in bile başkasının sohbetini
/// okuması için bir iş gerekçesi yok — bu yüzden kapsam her rol için
/// "yalnızca kendi satırların".
/// </summary>
public sealed class ListConversationsHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<PagedResult<ConversationListItemResponse>>> Handle(
        ListConversationsRequest request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not { } userId)
            return Error.Forbidden("Sohbet geçmişi için oturum açmalısınız.");

        var query = db.AiConversations
            .AsNoTracking()
            .Where(ConversationAccess.OwnedBy(userId));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(c => c.LastMessageAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new ConversationListItemResponse(
                c.Id, c.Title, c.LastMessageAt, c.Messages.Count))
            .ToListAsync(ct);

        return new PagedResult<ConversationListItemResponse>(
            items, request.Page, request.PageSize, total);
    }
}
