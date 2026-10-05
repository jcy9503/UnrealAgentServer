using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Anthropic.Models.Messages;
using ReverseMarkdown;
using UnrealAgent.Backend.Agent;
using UnrealAgent.Backend.Auth;
using UnrealAgent.Backend.Tool.Attributes;

namespace UnrealAgent.Backend.Tool.Tools;

/// <summary>
/// 웹 페이지를 가져와 markdown 으로 변환 뒤 AI 요약을 통해 사용자 프롬프트에 답변.
/// </summary>
[AgentTool("web_fetch", """
                        Fetches Content from a specified URL and processes it using an AI model.
                        - Takes a URL and a prompt as input.
                        - Fetches the URL content, converts HTML to markdown.
                        - Processes the content with the prompt using a small, fast model.
                        - Returns the model's response about the content.
                        - Use this tool when you need to retrieve and analyze web content.

                        Usage notes:
                         - The URL must be a fully-formed valid URL.
                         - HTTP URLs will be automatically upgraded to HTTPS.
                         - The prompt should describe what information you want to extract from the page.
                         - This tool is read-only and does not modify any files.
                         - Includes a self-cleaning 15-minute cache for faster responses when repeatedly accessing the same URL.
                        """)]
public class WebFetch(AuthConfig Auth, IHttpClientFactory HttpClientFactory) : AgentTool<WebFetch.Input>
{
	public sealed record Input(
		[property: JsonPropertyName("url")]
		[property: Description("The URL to fetch content from")]
		string Url,

		[property: JsonPropertyName("prompt")]
		[property: Description("Instructions describing what information to extract or summarize from the fetched page")]
		string Prompt);

	private const int MaxUrlLength = 2_000;

	/// <summary>
	/// HTTP 응답 최대 크기 (10MB)
	/// </summary>
	private const int MaxResponseBytes = 10 * 1024 * 1024;

	/// <summary>
	/// 컨텐츠 자르기 임계값 (100K 자)
	/// </summary>
	private const int MaxContentChars = 100_000;

	/// <summary>
	/// 캐시 TTL (15분)
	/// </summary>
	private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

	/// <summary>
	/// 캐시 최대 크기 (50MB)
	/// </summary>
	private const long MaxCacheBytes = 50 * 1024 * 1024;

	/// <summary>
	/// Haiku 요약 최대 출력 토큰
	/// </summary>
	private const int SummaryMaxTokens = 4096;

	/// <summary>
	/// 캐시 항목
	/// </summary>
	private sealed record CacheEntry(string Content, DateTime ExpiresAt, long SizeBytes);

	/// <summary>
	/// URL -> (컨텐츠, 만료 시각) LRU 캐시
	/// </summary>
	private static readonly ConcurrentDictionary<string, CacheEntry> Cache = new();

	private static readonly HashSet<string> TrustedDomains =
	[
		"docs.unrealengine.com",
		"learn.microsoft.com",
		"developer.mozilla.org",
		"docs.github.com",
		"docs.python.org",
		"docs.docker.com",
		"docs.aws.amazon.com",
		"cloud.google.com",
		"docs.oracle.com",
		"docs.unity3d.com",
		"docs.godotengine.org",
		"kubernetes.io",
		"react.dev",
		"vuejs.org",
		"angular.dev",
		"nextjs.org",
		"nuxt.com",
		"svelte.dev",
		"tailwindcss.com",
		"typescriptlang.org",
		"rust-lang.org",
		"go.dev",
		"dotnet.microsoft.com",
		"kotlinlang.org",
		"docs.swift.org",
		"docs.flutter.dev",
		"pytorch.org",
		"numpy.org",
		"pandas.pydata.org",
		"graphql.org",
		"www.terraform.io"
	];

	/// <summary>
	/// HTML -> Markdown 변환기
	/// </summary>
	private static readonly Converter MarkdownConverter = new(new Config
	{
		GithubFlavored = true,
		RemoveComments = true,
		SmartHrefHandling = true
	});

