using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BalonmanoBunol.Domain.Teams;
using BalonmanoBunol.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BalonmanoBunol.Tests;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BALONMANO_TEST_DATABASE")))
            Skip = "Configura BALONMANO_TEST_DATABASE con una base PostgreSQL dedicada a pruebas.";
    }
}

public sealed class PostgreSqlIntegrationTests
{
    [PostgreSqlFact]
    public async Task PublicFeed_Registration_Csrf_AndAuthorDeletionFlowWorks()
    {
        var testDatabase = Environment.GetEnvironmentVariable("BALONMANO_TEST_DATABASE")!;
        var schema = $"test_{Guid.NewGuid():N}";
        var mediaRoot = Path.Combine(Path.GetTempPath(), "balonmano-api-tests", Guid.NewGuid().ToString("N"));
        var avatarRoot = Path.Combine(Path.GetTempPath(), "balonmano-api-tests", Guid.NewGuid().ToString("N"));
        var frontendRoot = Path.Combine(Path.GetTempPath(), "balonmano-api-tests", Guid.NewGuid().ToString("N"));
        var masterConnection = new NpgsqlConnectionStringBuilder(testDatabase);
        var schemaConnection = new NpgsqlConnectionStringBuilder(testDatabase) { SearchPath = schema };
        var previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__ClubDatabase");
        var previousMediaRoot = Environment.GetEnvironmentVariable("PostMedia__RootPath");
        var previousAvatarRoot = Environment.GetEnvironmentVariable("ProfileAvatar__RootPath");
        var previousFrontendRoot = Environment.GetEnvironmentVariable("Frontend__WebRoot");
        var previousAdminEmail = Environment.GetEnvironmentVariable("BootstrapAdmin__Email");
        var previousAdminPassword = Environment.GetEnvironmentVariable("BootstrapAdmin__Password");
        var administratorEmail = $"admin-{Guid.NewGuid():N}@example.es";
        const string administratorPassword = "BalonmanoTest1!";

        await using (var connection = new NpgsqlConnection(masterConnection.ConnectionString))
        {
            await connection.OpenAsync();
            await using var createSchema = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", connection);
            await createSchema.ExecuteNonQueryAsync();
        }

        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__ClubDatabase", schemaConnection.ConnectionString);
            Environment.SetEnvironmentVariable("PostMedia__RootPath", mediaRoot);
            Environment.SetEnvironmentVariable("ProfileAvatar__RootPath", avatarRoot);
            Directory.CreateDirectory(frontendRoot);
            await File.WriteAllTextAsync(Path.Combine(frontendRoot, "index.html"), "<html><body><div id=\"root\">demo spa</div></body></html>");
            await File.WriteAllTextAsync(Path.Combine(frontendRoot, "sw.js"), "self.addEventListener('fetch', () => {});");
            Environment.SetEnvironmentVariable("Frontend__WebRoot", frontendRoot);
            Environment.SetEnvironmentVariable("BootstrapAdmin__Email", administratorEmail);
            Environment.SetEnvironmentVariable("BootstrapAdmin__Password", administratorPassword);
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

            using var firstUser = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            using var secondUser = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            using var administrator = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            using var thirdUser = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            using var fourthUser = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            using var secondAdministrator = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            using var anonymousVisitor = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

            var health = await firstUser.GetAsync("/api/health");
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
            using (var ready = await firstUser.GetAsync("/api/ready"))
            {
                Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
                using var readyBody = JsonDocument.Parse(await ready.Content.ReadAsStringAsync());
                Assert.Equal("ready", readyBody.RootElement.GetProperty("status").GetString());
            }
            using (var readiness = await firstUser.GetAsync("/api/ready"))
            {
                Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);
                using var readinessBody = JsonDocument.Parse(await readiness.Content.ReadAsStringAsync());
                Assert.Equal("ready", readinessBody.RootElement.GetProperty("status").GetString());
            }
            using (var apiInfoResponse = await firstUser.GetAsync("/api"))
            {
                Assert.Equal(HttpStatusCode.OK, apiInfoResponse.StatusCode);
                using var apiInfo = JsonDocument.Parse(await apiInfoResponse.Content.ReadAsStringAsync());
                Assert.Equal("/docs", apiInfo.RootElement.GetProperty("documentation").GetString());
            }
            using (var documentation = await firstUser.GetAsync("/docs"))
            {
                Assert.Equal(HttpStatusCode.OK, documentation.StatusCode);
                Assert.Equal("text/html", documentation.Content.Headers.ContentType?.MediaType);
                Assert.Contains("API del Club Balonmano Buñol", await documentation.Content.ReadAsStringAsync());
            }
            using (var spaRoute = await firstUser.GetAsync("/auth"))
            {
                Assert.Equal(HttpStatusCode.OK, spaRoute.StatusCode);
                Assert.Contains("demo spa", await spaRoute.Content.ReadAsStringAsync());
                Assert.Equal("no-store, no-cache", spaRoute.Headers.CacheControl?.ToString());
            }
            using (var serviceWorker = await firstUser.GetAsync("/sw.js"))
            {
                Assert.Equal(HttpStatusCode.OK, serviceWorker.StatusCode);
                Assert.Equal("no-cache", serviceWorker.Headers.CacheControl?.ToString());
            }
            using (var spaRoute = await firstUser.GetAsync("/auth"))
            {
                Assert.Equal(HttpStatusCode.OK, spaRoute.StatusCode);
                Assert.Contains("demo spa", await spaRoute.Content.ReadAsStringAsync());
                Assert.Equal("no-store, no-cache", spaRoute.Headers.CacheControl?.ToString());
            }
            using (var serviceWorker = await firstUser.GetAsync("/sw.js"))
            {
                Assert.Equal(HttpStatusCode.OK, serviceWorker.StatusCode);
                Assert.Equal("no-cache", serviceWorker.Headers.CacheControl?.ToString());
            }
            using (var spaRoutesResponse = await firstUser.GetAsync("/auth"))
            {
                Assert.Equal(HttpStatusCode.OK, spaRoutesResponse.StatusCode);
                Assert.Contains("id=\"root\"", await spaRoutesResponse.Content.ReadAsStringAsync());
            }
            using (var serviceWorkerResponse = await firstUser.GetAsync("/sw.js"))
            {
                Assert.Equal(HttpStatusCode.OK, serviceWorkerResponse.StatusCode);
                Assert.Equal("no-cache", serviceWorkerResponse.Headers.CacheControl?.ToString());
            }
            Assert.Equal(HttpStatusCode.Unauthorized, (await firstUser.GetAsync("/api/v1/auth/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await firstUser.GetAsync("/api/v1/profile/")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await firstUser.GetAsync("/api/v1/feed")).StatusCode);
            var registration = await RegisterAsync(firstUser, "ana@example.es", "Jugador", "Ana");
            Assert.Contains(registration.Headers, header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) && string.Join(';', header.Value).Contains("cb-bunol-session", StringComparison.Ordinal));
            Assert.Equal(HttpStatusCode.OK, (await firstUser.GetAsync("/api/v1/auth/me")).StatusCode);
            using (var bodyTokenOnlyForm = new MultipartFormDataContent())
            {
                bodyTokenOnlyForm.Add(new StringContent(await GetCsrfTokenAsync(firstUser)), "__RequestVerificationToken");
                using var bodyTokenOnlyRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/profile/avatar") { Content = bodyTokenOnlyForm };
                using var bodyTokenOnlyResponse = await firstUser.SendAsync(bodyTokenOnlyRequest);
                Assert.Equal(HttpStatusCode.BadRequest, bodyTokenOnlyResponse.StatusCode);
                Assert.Contains("caducado", await bodyTokenOnlyResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
            }
            var profile = await firstUser.GetFromJsonAsync<JsonElement>("/api/v1/profile/");
            Assert.Equal("Ana", profile.GetProperty("firstName").GetString());
            Assert.True(profile.GetProperty("preferredTeamId").ValueKind == JsonValueKind.Null);
            Assert.Empty(profile.GetProperty("availableTeams").EnumerateArray());
            using (var missingProfileCsrf = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile/")
            {
                Content = JsonContent.Create(new { firstName = "Ana", lastName = "Pruebas", email = "ana@example.es", preferredTeamId = (Guid?)null })
            })
                Assert.Equal(HttpStatusCode.BadRequest, (await firstUser.SendAsync(missingProfileCsrf)).StatusCode);
            using (var profileRequest = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile/")
            {
                Content = JsonContent.Create(new { firstName = "Ana María", lastName = "Pruebas", email = "ana.nueva@example.es", preferredTeamId = (Guid?)null })
            })
            {
                profileRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                var response = await firstUser.SendAsync(profileRequest);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var updatedProfile = await response.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal("ana.nueva@example.es", updatedProfile.GetProperty("email").GetString());
                Assert.True(updatedProfile.GetProperty("preferredTeamId").ValueKind == JsonValueKind.Null);
            }
            var refreshedSession = await firstUser.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
            Assert.Equal("ana.nueva@example.es", refreshedSession.GetProperty("email").GetString());
            Assert.Equal("Ana María Pruebas", refreshedSession.GetProperty("displayName").GetString());
            using (var invalidProfileRequest = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile/")
            {
                Content = JsonContent.Create(new { firstName = "Ana", lastName = "Pruebas", email = administratorEmail, preferredTeamId = (Guid?)null })
            })
            {
                invalidProfileRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.BadRequest, (await firstUser.SendAsync(invalidProfileRequest)).StatusCode);
            }
            await using var profileTestConnection = new NpgsqlConnection(schemaConnection.ConnectionString);
            await profileTestConnection.OpenAsync();
            await using (var installProfileUpdateFailure = new NpgsqlCommand("""
                CREATE FUNCTION reject_test_profile_update() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF NEW."Email" = 'ana.fallida@example.es' THEN
                        RAISE EXCEPTION 'Fallo inducido durante la escritura del perfil';
                    END IF;
                    RETURN NEW;
                END;
                $$;
                CREATE TRIGGER reject_test_profile_update BEFORE UPDATE ON "AspNetUsers"
                FOR EACH ROW EXECUTE FUNCTION reject_test_profile_update();
                """, profileTestConnection))
            {
                await installProfileUpdateFailure.ExecuteNonQueryAsync();
            }
            using (var failedProfileUpdate = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile/")
            {
                Content = JsonContent.Create(new { firstName = "Cambio", lastName = "No Persistido", email = "ana.fallida@example.es", preferredTeamId = (Guid?)null })
            })
            {
                failedProfileUpdate.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.InternalServerError, (await firstUser.SendAsync(failedProfileUpdate)).StatusCode);
            }
            await using (var verifyProfileRollback = new NpgsqlCommand("SELECT \"Email\", \"UserName\", \"FirstName\", \"LastName\" FROM \"AspNetUsers\" WHERE \"Email\" = 'ana.nueva@example.es'", profileTestConnection))
            await using (var reader = await verifyProfileRollback.ExecuteReaderAsync())
            {
                Assert.True(await reader.ReadAsync());
                Assert.Equal("ana.nueva@example.es", reader.GetString(0));
                Assert.Equal("ana.nueva@example.es", reader.GetString(1));
                Assert.Equal("Ana María", reader.GetString(2));
                Assert.Equal("Pruebas", reader.GetString(3));
            }
            using (var loginWithUnchangedEmail = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
            {
                Content = JsonContent.Create(new { email = "ana.nueva@example.es", password = "BalonmanoTest1!", rememberMe = false })
            })
            {
                loginWithUnchangedEmail.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(secondUser));
                Assert.Equal(HttpStatusCode.OK, (await secondUser.SendAsync(loginWithUnchangedEmail)).StatusCode);
            }
            using (var loginWithUncommittedEmail = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
            {
                Content = JsonContent.Create(new { email = "ana.fallida@example.es", password = "BalonmanoTest1!", rememberMe = false })
            })
            {
                loginWithUncommittedEmail.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(thirdUser));
                Assert.Equal(HttpStatusCode.Unauthorized, (await thirdUser.SendAsync(loginWithUncommittedEmail)).StatusCode);
            }
            using (var avatarForm = new MultipartFormDataContent())
            {
                using var avatarContent = new ByteArrayContent([0xFF, 0xD8, 0xFF]);
                avatarContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                avatarForm.Add(avatarContent, "avatar", "avatar.jpg");
                using var avatarRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/profile/avatar") { Content = avatarForm };
                avatarRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.OK, (await firstUser.SendAsync(avatarRequest)).StatusCode);
            }
            var profileWithAvatar = await firstUser.GetFromJsonAsync<JsonElement>("/api/v1/profile/");
            var avatarUrl = profileWithAvatar.GetProperty("avatarUrl").GetString();
            Assert.NotNull(avatarUrl);
            var authorAvatarUrl = $"/api/v1/profile/avatar/{profileWithAvatar.GetProperty("id").GetGuid()}";
            using (var anonymousAvatarResponse = await anonymousVisitor.GetAsync(authorAvatarUrl))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousAvatarResponse.StatusCode);
            using (var removedPublicAvatarResponse = await anonymousVisitor.GetAsync($"/api/v1/feed/author-avatar/{profileWithAvatar.GetProperty("id").GetGuid()}"))
                Assert.Equal(HttpStatusCode.NotFound, removedPublicAvatarResponse.StatusCode);
            using (var avatarPostForm = new MultipartFormDataContent { { new StringContent("Publicación con avatar"), "content" } })
            {
                using var avatarPostRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/feed") { Content = avatarPostForm };
                avatarPostRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.Created, (await firstUser.SendAsync(avatarPostRequest)).StatusCode);
            }
            var avatarFeed = await firstUser.GetFromJsonAsync<JsonElement>("/api/v1/feed");
            var avatarPost = Assert.Single(avatarFeed.GetProperty("posts").EnumerateArray(), post => post.GetProperty("content").GetString() == "Publicación con avatar");
            Assert.Equal(authorAvatarUrl, avatarPost.GetProperty("authorAvatarUrl").GetString());
            using (var authenticatedAvatarResponse = await firstUser.GetAsync(authorAvatarUrl))
            {
                var authenticatedAvatarBody = await authenticatedAvatarResponse.Content.ReadAsStringAsync();
                Assert.True(authenticatedAvatarResponse.StatusCode == HttpStatusCode.OK, authenticatedAvatarBody);
            }
            using (var anonymousAvatarAfterPost = await anonymousVisitor.GetAsync(authorAvatarUrl))
                Assert.Equal(HttpStatusCode.Unauthorized, anonymousAvatarAfterPost.StatusCode);
            using (var avatarDelete = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/profile/avatar"))
            {
                avatarDelete.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.NoContent, (await firstUser.SendAsync(avatarDelete)).StatusCode);
            }
            Assert.Equal(HttpStatusCode.NotFound, (await firstUser.GetAsync(authorAvatarUrl)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await firstUser.GetAsync("/api/v1/admin/role-requests/")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await firstUser.GetAsync("/api/v1/admin/teams")).StatusCode);
            using (var missingCsrfRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/feed") { Content = new MultipartFormDataContent() })
                Assert.Equal(HttpStatusCode.BadRequest, (await firstUser.SendAsync(missingCsrfRequest)).StatusCode);
            var csrf = await GetCsrfTokenAsync(firstUser);
            using var form = new MultipartFormDataContent { { new StringContent("Publicación de integración"), "content" } };
            using var image = new ByteArrayContent([0xFF, 0xD8, 0xFF]);
            image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            form.Add(image, "files", "foto.jpg");
            using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/feed") { Content = form };
            createRequest.Headers.Add("X-CSRF-TOKEN", csrf);
            var createResponse = await firstUser.SendAsync(createRequest);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var createdPost = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            var postId = createdPost.GetProperty("id").GetGuid();
            var attachmentId = createdPost.GetProperty("attachments")[0].GetProperty("id").GetGuid();
            Assert.Equal(HttpStatusCode.OK, (await firstUser.GetAsync($"/api/v1/feed/media/{attachmentId}")).StatusCode);

            using var invalidMediaForm = new MultipartFormDataContent { { new StringContent("Firma falsa"), "content" } };
            using var invalidImage = new ByteArrayContent([0, 1, 2, 3]);
            invalidImage.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            invalidMediaForm.Add(invalidImage, "files", "falsa.jpg");
            using var invalidMediaRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/feed") { Content = invalidMediaForm };
            invalidMediaRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
            Assert.Equal(HttpStatusCode.BadRequest, (await firstUser.SendAsync(invalidMediaRequest)).StatusCode);

            using (var oversizedTextForm = new MultipartFormDataContent { { new StringContent(new string('x', 17 * 1024)), "content" } })
            using (var oversizedTextRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/feed") { Content = oversizedTextForm })
            {
                oversizedTextRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.BadRequest, (await firstUser.SendAsync(oversizedTextRequest)).StatusCode);
            }
            using (var unknownLengthContent = CreateUnknownLengthFormContent("content", "Publicación sin longitud declarada"))
            using (var unknownLengthRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/feed") { Content = unknownLengthContent })
            {
                Assert.Null(unknownLengthRequest.Content.Headers.ContentLength);
                unknownLengthRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.Created, (await firstUser.SendAsync(unknownLengthRequest)).StatusCode);
            }

            using (var largeVideoForm = new MultipartFormDataContent { { new StringContent("Vídeo de 40 MiB"), "content" } })
            {
                var videoBytes = new byte[40 * 1024 * 1024];
                videoBytes[4] = 0x66; videoBytes[5] = 0x74; videoBytes[6] = 0x79; videoBytes[7] = 0x70;
                using var largeVideo = new ByteArrayContent(videoBytes);
                largeVideo.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
                largeVideoForm.Add(largeVideo, "files", "sesion-40mib.mp4");
                using var largeVideoRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/feed") { Content = largeVideoForm };
                largeVideoRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                using var largeVideoResponse = await firstUser.SendAsync(largeVideoRequest, HttpCompletionOption.ResponseHeadersRead);
                var largeVideoResponseBody = await largeVideoResponse.Content.ReadAsStringAsync();
                Assert.True(largeVideoResponse.StatusCode == HttpStatusCode.Created, largeVideoResponseBody);
            }

            await RegisterAsync(secondUser, "socia@example.es", "CuerpoTecnico");
            await LoginAsync(administrator, administratorEmail, administratorPassword);
            var bootstrapAdministrator = await administrator.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
            var requests = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/role-requests/");
            Assert.NotNull(requests);
            var roleRequest = Assert.Single(requests!, request => request.GetProperty("email").GetString() == "socia@example.es");
            var requestId = roleRequest.GetProperty("id").GetGuid();
            using var approveRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/role-requests/{requestId}/approve") { Content = JsonContent.Create(new { }) };
            approveRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
            Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(approveRequest)).StatusCode);
            var pendingRequests = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/role-requests/");
            Assert.NotNull(pendingRequests);
            var playerRequest = Assert.Single(pendingRequests!, request => request.GetProperty("email").GetString() == "ana.nueva@example.es");
            var manualPlayerRequestId = playerRequest.GetProperty("id").GetGuid();
            using (var manuallyAssignRequestedRole = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{playerRequest.GetProperty("userId").GetGuid()}/role")
            {
                Content = JsonContent.Create(new { role = "Jugador" })
            })
            {
                manuallyAssignRequestedRole.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(manuallyAssignRequestedRole)).StatusCode);
            }
            var requestsAfterManualRequestedRole = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/role-requests/?pendingOnly=true");
            Assert.NotNull(requestsAfterManualRequestedRole);
            Assert.DoesNotContain(requestsAfterManualRequestedRole!, item => item.GetProperty("id").GetGuid() == manualPlayerRequestId);
            using (var repeatedManualRequestApproval = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/role-requests/{manualPlayerRequestId}/approve") { Content = JsonContent.Create(new { }) })
            {
                repeatedManualRequestApproval.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.Conflict, (await administrator.SendAsync(repeatedManualRequestApproval)).StatusCode);
            }

            using (var secondPendingPlayerRegistration = await RegisterAsync(thirdUser, "ana.segunda@example.es", "Jugador", "Ana Segunda")) { }
            var requestsAfterSecondRegistration = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/role-requests/?pendingOnly=true");
            Assert.NotNull(requestsAfterSecondRegistration);
            var otherPendingPlayerRequest = Assert.Single(requestsAfterSecondRegistration!, item => item.GetProperty("email").GetString() == "ana.segunda@example.es");
            using (var manuallyAssignDifferentRole = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{otherPendingPlayerRequest.GetProperty("userId").GetGuid()}/role")
            {
                Content = JsonContent.Create(new { role = "Administrador" })
            })
            {
                manuallyAssignDifferentRole.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(manuallyAssignDifferentRole)).StatusCode);
            }
            var requestsAfterManualDifferentRole = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/role-requests/?pendingOnly=true");
            Assert.NotNull(requestsAfterManualDifferentRole);
            Assert.DoesNotContain(requestsAfterManualDifferentRole!, item => item.GetProperty("id").GetGuid() == otherPendingPlayerRequest.GetProperty("id").GetGuid());
            var resolvedRequests = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/role-requests/");
            Assert.NotNull(resolvedRequests);
            var manuallyResolvedRequest = Assert.Single(resolvedRequests!, item => item.GetProperty("id").GetGuid() == manualPlayerRequestId);
            Assert.Equal("Approved", manuallyResolvedRequest.GetProperty("status").GetString());
            var manuallyDeclinedRequest = Assert.Single(resolvedRequests!, item => item.GetProperty("id").GetGuid() == otherPendingPlayerRequest.GetProperty("id").GetGuid());
            Assert.Equal("Rejected", manuallyDeclinedRequest.GetProperty("status").GetString());
            var manuallyChangedDifferentRoleUser = await thirdUser.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
            Assert.Contains("Administrador", manuallyChangedDifferentRoleUser.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
            var manuallyResolvedDifferentRole = Assert.Single(resolvedRequests!, item => item.GetProperty("id").GetGuid() == otherPendingPlayerRequest.GetProperty("id").GetGuid());
            Assert.Equal("Rejected", manuallyResolvedDifferentRole.GetProperty("status").GetString());
            using (var repeatedDifferentRequestApproval = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/role-requests/{otherPendingPlayerRequest.GetProperty("id").GetGuid()}/approve") { Content = JsonContent.Create(new { }) })
            {
                repeatedDifferentRequestApproval.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.Conflict, (await administrator.SendAsync(repeatedDifferentRequestApproval)).StatusCode);
            }

            using (var approvePlayer = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/role-requests/{manualPlayerRequestId}/approve") { Content = JsonContent.Create(new { }) })
            {
                approvePlayer.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.Conflict, (await administrator.SendAsync(approvePlayer)).StatusCode);
            }

            using (var concurrentDecisionRegistration = await RegisterAsync(fourthUser, "solicitud.concurrente@example.es", "Jugador", "Solicitud Concurrente")) { }
            var requestsBeforeConcurrentDecision = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/role-requests/?pendingOnly=true");
            Assert.NotNull(requestsBeforeConcurrentDecision);
            var concurrentDecisionRequest = Assert.Single(requestsBeforeConcurrentDecision!, item => item.GetProperty("email").GetString() == "solicitud.concurrente@example.es");
            var concurrentDecisionRequestId = concurrentDecisionRequest.GetProperty("id").GetGuid();
            await LoginAsync(secondAdministrator, "ana.segunda@example.es", "BalonmanoTest1!");
            var secondAdministratorProfile = await secondAdministrator.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
            var secondAdministratorId = secondAdministratorProfile.GetProperty("id").GetGuid();
            Assert.Contains("Administrador", secondAdministratorProfile.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
            using var decisionBarrier = new Barrier(2);
            var concurrentDecisions = await Task.WhenAll(
                ResolveRoleRequestConcurrentlyAsync(administrator, concurrentDecisionRequestId, "approve", decisionBarrier),
                ResolveRoleRequestConcurrentlyAsync(secondAdministrator, concurrentDecisionRequestId, "reject", decisionBarrier));
            Assert.Single(concurrentDecisions, status => status == HttpStatusCode.NoContent);
            Assert.Single(concurrentDecisions, status => status == HttpStatusCode.Conflict);
            var requestAfterConcurrentDecision = await administrator.GetFromJsonAsync<JsonElement[]>($"/api/v1/admin/role-requests/?pendingOnly=false");
            Assert.NotNull(requestAfterConcurrentDecision);
            var resolvedConcurrentRequest = Assert.Single(requestAfterConcurrentDecision!, item => item.GetProperty("id").GetGuid() == concurrentDecisionRequestId);
            var concurrentResolutionStatus = resolvedConcurrentRequest.GetProperty("status").GetString();
            Assert.Contains(concurrentResolutionStatus, new[] { "Approved", "Rejected" });
            var concurrentDecisionUser = await fourthUser.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
            var concurrentDecisionRoles = concurrentDecisionUser.GetProperty("roles").EnumerateArray().Select(role => role.GetString()).ToArray();
            if (concurrentResolutionStatus == "Approved")
            {
                Assert.Contains("Jugador", concurrentDecisionRoles);
                Assert.DoesNotContain("SocioAficion", concurrentDecisionRoles);
            }
            else
            {
                Assert.Contains("SocioAficion", concurrentDecisionRoles);
                Assert.DoesNotContain("Jugador", concurrentDecisionRoles);
            }
            await using (var auditConnection = new NpgsqlConnection(schemaConnection.ConnectionString))
            {
                await auditConnection.OpenAsync();
                await using var countResolutionAudits = new NpgsqlCommand("SELECT COUNT(*) FROM administration_audit_entries WHERE \"Details\" LIKE @requestId AND \"Action\" IN ('RoleRequestApproved', 'RoleRequestRejected')", auditConnection);
                countResolutionAudits.Parameters.AddWithValue("requestId", $"{concurrentDecisionRequestId}%");
                Assert.Equal(1L, (long)(await countResolutionAudits.ExecuteScalarAsync())!);
            }

            var approvedUser = await secondUser.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
            Assert.Contains("CuerpoTecnico", approvedUser.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
            Assert.DoesNotContain("SocioAficion", approvedUser.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
            var searchablePlayers = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/users?query=socia%40example.es&playersOnly=true");
            Assert.NotNull(searchablePlayers);
            Assert.Contains(searchablePlayers!, item => item.GetProperty("id").GetGuid() == approvedUser.GetProperty("id").GetGuid());
            var searchableAdministrators = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/users?query=admin-&playersOnly=true");
            Assert.NotNull(searchableAdministrators);
            Assert.Empty(searchableAdministrators!);

            using var createTeam = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/teams")
            {
                Content = JsonContent.Create(new { name = "Infantil Mixto", category = "Infantil" })
            };
            createTeam.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
            Assert.Equal(HttpStatusCode.Created, (await administrator.SendAsync(createTeam)).StatusCode);
            using var createSeniorTeam = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/teams")
            {
                Content = JsonContent.Create(new { name = "Senior A", category = "Senior" })
            };
            createSeniorTeam.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
            Assert.Equal(HttpStatusCode.Created, (await administrator.SendAsync(createSeniorTeam)).StatusCode);
            var adminTeams = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/teams");
            Assert.NotNull(adminTeams);
            var teamId = Assert.Single(adminTeams!, team => team.GetProperty("name").GetString() == "Infantil Mixto").GetProperty("id").GetGuid();
            var seniorTeamId = Assert.Single(adminTeams!, team => team.GetProperty("name").GetString() == "Senior A").GetProperty("id").GetGuid();
            var seasonToday = DateOnly.FromDateTime(DateTime.UtcNow);
            var seasonStartYear = seasonToday.Month >= 7 ? seasonToday.Year : seasonToday.Year - 1;
            var planEffectiveFrom = new DateOnly(seasonStartYear, 7, 1);
            using (var setWeeklyPlan = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/teams/{teamId}/training-plan")
            {
                Content = JsonContent.Create(new
                {
                    effectiveFrom = planEffectiveFrom,
                    slots = new[]
                    {
                        new { dayOfWeek = (int)planEffectiveFrom.DayOfWeek, startsAt = "20:00:00", endsAt = "21:30:00", venue = "" },
                    },
                })
            })
            {
                setWeeklyPlan.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(setWeeklyPlan)).StatusCode);
            }
            var calendarMonth = planEffectiveFrom;
            using var calendarResponse = await administrator.GetAsync($"/api/v1/teams/{teamId}/calendar?year={calendarMonth.Year}&month={calendarMonth.Month}");
            var calendarBody = await calendarResponse.Content.ReadAsStringAsync();
            Assert.True(calendarResponse.StatusCode == HttpStatusCode.OK, calendarBody);
            using var calendarDocument = JsonDocument.Parse(calendarBody);
            var calendar = calendarDocument.RootElement;
            Assert.Equal(planEffectiveFrom.ToString("yyyy-MM-dd"), calendar.GetProperty("seasonStart").GetString());
            Assert.Contains(calendar.GetProperty("items").EnumerateArray(), item => item.GetProperty("source").GetString() == "Planificacion");
            var profileAfterTeamsExist = await firstUser.GetFromJsonAsync<JsonElement>("/api/v1/profile/");
            Assert.True(profileAfterTeamsExist.GetProperty("preferredTeamId").ValueKind == JsonValueKind.Null);
            Assert.Equal("Senior", Assert.Single(adminTeams!, item => item.GetProperty("name").GetString() == "Senior A").GetProperty("category").GetString());
            using (var updatePlayerPreference = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile/")
            {
                Content = JsonContent.Create(new { firstName = "Ana", lastName = "Pruebas", email = "ana.nueva@example.es", preferredTeamId = seniorTeamId })
            })
            {
                updatePlayerPreference.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.OK, (await firstUser.SendAsync(updatePlayerPreference)).StatusCode);
            }
            var implicitCalendar = await administrator.GetFromJsonAsync<JsonElement>($"/api/v1/teams/{teamId}/calendar");
            var clubToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ResolveTestClubTimeZone()).DateTime);
            Assert.Equal($"{clubToday:yyyy-MM}-01", implicitCalendar.GetProperty("month").GetString());
            var calendarSeasonStartYear = clubToday.Month >= 7 ? clubToday.Year : clubToday.Year - 1;
            var octoberYear = calendarSeasonStartYear;
            var explicitCalendar = await administrator.GetFromJsonAsync<JsonElement>($"/api/v1/teams/{teamId}/calendar?year={octoberYear}&month=10");
            Assert.Equal($"{octoberYear}-10-01", explicitCalendar.GetProperty("month").GetString());
            using (var deactivatePreferredTeam = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/teams/{seniorTeamId}/active?isActive=false"))
            {
                deactivatePreferredTeam.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(deactivatePreferredTeam)).StatusCode);
            }
            using (var preserveInactivePreference = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile/")
            {
                Content = JsonContent.Create(new { firstName = "Ana", lastName = "Pruebas", email = "ana.nueva@example.es", preferredTeamId = seniorTeamId })
            })
            {
                preserveInactivePreference.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.OK, (await firstUser.SendAsync(preserveInactivePreference)).StatusCode);
            }
            using (var deactivateOtherTeam = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/teams/{teamId}/active?isActive=false"))
            {
                deactivateOtherTeam.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(deactivateOtherTeam)).StatusCode);
            }
            using (var rejectOtherInactivePreference = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile/")
            {
                Content = JsonContent.Create(new { firstName = "Ana", lastName = "Pruebas", email = "ana.nueva@example.es", preferredTeamId = teamId })
            })
            {
                rejectOtherInactivePreference.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.BadRequest, (await firstUser.SendAsync(rejectOtherInactivePreference)).StatusCode);
            }
            using (var reactivatePreferredTeam = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/teams/{seniorTeamId}/active?isActive=true"))
            {
                reactivatePreferredTeam.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(reactivatePreferredTeam)).StatusCode);
            }
            using (var reactivateOtherTeam = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/teams/{teamId}/active?isActive=true"))
            {
                reactivateOtherTeam.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(reactivateOtherTeam)).StatusCode);
            }
            using (var registerWithSeniorPreference = await RegisterAsync(thirdUser, "nuevo@example.es", "Socio", "Nuevo"))
            {
                var newlyRegisteredProfile = await thirdUser.GetFromJsonAsync<JsonElement>("/api/v1/profile/");
                Assert.Equal(seniorTeamId, newlyRegisteredProfile.GetProperty("preferredTeamId").GetGuid());
            }
            var registeredPlayer = await firstUser.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
            using (var assignPlayerAdmin = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/teams/{teamId}/players/{registeredPlayer.GetProperty("id").GetGuid()}")
            {
                Content = JsonContent.Create(new { userId = registeredPlayer.GetProperty("id").GetGuid(), jerseyNumber = 7, position = "Extremo Derecho" })
            })
            {
                assignPlayerAdmin.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(assignPlayerAdmin)).StatusCode);
            }
            using var adminRosterResponse = await administrator.GetAsync($"/api/v1/admin/teams/{teamId}/players");
            Assert.True(adminRosterResponse.IsSuccessStatusCode, await adminRosterResponse.Content.ReadAsStringAsync());
            var adminRoster = await adminRosterResponse.Content.ReadFromJsonAsync<JsonElement[]>();
            Assert.NotNull(adminRoster);
            Assert.Equal(7, Assert.Single(adminRoster!).GetProperty("jerseyNumber").GetInt32());
            using var assignTeam = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{approvedUser.GetProperty("id").GetGuid()}/teams")
            {
                Content = JsonContent.Create(new { teamIds = new[] { teamId } })
            };
            assignTeam.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
            Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(assignTeam)).StatusCode);
            using (var removeAllCoachTeams = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{approvedUser.GetProperty("id").GetGuid()}/teams")
            {
                Content = JsonContent.Create(new { teamIds = Array.Empty<Guid>() })
            })
            {
                removeAllCoachTeams.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                var response = await administrator.SendAsync(removeAllCoachTeams);
                Assert.True(response.StatusCode == HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
            }
            var coachWithoutTeams = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/users?query=socia%40example.es&coachesOnly=true");
            Assert.NotNull(coachWithoutTeams);
            Assert.Empty(Assert.Single(coachWithoutTeams!).GetProperty("teams").EnumerateArray());
            await using (var auditConnection = new NpgsqlConnection(schemaConnection.ConnectionString))
            {
                await auditConnection.OpenAsync();
                await using var getUnassignmentAudit = new NpgsqlCommand("SELECT \"Details\" FROM administration_audit_entries WHERE \"TargetUserId\" = @userId AND \"Action\" = 'TeamsUnassigned' ORDER BY \"CreatedAt\" DESC LIMIT 1", auditConnection);
                getUnassignmentAudit.Parameters.AddWithValue("userId", approvedUser.GetProperty("id").GetGuid());
                var auditDetails = (string?)await getUnassignmentAudit.ExecuteScalarAsync();
                Assert.Contains(teamId.ToString(), auditDetails);
            }
            using (var restoreCoachTeam = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{approvedUser.GetProperty("id").GetGuid()}/teams")
            {
                Content = JsonContent.Create(new { teamIds = new[] { teamId } })
            })
            {
                restoreCoachTeam.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(restoreCoachTeam)).StatusCode);
            }
            var staffPlayerCandidates = await secondUser.GetFromJsonAsync<JsonElement[]>($"/api/v1/teams/{teamId}/players/search?query=socia%40example.es");
            Assert.NotNull(staffPlayerCandidates);
            Assert.Contains(staffPlayerCandidates!, item => item.GetProperty("userId").GetGuid() == approvedUser.GetProperty("id").GetGuid());
            var administratorPlayerCandidates = await secondUser.GetFromJsonAsync<JsonElement[]>($"/api/v1/teams/{teamId}/players/search?query=admin-");
            Assert.NotNull(administratorPlayerCandidates);
            Assert.Empty(administratorPlayerCandidates!);
            using (var addCoachAsPlayerByStaff = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/teams/{teamId}/players")
            {
                Content = JsonContent.Create(new { userId = approvedUser.GetProperty("id").GetGuid(), jerseyNumber = 23, position = "Central" })
            })
            {
                addCoachAsPlayerByStaff.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(secondUser));
                Assert.Equal(HttpStatusCode.NoContent, (await secondUser.SendAsync(addCoachAsPlayerByStaff)).StatusCode);
            }
            var registeredPlayerId = registeredPlayer.GetProperty("id").GetGuid();
            var coachId = approvedUser.GetProperty("id").GetGuid();
            var matchSeedNow = DateTimeOffset.UtcNow;
            var historicalMatches = Enumerable.Range(0, 21)
                .Select(index => new TeamMatch(teamId, coachId, $"Histórico {index + 1}", matchSeedNow.AddDays(-30 + index), "El Planell", $"Jornada {index + 1}"))
                .ToArray();
            var upcomingMatch = new TeamMatch(teamId, coachId, "Próximo con convocatoria", matchSeedNow.AddDays(2), "El Planell", "Próxima jornada");
            var secondUpcomingMatch = new TeamMatch(teamId, coachId, "Segundo próximo", matchSeedNow.AddDays(3), "El Planell", "Siguiente jornada");
            using (var seedMatchScope = factory.Services.CreateScope())
            {
                var matchDb = seedMatchScope.ServiceProvider.GetRequiredService<ClubDbContext>();
                matchDb.TeamMatches.AddRange(historicalMatches);
                matchDb.TeamMatches.AddRange(upcomingMatch, secondUpcomingMatch);
                matchDb.TeamCallups.Add(new TeamCallup(upcomingMatch.Id, registeredPlayerId, coachId));
                await matchDb.SaveChangesAsync();
            }
            using var upcomingTeamSpaceResponse = await secondUser.GetAsync($"/api/v1/teams/{teamId}/space");
            var upcomingTeamSpaceBody = await upcomingTeamSpaceResponse.Content.ReadAsStringAsync();
            Assert.True(upcomingTeamSpaceResponse.StatusCode == HttpStatusCode.OK, upcomingTeamSpaceBody);
            using var upcomingTeamSpaceDocument = JsonDocument.Parse(upcomingTeamSpaceBody);
            var upcomingTeamSpace = upcomingTeamSpaceDocument.RootElement;
            var upcomingMatches = upcomingTeamSpace.GetProperty("matches").EnumerateArray().ToArray();
            Assert.Equal(2, upcomingMatches.Length);
            Assert.Equal("Próximo con convocatoria", upcomingMatches[0].GetProperty("opponent").GetString());
            var upcomingCallup = Assert.Single(upcomingMatches[0].GetProperty("callups").EnumerateArray());
            Assert.Equal(registeredPlayerId, upcomingCallup.GetProperty("playerId").GetGuid());
            Assert.Equal("Pendiente", upcomingCallup.GetProperty("attendance").GetString());
            Assert.DoesNotContain(upcomingMatches, match => match.GetProperty("opponent").GetString()?.StartsWith("Histórico", StringComparison.Ordinal) == true);

            var historicalDate = DateOnly.FromDateTime(historicalMatches[0].StartsAt.UtcDateTime);
            using (var historicalCalendarResponse = await secondUser.GetAsync($"/api/v1/teams/{teamId}/calendar?year={historicalDate.Year}&month={historicalDate.Month}"))
            {
                var historicalCalendarBody = await historicalCalendarResponse.Content.ReadAsStringAsync();
                Assert.True(historicalCalendarResponse.StatusCode == HttpStatusCode.OK, historicalCalendarBody);
                using var historicalCalendarDocument = JsonDocument.Parse(historicalCalendarBody);
                Assert.Contains(historicalCalendarDocument.RootElement.GetProperty("items").EnumerateArray(),
                    item => item.GetProperty("source").GetString() == "Partido" && item.GetProperty("title").GetString() == "Histórico 1");
            }
            var staffAndPlayerSpace = await secondUser.GetFromJsonAsync<JsonElement>($"/api/v1/teams/{teamId}/space");
            Assert.True(staffAndPlayerSpace.GetProperty("canManage").GetBoolean());
            Assert.Contains(staffAndPlayerSpace.GetProperty("players").EnumerateArray(), item => item.GetProperty("userId").GetGuid() == approvedUser.GetProperty("id").GetGuid());
            using (var assignCoachAsPlayer = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/teams/{seniorTeamId}/players/{approvedUser.GetProperty("id").GetGuid()}")
            {
                Content = JsonContent.Create(new { userId = approvedUser.GetProperty("id").GetGuid(), jerseyNumber = 15, position = "Extremo Derecho" })
            })
            {
                assignCoachAsPlayer.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(assignCoachAsPlayer)).StatusCode);
            }
            var coachWithRosterMembership = await secondUser.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
            Assert.Contains("CuerpoTecnico", coachWithRosterMembership.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
            var coachTeams = await secondUser.GetFromJsonAsync<JsonElement[]>("/api/v1/my-teams");
            Assert.NotNull(coachTeams);
            Assert.Contains(coachTeams!, item => item.GetProperty("id").GetGuid() == teamId);
            Assert.Contains(coachTeams!, item => item.GetProperty("id").GetGuid() == seniorTeamId);
            var coachPlayerSpace = await secondUser.GetFromJsonAsync<JsonElement>($"/api/v1/teams/{seniorTeamId}/space");
            Assert.False(coachPlayerSpace.GetProperty("canManage").GetBoolean());
            Assert.Contains(coachPlayerSpace.GetProperty("players").EnumerateArray(), item => item.GetProperty("userId").GetGuid() == approvedUser.GetProperty("id").GetGuid());
            using (var assignAdministratorTeam = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{bootstrapAdministrator.GetProperty("id").GetGuid()}/teams")
            {
                Content = JsonContent.Create(new { teamIds = new[] { teamId } })
            })
            {
                assignAdministratorTeam.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.BadRequest, (await administrator.SendAsync(assignAdministratorTeam)).StatusCode);
            }
            var myTeams = await secondUser.GetFromJsonAsync<JsonElement[]>("/api/v1/my-teams");
            Assert.NotNull(myTeams);
            Assert.Contains(myTeams!, item => item.GetProperty("id").GetGuid() == teamId);
            var teamSpace = await secondUser.GetFromJsonAsync<JsonElement>($"/api/v1/teams/{teamId}/space");
            Assert.Equal("Infantil Mixto", teamSpace.GetProperty("team").GetProperty("name").GetString());
            Assert.True(teamSpace.GetProperty("canManage").GetBoolean());
            var playerTeamSpace = await firstUser.GetFromJsonAsync<JsonElement>($"/api/v1/teams/{teamId}/space");
            Assert.False(playerTeamSpace.GetProperty("canManage").GetBoolean());
            using (var staffBoardPost = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/teams/{teamId}/board")
            {
                Content = new MultipartFormDataContent { { new StringContent("Indicaciones del entrenamiento"), "content" } }
            })
            {
                staffBoardPost.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(secondUser));
                Assert.Equal(HttpStatusCode.NoContent, (await secondUser.SendAsync(staffBoardPost)).StatusCode);
            }
            var imageAttachmentId = Guid.NewGuid();
            var videoAttachmentId = Guid.NewGuid();
            using (var mediaBoardForm = new MultipartFormDataContent { { new StringContent("Vídeo de la sesión"), "content" } })
            {
                using var imageFile = new ByteArrayContent([0xFF, 0xD8, 0xFF]);
                imageFile.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                mediaBoardForm.Add(imageFile, "files", "tactica.jpg");
                using var videoFile = new ByteArrayContent([0, 0, 0, 24, 0x66, 0x74, 0x79, 0x70]);
                videoFile.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
                mediaBoardForm.Add(videoFile, "files", "entrenamiento.mp4");
                using var mediaBoardRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/teams/{teamId}/board") { Content = mediaBoardForm };
                mediaBoardRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(secondUser));
                Assert.Equal(HttpStatusCode.NoContent, (await secondUser.SendAsync(mediaBoardRequest)).StatusCode);
            }
            var mediaTeamSpace = await secondUser.GetFromJsonAsync<JsonElement>($"/api/v1/teams/{teamId}/space");
            var mediaPost = Assert.Single(mediaTeamSpace.GetProperty("boardPosts").EnumerateArray(), post => post.GetProperty("content").GetString() == "Vídeo de la sesión");
            var mediaAttachments = mediaPost.GetProperty("attachments").EnumerateArray().ToArray();
            imageAttachmentId = mediaAttachments.Single(file => file.GetProperty("contentType").GetString() == "image/jpeg").GetProperty("id").GetGuid();
            videoAttachmentId = mediaAttachments.Single(file => file.GetProperty("contentType").GetString() == "video/mp4").GetProperty("id").GetGuid();
            using (var imageResponse = await secondUser.GetAsync($"/api/v1/teams/{teamId}/board-files/{imageAttachmentId}"))
            {
                Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
                Assert.Equal("image/jpeg", imageResponse.Content.Headers.ContentType?.MediaType);
                Assert.Null(imageResponse.Content.Headers.ContentDisposition?.FileName);
            }
            using (var videoResponse = await secondUser.GetAsync($"/api/v1/teams/{teamId}/board-files/{videoAttachmentId}"))
            {
                Assert.Equal(HttpStatusCode.OK, videoResponse.StatusCode);
                Assert.Equal("video/mp4", videoResponse.Content.Headers.ContentType?.MediaType);
                Assert.Null(videoResponse.Content.Headers.ContentDisposition?.FileName);
            }
            using (var rangedVideoRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/teams/{teamId}/board-files/{videoAttachmentId}"))
            {
                rangedVideoRequest.Headers.Range = new RangeHeaderValue(0, 2);
                using var rangedVideoResponse = await secondUser.SendAsync(rangedVideoRequest, HttpCompletionOption.ResponseHeadersRead);
                Assert.Equal(HttpStatusCode.PartialContent, rangedVideoResponse.StatusCode);
            }
            var teamSpaceWithBoard = await secondUser.GetFromJsonAsync<JsonElement>($"/api/v1/teams/{teamId}/space");
            Assert.Contains(teamSpaceWithBoard.GetProperty("boardPosts").EnumerateArray(), post => post.GetProperty("content").GetString() == "Indicaciones del entrenamiento");
            using (var privateImageResponse = await thirdUser.GetAsync($"/api/v1/teams/{teamId}/board-files/{imageAttachmentId}"))
                Assert.Equal(HttpStatusCode.NotFound, privateImageResponse.StatusCode);
            using (var addPlayerByStaff = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/teams/{teamId}/players")
            {
                Content = JsonContent.Create(new { userId = registration.Content is null ? Guid.Empty : JsonDocument.Parse(await registration.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid(), jerseyNumber = 22, position = "Portero" })
            })
            {
                addPlayerByStaff.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(secondUser));
                Assert.Equal(HttpStatusCode.NoContent, (await secondUser.SendAsync(addPlayerByStaff)).StatusCode);
            }
            var search = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/users?query=socia@example.es");
            Assert.NotNull(search);
            var listedCoach = Assert.Single(search!);
            Assert.Contains("CuerpoTecnico", listedCoach.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
            Assert.Single(listedCoach.GetProperty("teams").EnumerateArray());

            var coachTeamIdsBeforeRoleChange = listedCoach.GetProperty("teams").EnumerateArray().Select(team => team.GetProperty("id").GetGuid()).ToArray();
            using (var removeCoachRole = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{approvedUser.GetProperty("id").GetGuid()}/role")
            {
                Content = JsonContent.Create(new { role = "Jugador" })
            })
            {
                removeCoachRole.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(removeCoachRole)).StatusCode);
            }
            var myTeamsAfterCoachRoleRemoval = await secondUser.GetFromJsonAsync<JsonElement[]>("/api/v1/my-teams");
            Assert.NotNull(myTeamsAfterCoachRoleRemoval);
            Assert.All(myTeamsAfterCoachRoleRemoval!, item => Assert.Equal("Jugador", item.GetProperty("access").GetString()));
            var teamsAfterCoachRoleRemoval = await administrator.GetFromJsonAsync<JsonElement[]>($"/api/v1/admin/users?query=socia%40example.es");
            Assert.NotNull(teamsAfterCoachRoleRemoval);
            var playerRoleAdminEntry = Assert.Single(teamsAfterCoachRoleRemoval!);
            Assert.Empty(playerRoleAdminEntry.GetProperty("teams").EnumerateArray());
            using (var restoreCoachRole = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{approvedUser.GetProperty("id").GetGuid()}/role")
            {
                Content = JsonContent.Create(new { role = "CuerpoTecnico" })
            })
            {
                restoreCoachRole.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(restoreCoachRole)).StatusCode);
            }
            var myTeamsAfterCoachRoleRestore = await secondUser.GetFromJsonAsync<JsonElement[]>("/api/v1/my-teams");
            Assert.NotNull(myTeamsAfterCoachRoleRestore);
            Assert.Contains(myTeamsAfterCoachRoleRestore!, item => item.GetProperty("id").GetGuid() == teamId && item.GetProperty("access").GetString() == "CuerpoTecnico");
            var restoredCoachEntries = await administrator.GetFromJsonAsync<JsonElement[]>("/api/v1/admin/users?query=socia%40example.es");
            Assert.NotNull(restoredCoachEntries);
            var restoredCoachAdminEntry = Assert.Single(restoredCoachEntries!);
            Assert.Equal(coachTeamIdsBeforeRoleChange.Order(), restoredCoachAdminEntry.GetProperty("teams").EnumerateArray().Select(item => item.GetProperty("id").GetGuid()).Order());

            using var officialForm = new MultipartFormDataContent { { new StringContent("Comunicado de integración"), "content" } };
            using var officialPost = new HttpRequestMessage(HttpMethod.Post, "/api/v1/feed") { Content = officialForm };
            officialPost.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(secondUser));
            Assert.Equal(HttpStatusCode.Created, (await secondUser.SendAsync(officialPost)).StatusCode);
            var announcements = await firstUser.GetFromJsonAsync<JsonElement>("/api/v1/feed/announcements");
            Assert.Contains(announcements.GetProperty("posts").EnumerateArray(), post => post.GetProperty("content").GetString() == "Comunicado de integración");
            Assert.DoesNotContain(announcements.GetProperty("posts").EnumerateArray(), post => post.GetProperty("content").GetString() == "Publicación de integración");

            using var changeRole = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{approvedUser.GetProperty("id").GetGuid()}/role")
            {
                Content = JsonContent.Create(new { role = "Jugador" })
            };
            changeRole.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
            Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(changeRole)).StatusCode);
            var changedRoles = await secondUser.GetFromJsonAsync<JsonElement>("/api/v1/auth/me");
            Assert.Contains("Jugador", changedRoles.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));
            Assert.DoesNotContain("CuerpoTecnico", changedRoles.GetProperty("roles").EnumerateArray().Select(role => role.GetString()));

            var secondCsrf = await GetCsrfTokenAsync(secondUser);
            using var foreignDelete = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/feed/{postId}");
            foreignDelete.Headers.Add("X-CSRF-TOKEN", secondCsrf);
            Assert.Equal(HttpStatusCode.NotFound, (await secondUser.SendAsync(foreignDelete)).StatusCode);

            var ownerCsrf = await GetCsrfTokenAsync(firstUser);
            using var ownerDelete = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/feed/{postId}");
            ownerDelete.Headers.Add("X-CSRF-TOKEN", ownerCsrf);
            Assert.Equal(HttpStatusCode.NoContent, (await firstUser.SendAsync(ownerDelete)).StatusCode);
            using var repeatedDelete = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/feed/{postId}");
            repeatedDelete.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
            Assert.Equal(HttpStatusCode.NotFound, (await firstUser.SendAsync(repeatedDelete)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await firstUser.GetAsync($"/api/v1/feed/media/{attachmentId}")).StatusCode);

            var feed = await firstUser.GetFromJsonAsync<JsonElement>("/api/v1/feed");
            Assert.DoesNotContain(feed.GetProperty("posts").EnumerateArray(), post => post.GetProperty("id").GetGuid() == postId);

            var adminOneId = bootstrapAdministrator.GetProperty("id").GetGuid();
            var adminTwoId = secondAdministratorId;

            using var roleChangeBarrier = new Barrier(2);
            var concurrentRoleChanges = await Task.WhenAll(
                RemoveOtherAdministratorAsync(administrator, adminTwoId, roleChangeBarrier),
                RemoveOtherAdministratorAsync(secondAdministrator, adminOneId, roleChangeBarrier));
            Assert.Single(concurrentRoleChanges, status => status == HttpStatusCode.NoContent);
            Assert.Single(concurrentRoleChanges, status => status is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden);
            var remainingAdministratorCount = await GetActiveAdministratorCountAsync(schemaConnection.ConnectionString);
            Assert.Equal(1, remainingAdministratorCount);

            using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout") { Content = JsonContent.Create(new { }) };
            logout.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
            Assert.Equal(HttpStatusCode.NoContent, (await firstUser.SendAsync(logout)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await firstUser.GetAsync("/api/v1/auth/me")).StatusCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__ClubDatabase", previousConnection);
            Environment.SetEnvironmentVariable("PostMedia__RootPath", previousMediaRoot);
            Environment.SetEnvironmentVariable("ProfileAvatar__RootPath", previousAvatarRoot);
            Environment.SetEnvironmentVariable("Frontend__WebRoot", previousFrontendRoot);
            Environment.SetEnvironmentVariable("BootstrapAdmin__Email", previousAdminEmail);
            Environment.SetEnvironmentVariable("BootstrapAdmin__Password", previousAdminPassword);
            if (Directory.Exists(mediaRoot)) Directory.Delete(mediaRoot, recursive: true);
            if (Directory.Exists(avatarRoot)) Directory.Delete(avatarRoot, recursive: true);
            if (Directory.Exists(frontendRoot)) Directory.Delete(frontendRoot, recursive: true);
            await using var connection = new NpgsqlConnection(masterConnection.ConnectionString);
            await connection.OpenAsync();
            await using var dropSchema = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE", connection);
            await dropSchema.ExecuteNonQueryAsync();
        }
    }

    private static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string accountType = "Socio", string firstName = "Socia")
    {
        var csrf = await GetCsrfTokenAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/register")
        {
            Content = JsonContent.Create(new
            {
                firstName,
                lastName = "Pruebas",
                email,
                password = "BalonmanoTest1!",
                accountType
            })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return response;
    }

    private static HttpContent CreateUnknownLengthFormContent(string fieldName, string value)
    {
        var content = new StreamContent(new ChunkedRequestStream(System.Text.Encoding.UTF8.GetBytes(value)));
        content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        var multipart = new MultipartFormDataContent($"boundary-{Guid.NewGuid():N}");
        multipart.Add(content, fieldName);
        return multipart;
    }

    private static TimeZoneInfo ResolveTestClubTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time"); }
    }

    private sealed class ChunkedRequestStream(byte[] content) : Stream
    {
        private int position;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            var available = Math.Min(count, content.Length - position);
            Array.Copy(content, position, buffer, offset, available);
            position += available;
            return available;
        }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var available = Math.Min(buffer.Length, content.Length - position);
            content.AsMemory(position, available).CopyTo(buffer);
            position += available;
            return ValueTask.FromResult(available);
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private static async Task LoginAsync(HttpClient client, string email, string password)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email, password, rememberMe = false })
        };
        request.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(client));
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<int> GetActiveAdministratorCountAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM \"AspNetUsers\" AS account JOIN \"AspNetUserRoles\" AS userRole ON userRole.\"UserId\" = account.\"Id\" JOIN \"AspNetRoles\" AS role ON role.\"Id\" = userRole.\"RoleId\" WHERE account.\"IsActive\" AND role.\"NormalizedName\" = 'ADMINISTRADOR'", connection);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task<HttpStatusCode> RemoveOtherAdministratorAsync(HttpClient client, Guid targetId, Barrier barrier)
    {
        using var change = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/users/{targetId}/role")
        {
            Content = JsonContent.Create(new { role = "Jugador" })
        };
        change.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(client));
        barrier.SignalAndWait(TimeSpan.FromSeconds(10));
        using var response = await client.SendAsync(change);
        return response.StatusCode;
    }

    private static async Task<HttpStatusCode> ResolveRoleRequestConcurrentlyAsync(HttpClient client, Guid requestId, string decision, Barrier barrier)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/role-requests/{requestId}/{decision}")
        {
            Content = JsonContent.Create(new { })
        };
        request.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(client));
        barrier.SignalAndWait(TimeSpan.FromSeconds(10));
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }
}
