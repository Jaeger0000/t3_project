using T3.Application.Common.Results;
using T3.Application.Features.Assistant.Chat;
using T3.Application.Features.Assistant.Chat.GetConversation;
using T3.Application.Features.Assistant.Chat.ListConversations;
using T3.Domain.Assistant;
using T3.Domain.Identity;

namespace T3.Application.Tests;

/// <summary>
/// Sohbet geçmişinin satır düzeyi yetkisi. Sohbet metni kullanıcının kendi
/// serbest metni: rol ne olursa olsun başkasının sohbetine erişim yok. Kimlik
/// yoksa karar veritabanına hiç gitmemeli — <see cref="UnreachableDbContext"/>
/// bunu kanıtlıyor: sessizce çalışan bir sorgu testi düşürür.
/// </summary>
public class ConversationOwnershipTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();


    [Fact]
    public async Task Kimliksiz_sohbet_ucu_veritabanina_gitmeden_reddedilir()
    {
        var handler = new ChatHandler(
            new UnreachableDbContext(),
            runner: null!,
            FakeCurrentUser.Anonymous,
            audit: null!);

        var result = await handler.Handle(new ChatRequest(null, "merhaba"), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task Kimliksiz_sohbet_listesi_veritabanina_gitmeden_reddedilir()
    {
        var handler = new ListConversationsHandler(
            new UnreachableDbContext(), FakeCurrentUser.Anonymous);

        var result = await handler.Handle(new ListConversationsRequest(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task Kimliksiz_tek_sohbet_okumasi_veritabanina_gitmeden_reddedilir()
    {
        var handler = new GetConversationHandler(
            new UnreachableDbContext(), FakeCurrentUser.Anonymous);

        var result = await handler.Handle(Guid.NewGuid(), default);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorKind.Forbidden, result.Error!.Kind);
    }

    [Fact]
    public async Task Kimlikli_kullanicida_sahiplik_sorgusu_veritabanindan_gecer()
    {
        // Sahiplik süzgeci sorgunun içinde olmalı: rol ne olursa olsun (burada
        // SuperAdmin) kısayol yok, satır veritabanından okunur. Sorgunun
        // çalıştığını UnreachableDbContext'in patlaması gösteriyor.
        var handler = new GetConversationHandler(
            new UnreachableDbContext(), FakeCurrentUser.As(UserRole.SuperAdmin));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(Guid.NewGuid(), default));
    }

    [Fact]
    public void Baskasinin_sohbeti_sahiplik_suzgecinden_gecmez()
    {
        // Handler'ların kullandığı süzgecin ta kendisi: eşleşme olmadığı için
        // "kayıt var ama senin değil" durumu hiç oluşmaz, handler NotFound
        // döner — kimliğin varlığı doğrulanmamış olur.
        var mine = new AiConversation { OwnerUserId = Owner, Title = "benim" };
        var theirs = new AiConversation { OwnerUserId = Stranger, Title = "başkasının" };

        var owned = ConversationAccess.Owned(theirs.Id, Owner).Compile();

        Assert.False(owned(theirs));
        Assert.True(ConversationAccess.Owned(mine.Id, Owner).Compile()(mine));
    }

    [Fact]
    public void Sohbet_listesi_yalnizca_kendi_satirlarini_gorur()
    {
        AiConversation[] all =
        [
            new() { OwnerUserId = Owner, Title = "bir" },
            new() { OwnerUserId = Owner, Title = "iki" },
            new() { OwnerUserId = Stranger, Title = "üç" }
        ];

        var visible = all.AsQueryable().Where(ConversationAccess.OwnedBy(Owner)).ToArray();

        Assert.Equal(2, visible.Length);
        Assert.All(visible, c => Assert.Equal(Owner, c.OwnerUserId));
    }

    [Fact]
    public void Sohbet_basligi_ilk_sorudan_kisaltilir()
    {
        var question = new string('a', 200);

        var title = ChatHandler.Title(question);

        Assert.True(title.Length <= 81, "Başlık sütunu 200 karakter; başlık kısa kalmalı.");
        Assert.EndsWith("…", title);
        Assert.Equal("Kısa soru", ChatHandler.Title("  Kısa soru  "));
    }
}