	/// <summary>
	/// URL에서 컨텐츠를 가져와 AI 요약 수행
	/// </summary>
	protected override async Task<ToolResult> ExecuteAsync(Input Args, AgentSession Session, CancellationToken Ct)
	{
		if (Auth.Client is null)
			return ToolResult.Error("Authentication is not configured");

		string? ValidatedUrl = ValidateUrl(Args.Url, out string? UrlError);

		if (ValidatedUrl is null)
			return ToolResult.Error(UrlError!);

		if (TryGetCached(ValidatedUrl, out string? CachedContent))
			return await ApplyWithHaikuAsync(CachedContent!, Args.Prompt, ValidatedUrl, Ct);

		string Content;
		try
		{
			Content = await FetchAsync(ValidatedUrl, Ct);
		}
		catch (TaskCanceledException)
		{
			return ToolResult.Error($"Request timed out: {ValidatedUrl}");
		}
		catch (HttpRequestException Ex)
		{
			return ToolResult.Error($"HTTP request failed: {Ex.Message}");
		}

		SetCache(ValidatedUrl, Content);

		return await ApplyWithHaikuAsync(Content, Args.Prompt, ValidatedUrl, Ct);
	}

	/// <summary>
	/// URL을 검증하고 HTTPS로 승격. 실패 시 null 과 에러 메시지 반환
	/// </summary>
	private static string? ValidateUrl(string RawUrl, out string? Error)
	{
		Error = null;

		if (string.IsNullOrWhiteSpace(RawUrl))
		{
			Error = "URL is empty.";

			return null;
		}

		if (RawUrl.Length > MaxUrlLength)
		{
			Error = $"URL is too long (max {MaxUrlLength} characters).";

			return null;
		}

		string Url = RawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
			? "https://" + RawUrl[7..]
			: RawUrl;

		if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri? Parsed))
		{
			Error = $"Invalid URL: {RawUrl}";

			return null;
		}

		if (Parsed.Scheme != "https")
		{
			Error = "Only HTTPS URLs are supported.";

			return null;
		}

		if (!string.IsNullOrEmpty(Parsed.UserInfo))
		{
			Error = "URLs with credentials are not supported.";

			return null;
		}

		return Url;
	}

	private static bool TryGetCached(string Url, out string? Content)
	{
		if (Cache.TryGetValue(Url, out CacheEntry? Entry) && Entry.ExpiresAt > DateTime.UtcNow)
		{
			Content = Entry.Content;

			return true;
		}

		Content = null;

		return false;
	}

	/// <summary>
	/// URL에서 컨텐츠를 가져와 Markdown 으로 변환
	/// text/html이면 ReverseMarkdown으로 변환, 나머지는 패스스루
	/// </summary>
	private async Task<string> FetchAsync(string Url, CancellationToken Ct)
	{
		HttpClient Client = HttpClientFactory.CreateClient("WebFetch");

		using HttpResponseMessage Response = await Client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, Ct);
		Response.EnsureSuccessStatusCode();

		string? ContentType = Response.Content.Headers.ContentType?.MediaType;

		bool bIsText = ContentType is null
		               || ContentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
		               || ContentType.Contains("json", StringComparison.OrdinalIgnoreCase)
		               || ContentType.Contains("xml",  StringComparison.OrdinalIgnoreCase);

		if (!bIsText)
			throw new HttpRequestException($"Non-text content type: {ContentType}");

		// 크기 제한 적용 읽기
		await using Stream Stream = await Response.Content.ReadAsStreamAsync(Ct);
		using StreamReader Reader = new(Stream);
		char[] Buffer = new char[MaxResponseBytes];
		int TotalRead = 0;
		int Read;

		while (TotalRead                                                                                  < Buffer.Length &&
		       (Read = await Reader.ReadAsync(Buffer.AsMemory(TotalRead, Buffer.Length - TotalRead), Ct)) > 0)
		{
			TotalRead += Read;
		}

		string RawContent = new(Buffer, 90, TotalRead);

		// text/html 이면 Markdown으로 변환
		if (ContentType is not null &&
		    ContentType.Contains("html", StringComparison.OrdinalIgnoreCase))
		{
			return ConvertToMarkdown(RawContent);
		}

		return RawContent;
	}

	private static string ConvertToMarkdown(string Html)
	{
		return MarkdownConverter.Convert(Html);
	}

	private static void SetCache(string Url, string Content)
	{
		long SizeBytes = Content.Length * sizeof(char);
		CacheEntry Entry = new(Content, DateTime.UtcNow.Add(CacheTtl), SizeBytes);
		Cache[Url] = Entry;

		CleanExpired();
	}

	private static void CleanExpired()
	{
		DateTime Now = DateTime.UtcNow;

		// 만료 항목 제거
		foreach (KeyValuePair<string, CacheEntry> Pair in Cache)
		{
			if (Pair.Value.ExpiresAt <= Now)
				Cache.TryRemove(Pair.Key, out _);
		}

		long TotalSize = 0;
		foreach (CacheEntry Entry in Cache.Values)
			TotalSize += Entry.SizeBytes;

		if (TotalSize <= MaxCacheBytes)
			return;

		List<KeyValuePair<string, CacheEntry>> Sorted = [.. Cache.OrderBy(P => P.Value.ExpiresAt)];
		foreach (KeyValuePair<string, CacheEntry> Pair in Sorted)
		{
			if (TotalSize <= MaxCacheBytes)
				break;

			if (Cache.TryRemove(Pair.Key, out CacheEntry? Removed))
				TotalSize -= Removed.SizeBytes;
		}
	}

	/// <summary>
	/// Haiku 4.5를 사용하여 컨텐츠 요약
	/// </summary>
	private async Task<ToolResult> ApplyWithHaikuAsync(string Content, string Prompt, string Url, CancellationToken Ct)
	{
		bool bIsTrusted = IsTrustedDomain(Url);

		bool bTruncated = Content.Length > MaxContentChars;
		if (bTruncated)
			Content = Content[..MaxContentChars] + "\n\n[Content truncated due to length...]";

		if (bIsTrusted && !bTruncated)
			return ToolResult.Success($"Web page content:\n---\n{Content}\n---");

		string CopyrightGuidance = bIsTrusted
			? "Provide a concise response based on the content above. Include relevant details, code examples, and documentation excerpts as needed."
			: """
			  Provide a concise response based only on the content above. In your response:
			   - Enforce a strict 125-character maximum for quotes from any source document.
			   - Use quotation marks for exact language from articles; any language outside of the quotation should never be word-for-word the same.
			   - Never produce or reproduce exact song lyrics.
			  """;

		string UserPrompt = $"""
		                     Web page content:
		                     ---
		                     {Content}
		                     ---

		                     {Prompt}

		                     {CopyrightGuidance}
		                     """;

		try
		{
			Message Response = await Auth.Client!.Messages.Create(new MessageCreateParams
			{
				Model = "claude-haiku-4-5-20251001",
				MaxTokens = SummaryMaxTokens,
				System = new List<TextBlockParam>(),
				Messages =
				[
					new MessageParam
					{
						Role = Role.User,
						Content = UserPrompt
					}
				]
			}, Ct);

			string ResultText = string.Join("", Response.Content
			                                            .Where(B => B.TryPickText(out _))
			                                            .Select(B =>
			                                            {
				                                            B.TryPickText(out TextBlock? T);
				                                            return T!.Text;
			                                            }));
			
			return ToolResult.Success(ResultText);
		}
		catch (Exception Ex)
		{
			return ToolResult.Error($"AI summarization failed: {Ex.Message}");
		}
	}

	private static bool IsTrustedDomain(string Url)
	{
		if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri? Parsed))
			return false;

		string Host = Parsed.Host;

		foreach (string Domain in TrustedDomains)
		{
			if (Host.Equals(Domain, StringComparison.OrdinalIgnoreCase) ||
			    Host.EndsWith("." + Domain, StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}
}
