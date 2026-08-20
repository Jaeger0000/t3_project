using T3.Application.Common.Paging;
using T3.Application.Features.Startups.SearchStartups;

namespace T3.Application.Tests;

/// <summary>
/// Sayfalama sınırları. Üst sınır olmadan bir istemci tek istekte tüm
/// ekosistemi çekebilir; kırpma bu yüzden istek tipinin kendisinde duruyor.
/// </summary>
public class PagedRequestTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    public void Sayfa_numarasi_en_az_bir_olur(int input, int expected)
    {
        var request = new SearchStartupsRequest { Page = input };

        Assert.Equal(expected, request.Page);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(50, 50)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    [InlineData(int.MaxValue, 100)]
    public void Sayfa_boyutu_yuze_kadar_kirpilir(int input, int expected)
    {
        var request = new SearchStartupsRequest { PageSize = input };

        Assert.Equal(expected, request.PageSize);
    }

    [Fact]
    public void Varsayilanlar_ilk_sayfa_ve_yirmi_kayit()
    {
        var request = new SearchStartupsRequest();

        Assert.Equal(1, request.Page);
        Assert.Equal(20, request.PageSize);
    }

    [Theory]
    [InlineData(0, 20, 0, false)]
    [InlineData(5, 20, 1, false)]
    [InlineData(41, 20, 3, true)]
    public void Toplam_sayfa_ve_sonraki_sayfa_hesabi(
        int totalCount, int pageSize, int expectedPages, bool expectedHasNext)
    {
        var result = new PagedResult<string>([], 1, pageSize, totalCount);

        Assert.Equal(expectedPages, result.TotalPages);
        Assert.Equal(expectedHasNext, result.HasNext);
    }
}
