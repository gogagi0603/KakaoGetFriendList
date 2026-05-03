using System.Net.Http.Headers;
using System.Text.Json;
using Kakao.Models;

namespace Kakao.Services;

public class KakaoFriendsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public KakaoFriendsService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<KakaoFriend>> GetAllFriendsAsync(string accessToken)
    {
        var allFriends = new List<KakaoFriend>();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        int offset = 0;
        const int limit = 100;

        while (true)
        {
            var url = $"https://kapi.kakao.com/v1/api/talk/friends?offset={offset}&limit={limit}&order=asc&friend_order=nickname";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new KakaoApiException(response.StatusCode, errorBody);
            }

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<KakaoFriendsResponse>(json, JsonOptions);

            if (result?.Elements == null || result.Elements.Count == 0)
                break;

            allFriends.AddRange(result.Elements);

            if (result.AfterUrl == null || allFriends.Count >= result.TotalCount)
                break;

            offset += limit;
        }

        return allFriends;
    }
}
