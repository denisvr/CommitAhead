using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using CommitAhead.Api.Features.AnalysisDrafts;
using CommitAhead.Api.Features.JobAnalyses;
using CommitAhead.Api.Tests.StudyItems;
using CommitAhead.Application.AI;

namespace CommitAhead.Api.Tests.JobAnalyses;

[Collection(StudyItemsApiCollection.Name)]
public class JobAnalysesEndpointTests
{
    private readonly StudyItemsTestWebApplicationFactory _factory;

    public JobAnalysesEndpointTests(StudyItemsTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static CreateJobAnalysisRequest ValidCreateRequest() => new("Senior Backend Engineer", "Job posting text.", "Some notes.");

    /// <summary>A hand-crafted minimal valid single-page PDF (never authored with PdfPig itself) — the real PdfPigTextExtractor runs unmodified in this test host, only IJobPostingStorage is faked.</summary>
    private static byte[] ValidMinimalPdf()
    {
        var contentStream = "BT /F1 24 Tf 100 700 Td (Hello World) Tj ET";
        var objects = new[]
        {
            "<</Type/Catalog/Pages 2 0 R>>",
            "<</Type/Pages/Kids[3 0 R]/Count 1>>",
            "<</Type/Page/Parent 2 0 R/MediaBox[0 0 612 792]/Resources<</Font<</F1 4 0 R>>>>/Contents 5 0 R>>",
            "<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>",
            $"<</Length {contentStream.Length}>>stream\n{contentStream}\nendstream",
        };

        using var stream = new MemoryStream();
        void Write(string s)
        {
            var bytes = Encoding.ASCII.GetBytes(s);
            stream.Write(bytes, 0, bytes.Length);
        }

        Write("%PDF-1.4\n");
        var offsets = new List<long>();
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(stream.Length);
            Write($"{i + 1} 0 obj{objects[i]}endobj\n");
        }

        var xrefOffset = stream.Length;
        Write($"xref\n0 {offsets.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            Write($"{offset:D10} 00000 n \n");
        }

        Write($"trailer<</Size {offsets.Count + 1}/Root 1 0 R>>\nstartxref\n{xrefOffset}\n%%EOF");
        return stream.ToArray();
    }

