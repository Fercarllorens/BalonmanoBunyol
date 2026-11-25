using BalonmanoBunol.Domain.Auth;
using BalonmanoBunol.Domain.Administration;
using BalonmanoBunol.Domain.Social;
using BalonmanoBunol.Domain.Teams;

namespace BalonmanoBunol.Tests;

public sealed class DomainTests
{
    [Fact]
    public void Post_TrimsContent_AndStartsPublished()
    {
        var post = new Post(Guid.NewGuid(), "  Hola club  ");

        Assert.Equal("Hola club", post.Content);
        Assert.True(post.IsPublished);
        Assert.Null(post.DeletedAt);
    }

    [Fact]
    public void Post_RejectsEmptyAuthor_AndContentOverLimit()
    {
        Assert.Throws<ArgumentException>(() => new Post(Guid.Empty, "Texto"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Post(Guid.NewGuid(), new string('x', 5001)));
    }

    [Fact]
    public void Post_DeleteIsLogicalAndIdempotent()
    {
        var post = new Post(Guid.NewGuid(), "Publicación");

        post.Delete();
        var deletedAt = post.DeletedAt;
        post.Delete();

        Assert.False(post.IsPublished);
        Assert.NotNull(deletedAt);
        Assert.Equal(deletedAt, post.DeletedAt);
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("video/mp4")]
    [InlineData("video/webm")]
    public void PostAttachment_AcceptsSupportedFormats(string contentType)
    {
        var attachment = new PostAttachment(Guid.NewGuid(), "safe-key", "photo.jpg", contentType, 1);

        Assert.Equal(contentType, attachment.ContentType);
        Assert.Equal("photo.jpg", attachment.FileName);
    }

    [Fact]
    public void PostAttachment_RejectsUnsupportedMimeAndInvalidData()
    {
        Assert.Throws<ArgumentException>(() => new PostAttachment(Guid.NewGuid(), "safe", "x.svg", "image/svg+xml", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PostAttachment(Guid.NewGuid(), "safe", "x.jpg", "image/jpeg", 0));
        Assert.Throws<ArgumentException>(() => new PostAttachment(Guid.Empty, "safe", "x.jpg", "image/jpeg", 1));
    }

    [Theory]
    [InlineData("Jugador")]
    [InlineData("CuerpoTecnico")]
    public void RoleRequest_AllowsOnlySupportedRolesAndResolvesOnce(string role)
    {
        var administratorId = Guid.NewGuid();
        var request = new RoleRequest(Guid.NewGuid(), role);

        request.Approve(administratorId);

        Assert.Equal(RoleRequestStatus.Approved, request.Status);
        Assert.Equal(administratorId, request.ResolvedByUserId);
        Assert.NotNull(request.ResolvedAt);
        Assert.Throws<InvalidOperationException>(() => request.Reject(administratorId));
    }

    [Fact]
    public void RoleRequest_RejectsInvalidRoleAndEmptyAdministrator()
    {
        Assert.Throws<ArgumentException>(() => new RoleRequest(Guid.NewGuid(), "Administrador"));
        var request = new RoleRequest(Guid.NewGuid(), "Jugador");
        Assert.Throws<ArgumentException>(() => request.Reject(Guid.Empty));
    }

    [Fact]
    public void RoleRequest_ManualRoleChangeApprovesOnlyMatchingRequestedRole()
    {
        var administratorId = Guid.NewGuid();
        var matchingRequest = new RoleRequest(Guid.NewGuid(), "Jugador");
        matchingRequest.ResolveByManualRoleChange(administratorId, assignedRequestedRole: true);
        Assert.Equal(RoleRequestStatus.Approved, matchingRequest.Status);
        Assert.Equal(administratorId, matchingRequest.ResolvedByUserId);

        var differentRoleRequest = new RoleRequest(Guid.NewGuid(), "CuerpoTecnico");
        differentRoleRequest.ResolveByManualRoleChange(administratorId, assignedRequestedRole: false);
        Assert.Equal(RoleRequestStatus.Rejected, differentRoleRequest.Status);
    }

    [Fact]
    public void Team_TrimsRequiredFields()
    {
        var team = new Team("  Sénior ", " Masculino ");

        Assert.Equal("Sénior", team.Name);
        Assert.Equal("Masculino", team.Category);
        Assert.True(team.IsActive);
    }

    [Fact]
    public void Team_CanBeUpdatedAndDeactivated()
    {
        var team = new Team("Senior A", "Senior");
        team.Update("Senior A Masculino", "Senior");
        team.SetActive(false);

        Assert.Equal("Senior A Masculino", team.Name);
        Assert.Equal("Senior", team.Category);
        Assert.False(team.IsActive);
    }

    [Fact]
    public void TeamAssignment_RequiresActorAndReferences()
    {
        Assert.Throws<ArgumentException>(() => new TeamAssignment(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new TeamAssignment(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));
        var assignment = new TeamAssignment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Assert.NotEqual(Guid.Empty, assignment.Id);
    }

    [Fact]
    public void TeamMatch_StoresOpponentTimeAndVenue()
    {
        var startsAt = DateTimeOffset.UtcNow.AddDays(2);
        var match = new TeamMatch(Guid.NewGuid(), Guid.NewGuid(), "BM Quart", startsAt, "El Planell", "Jornada 17");
        Assert.Equal("BM Quart", match.Opponent);
        Assert.Equal(startsAt, match.StartsAt);
        Assert.Equal("El Planell", match.Venue);
    }

    [Fact]
    public void TeamEvent_OnlyAcceptsSupportedTypes()
    {
        Assert.Throws<ArgumentException>(() => new TeamEvent(Guid.NewGuid(), Guid.NewGuid(), "Actividad", "Otro", DateTimeOffset.UtcNow, null, "Pabellón", null));
        var teamEvent = new TeamEvent(Guid.NewGuid(), Guid.NewGuid(), "Entrenamiento", "Entrenamiento", DateTimeOffset.UtcNow, null, "El Planell", null);
        Assert.Equal("Entrenamiento", teamEvent.Kind);
    }

    [Fact]
    public void WeeklyTrainingSlot_ValidatesTimeRangeAndStoresOptionalVenue()
    {
        var planId = Guid.NewGuid();
        var slot = new TeamWeeklyTrainingSlot(planId, DayOfWeek.Monday, new TimeOnly(20, 0), new TimeOnly(21, 30), null);

        Assert.Equal(DayOfWeek.Monday, slot.DayOfWeek);
        Assert.Equal(string.Empty, slot.Venue);
        Assert.Throws<ArgumentException>(() => new TeamWeeklyTrainingSlot(planId, DayOfWeek.Monday, new TimeOnly(21, 0), new TimeOnly(20, 0), null));
    }

    [Fact]
    public void TrainingException_RequiresScheduleUnlessCancelled()
    {
        var slotId = Guid.NewGuid();
        var date = new DateOnly(2026, 9, 7);

        var cancelled = new TeamTrainingException(slotId, date, true, null, null, null);
        Assert.True(cancelled.IsCancelled);
        Assert.Throws<ArgumentException>(() => new TeamTrainingException(slotId, date, false, new TimeOnly(20, 0), null, null));
    }

    [Fact]
    public void AdministrationAuditEntry_StoresActionAndDetails()
    {
        var entry = new AdministrationAuditEntry(Guid.NewGuid(), Guid.NewGuid(), "RoleChanged", "Jugador -> CuerpoTecnico");
        Assert.Equal("RoleChanged", entry.Action);
        Assert.Equal("Jugador -> CuerpoTecnico", entry.Details);
        Assert.Throws<ArgumentException>(() => new AdministrationAuditEntry(Guid.Empty, null, "Accion", "Detalle"));
    }

    [Fact]
    public void TeamPlayerMembership_StoresJerseyAndPosition_AndUpdatesStatistics()
    {
        var membership = new TeamPlayerMembership(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 7, "Extremo Derecho");
        membership.Update(9, "Central", 12, 340, 25, 0);

        Assert.Equal(9, membership.JerseyNumber);
        Assert.Equal("Central", membership.Position);
        Assert.Equal(12, membership.MatchesPlayed);
        Assert.Equal(25, membership.Goals);
        Assert.Throws<ArgumentOutOfRangeException>(() => membership.Update(100, "Central", 0, 0, 0, 0));
    }

    [Fact]
    public void TeamCallup_AllowsPlayerToConfirmOrDeclineAttendance()
    {
        var callup = new TeamCallup(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        callup.Respond("Confirmada");
        Assert.Equal("Confirmada", callup.Attendance);
        Assert.NotNull(callup.RespondedAt);
        Assert.Throws<ArgumentException>(() => callup.Respond("Quizás"));
    }

    [Fact]
    public void TeamBoardPost_RequiresContentAndRejectsOversizedText()
    {
        Assert.Throws<ArgumentException>(() => new TeamBoardPost(Guid.NewGuid(), Guid.NewGuid(), "   "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TeamBoardPost(Guid.NewGuid(), Guid.NewGuid(), new string('x', 4001)));
    }
}
