using System;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;

namespace SLTranslateCN;

internal static class ReportForwarder
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(5) };

    // An injectable transport lets the regression test verify routing without a live QQ account.
    internal static Func<Uri, string, string, Task<bool>> SendRequestAsync = SendAsync;

    internal static bool TryForward(ChatConfig? config, Player sender, string report)
    {
        if (config?.QqReportEnabled != true ||
            config.QqReportGroupId <= 0 || config.QqReportAtQq <= 0 ||
            !Uri.TryCreate(config.QqBotApiUrl, UriKind.Absolute, out Uri? baseUrl) ||
            (baseUrl.Scheme != Uri.UriSchemeHttp && baseUrl.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(baseUrl.UserInfo) ||
            !string.IsNullOrEmpty(baseUrl.Query) || !string.IsNullOrEmpty(baseUrl.Fragment))
        {
            return false;
        }

        Uri endpoint = new(baseUrl.AbsoluteUri.TrimEnd('/') + "/send_group_msg");
        string name = ChatService.Sanitize(sender.DisplayName ?? sender.Nickname ?? "未知玩家");
        string state = sender.IsAlive ? "存活" : "死亡/观战";
        string content = ChatService.Sanitize(report);
        if (content.Length > 1000) content = content.Substring(0, 1000) + "…";
        string message = $"\n【服务器举报｜当前无在线管理】\n玩家：{name} [{ChatService.GetFactionName(sender.Faction)}｜{state}｜{ChatService.GetRoleName(sender.Role)}]\n内容：{content}";
        string body = "{\"group_id\":" + config.QqReportGroupId +
            ",\"message\":[{\"type\":\"at\",\"data\":{\"qq\":\"" + config.QqReportAtQq +
            "\"}},{\"type\":\"text\",\"data\":{\"text\":\"" + EscapeJson(message) + "\"}}]}";

        _ = DeliverAsync(endpoint, config.QqBotAccessToken ?? string.Empty, body);
        return true;
    }

    private static async Task DeliverAsync(Uri endpoint, string token, string body)
    {
        try
        {
            if (!await SendRequestAsync(endpoint, token, body).ConfigureAwait(false))
            {
                Logger.Warn("[AdminReport] QQ 机器人未确认接收举报，请检查接口和机器人日志。");
            }
        }
        catch (Exception)
        {
            // Never write the endpoint, token or player report into exception logs.
            Logger.Warn("[AdminReport] QQ 机器人接口请求失败，请检查接口和机器人日志。");
        }
    }

    private static async Task<bool> SendAsync(Uri endpoint, string token, string body)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, endpoint);
        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        using HttpResponseMessage response = await Client.SendAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return false;
        string result = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return Regex.IsMatch(result, "\\\"status\\\"\\s*:\\s*\\\"ok\\\"") &&
               !Regex.IsMatch(result, "\\\"retcode\\\"\\s*:\\s*(?!0(?:[,}\\s]))-?[0-9]+");
    }

    private static string EscapeJson(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (char character in value)
        {
            switch (character)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                default:
                    if (character < 0x20) builder.Append("\\u").Append(((int)character).ToString("x4"));
                    else builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }
}