    private static MultipartFormDataContent UploadRequestContent(byte[] pdfBytes, string title, string fileName = "posting.pdf", string contentType = "application/pdf")
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent(title), "Title" },
        };
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "File", fileName);
        return content;
    }

    [Fact]
    public async Task Get_WithoutAnyToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/job-analyses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithNoAnalysesYet_ReturnsEmptyList()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync("/api/job-analyses", accessCookie);
        var results = await response.Content.ReadFromJsonAsync<List<JobAnalysisResponse>>(StudyItemsApiTestHelpers.JsonOptions);

        Assert.Empty(results!);
    }

    [Fact]
    public async Task GetById_WithNoSuchAnalysis_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendGetAsync($"/api/job-analyses/{Guid.NewGuid()}", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_ThenGetById_RoundTripsThePastedTextJobSource()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/job-analyses", accessCookie, ValidCreateRequest());
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var getResponse = await client.SendGetAsync($"/api/job-analyses/{created!.Id}", accessCookie);
        var analysis = await getResponse.Content.ReadFromJsonAsync<JobAnalysisResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal("Senior Backend Engineer", analysis!.Title);
        var pastedText = Assert.IsType<PastedTextResponse>(analysis.JobSource);
        Assert.Equal("Job posting text.", pastedText.Content);
        Assert.Empty(analysis.Requirements);
        Assert.Empty(analysis.Gaps);
    }

    [Fact]
    public async Task Put_UpdatesTheTitle()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/job-analyses", accessCookie, ValidCreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var putResponse = await client.SendMutatingAsync(
            HttpMethod.Put, $"/api/job-analyses/{created!.Id}", accessCookie, new UpdateJobAnalysisRequest("New title", null));
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/job-analyses/{created.Id}", accessCookie);
        var analysis = await getResponse.Content.ReadFromJsonAsync<JobAnalysisResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal("New title", analysis!.Title);
    }

    [Fact]
    public async Task Put_WithNoSuchAnalysis_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(
            HttpMethod.Put, $"/api/job-analyses/{Guid.NewGuid()}", accessCookie, new UpdateJobAnalysisRequest("Title", null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheAnalysis()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/job-analyses", accessCookie, ValidCreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var deleteResponse = await client.SendMutatingAsync(HttpMethod.Delete, $"/api/job-analyses/{created!.Id}", accessCookie);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.SendGetAsync($"/api/job-analyses/{created.Id}", accessCookie);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNoSuchAnalysis_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(HttpMethod.Delete, $"/api/job-analyses/{Guid.NewGuid()}", accessCookie);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostUpload_WithAValidPdf_CreatesAnAnalysisWithTheUploadedFileJobSource()
    {
        var ownerUserId = Guid.NewGuid();
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(ownerUserId);

        var postResponse = await client.SendMultipartAsync(
            HttpMethod.Post, "/api/job-analyses/upload", accessCookie, UploadRequestContent(ValidMinimalPdf(), "Senior Backend Engineer"));
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var getResponse = await client.SendGetAsync($"/api/job-analyses/{created!.Id}", accessCookie);
        var analysis = await getResponse.Content.ReadFromJsonAsync<JobAnalysisResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal("Senior Backend Engineer", analysis!.Title);
        var uploadedFile = Assert.IsType<UploadedFileResponse>(analysis.JobSource);
        Assert.Equal("posting.pdf", uploadedFile.OriginalFileName);
        Assert.Equal("application/pdf", uploadedFile.MimeType);
        Assert.Equal("Hello World", uploadedFile.ExtractedText);

        var uploadCall = Assert.Single(_factory.JobPostingStorage.UploadCalls);
        Assert.StartsWith($"{ownerUserId:D}/", uploadCall.Key, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostUpload_WithAnEmptyFile_ReturnsUnprocessableEntity()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMultipartAsync(
            HttpMethod.Post, "/api/job-analyses/upload", accessCookie, UploadRequestContent([], "Title"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PostUpload_WithoutThePdfMagicBytes_ReturnsUnprocessableEntity()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var notAPdf = Encoding.ASCII.GetBytes("Not a PDF at all.");

        var response = await client.SendMultipartAsync(
            HttpMethod.Post, "/api/job-analyses/upload", accessCookie, UploadRequestContent(notAPdf, "Title"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Analyze_WithAValidJobAnalysis_ReturnsCreatedWithADraftId()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/job-analyses", accessCookie, ValidCreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var response = await client.SendMutatingAsync(
            HttpMethod.Post, $"/api/job-analyses/{created!.Id}/analyze", accessCookie, new AnalyzeCommandRequest($"key-{Guid.NewGuid()}"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AnalyzeCommandResponse>(StudyItemsApiTestHelpers.JsonOptions);
        Assert.Equal(AnalyzeCommandOutcome.Created, body!.Outcome);
        Assert.NotNull(body.AnalysisDraftId);
    }

    [Fact]
    public async Task Analyze_WithNoSuchJobAnalysis_ReturnsNotFound()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());

        var response = await client.SendMutatingAsync(
            HttpMethod.Post, $"/api/job-analyses/{Guid.NewGuid()}/analyze", accessCookie, new AnalyzeCommandRequest("key-1"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Analyze_ReplayingTheSameIdempotencyKey_ReturnsAlreadyCompleted_WithTheSameDraftId()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/job-analyses", accessCookie, ValidCreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);
        var idempotencyKey = $"key-{Guid.NewGuid()}";

        var first = await client.SendMutatingAsync(HttpMethod.Post, $"/api/job-analyses/{created!.Id}/analyze", accessCookie, new AnalyzeCommandRequest(idempotencyKey));
        var firstBody = await first.Content.ReadFromJsonAsync<AnalyzeCommandResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var second = await client.SendMutatingAsync(HttpMethod.Post, $"/api/job-analyses/{created.Id}/analyze", accessCookie, new AnalyzeCommandRequest(idempotencyKey));
        var secondBody = await second.Content.ReadFromJsonAsync<AnalyzeCommandResponse>(StudyItemsApiTestHelpers.JsonOptions);

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(AnalyzeCommandOutcome.AlreadyCompleted, secondBody!.Outcome);
        Assert.Equal(firstBody!.AnalysisDraftId, secondBody.AnalysisDraftId);
    }

    [Fact]
    public async Task Analyze_WithABlankIdempotencyKey_ReturnsBadRequest()
    {
        var (client, accessCookie) = await _factory.CreateAuthenticatedClientAsync(Guid.NewGuid());
        var postResponse = await client.SendMutatingAsync(HttpMethod.Post, "/api/job-analyses", accessCookie, ValidCreateRequest());
        var created = await postResponse.Content.ReadFromJsonAsync<JobAnalysisCreatedResponse>(StudyItemsApiTestHelpers.JsonOptions);

        var response = await client.SendMutatingAsync(HttpMethod.Post, $"/api/job-analyses/{created!.Id}/analyze", accessCookie, new AnalyzeCommandRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
