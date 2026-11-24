using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
        var masterConnection = new NpgsqlConnectionStringBuilder(testDatabase);
        var schemaConnection = new NpgsqlConnectionStringBuilder(testDatabase) { SearchPath = schema };
        var previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__ClubDatabase");
        var previousMediaRoot = Environment.GetEnvironmentVariable("PostMedia__RootPath");
        var previousAvatarRoot = Environment.GetEnvironmentVariable("ProfileAvatar__RootPath");
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

            var health = await firstUser.GetAsync("/api/health");
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await firstUser.GetAsync("/api/v1/auth/me")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await firstUser.GetAsync("/api/v1/profile/")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await firstUser.GetAsync("/api/v1/feed")).StatusCode);
            var registration = await RegisterAsync(firstUser, "ana@example.es", "Jugador", "Ana");
            Assert.Contains(registration.Headers, header => header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) && string.Join(';', header.Value).Contains("cb-bunol-session", StringComparison.Ordinal));
            Assert.Equal(HttpStatusCode.OK, (await firstUser.GetAsync("/api/v1/auth/me")).StatusCode);
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
            using (var invalidProfileRequest = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile/")
            {
                Content = JsonContent.Create(new { firstName = "Ana", lastName = "Pruebas", email = administratorEmail, preferredTeamId = (Guid?)null })
            })
            {
                invalidProfileRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.BadRequest, (await firstUser.SendAsync(invalidProfileRequest)).StatusCode);
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
            var authorAvatarUrl = $"/api/v1/feed/author-avatar/{profileWithAvatar.GetProperty("id").GetGuid()}";
            Assert.Equal(HttpStatusCode.NotFound, (await firstUser.GetAsync(authorAvatarUrl)).StatusCode);
            using (var avatarPostForm = new MultipartFormDataContent { { new StringContent("Publicación con avatar"), "content" } })
            {
                using var avatarPostRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/feed") { Content = avatarPostForm };
                avatarPostRequest.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.Created, (await firstUser.SendAsync(avatarPostRequest)).StatusCode);
            }
            var avatarFeed = await firstUser.GetFromJsonAsync<JsonElement>("/api/v1/feed");
            var avatarPost = Assert.Single(avatarFeed.GetProperty("posts").EnumerateArray(), post => post.GetProperty("content").GetString() == "Publicación con avatar");
            Assert.Equal(authorAvatarUrl, avatarPost.GetProperty("authorAvatarUrl").GetString());
            Assert.Equal(HttpStatusCode.OK, (await firstUser.GetAsync(authorAvatarUrl)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await secondUser.GetAsync(authorAvatarUrl)).StatusCode);
            using (var avatarDelete = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/profile/avatar"))
            {
                avatarDelete.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(firstUser));
                Assert.Equal(HttpStatusCode.NoContent, (await firstUser.SendAsync(avatarDelete)).StatusCode);
            }
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
            using (var approvePlayer = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/role-requests/{playerRequest.GetProperty("id").GetGuid()}/approve") { Content = JsonContent.Create(new { }) })
            {
                approvePlayer.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync(administrator));
                Assert.Equal(HttpStatusCode.NoContent, (await administrator.SendAsync(approvePlayer)).StatusCode);
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
            Environment.SetEnvironmentVariable("BootstrapAdmin__Email", previousAdminEmail);
            Environment.SetEnvironmentVariable("BootstrapAdmin__Password", previousAdminPassword);
            if (Directory.Exists(mediaRoot)) Directory.Delete(mediaRoot, recursive: true);
            if (Directory.Exists(avatarRoot)) Directory.Delete(avatarRoot, recursive: true);
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

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("token").GetString()!;
    }
}
