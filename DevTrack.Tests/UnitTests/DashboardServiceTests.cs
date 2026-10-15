using DevTrack.Application.Interfaces;
using DevTrack.Domain.Entities;
using DevTrack.Domain.Enums;
using DevTrack.Infrastructure.Data;
using DevTrack.Infrastructure.Services;

namespace DevTrack.Tests.UnitTests;

public class DashboardServiceTests
{
    private static readonly DateOnly Today = new(2026, 10, 14);
    private static readonly DateTime TodayStart = Today.ToDateTime(TimeOnly.MinValue);

    private readonly DevTrackDbContext _db;
    private readonly DashboardService _sut;

    public DashboardServiceTests()
    {
        _db = TestDbContextFactory.Create();
        _sut = new DashboardService(_db);
    }

    private async Task<User> AddUserAsync(string name, UserRole role = UserRole.Developer, bool isActive = true)
    {
        var user = new User
        {
            FullName = name,
            Email = $"{name.ToLower().Replace(' ', '.')}@example.com",
            PasswordHash = "x",
            Role = role,
            IsActive = isActive
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<Team> AddTeamAsync(string name, User owner, params User[] members)
    {
        var team = new Team { Name = name, OwnerId = owner.Id };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        foreach (var member in members)
            _db.TeamMembers.Add(new TeamMember { TeamId = team.Id, UserId = member.Id });
        await _db.SaveChangesAsync();
        return team;
    }

    private async Task<Project> AddProjectAsync(
        string name, Team team, ProjectStatus status = ProjectStatus.Active, DateTime? dueDate = null)
    {
        var project = new Project { Name = name, TeamId = team.Id, StartDate = TodayStart, Status = status, DueDate = dueDate };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return project;
    }

    private async Task AddProjectMemberAsync(Project project, User user)
    {
        _db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = user.Id });
        await _db.SaveChangesAsync();
    }

    private async Task<TaskItem> AddTaskAsync(
        Project project, User creator, TaskItemStatus status = TaskItemStatus.ToDo,
        DateTime? dueDate = null, User? assignee = null)
    {
        var task = new TaskItem
        {
            Title = "Task",
            ProjectId = project.Id,
            CreatorId = creator.Id,
            Status = status,
            DueDate = dueDate,
            AssigneeId = assignee?.Id
        };
        _db.TaskItems.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    private async Task AddActivityAsync(User user, int? projectId, string action, DateTime timestamp)
    {
        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = user.Id,
            ProjectId = projectId,
            Action = action,
            EntityType = "Task",
            EntityId = 1,
            Timestamp = timestamp
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetStatsAsync_AsAdmin_CountsEverythingExactly()
    {
        var admin = await AddUserAsync("Admin", UserRole.Admin);
        var manager = await AddUserAsync("Manager", UserRole.ProjectManager);
        await AddUserAsync("Active Dev");
        await AddUserAsync("Gone Dev", isActive: false);
        var teamA = await AddTeamAsync("Team A", manager);
        var teamB = await AddTeamAsync("Team B", manager);
        var active = await AddProjectAsync("Active", teamA, ProjectStatus.Active);
        var planning = await AddProjectAsync("Planning", teamB, ProjectStatus.Planning);
        await AddProjectAsync("Archived", teamA, ProjectStatus.Archived);

        await AddTaskAsync(active, manager, TaskItemStatus.ToDo, TodayStart.AddDays(-1));      // overdue
        await AddTaskAsync(active, manager, TaskItemStatus.InProgress, TodayStart);            // due today, not overdue
        await AddTaskAsync(active, manager, TaskItemStatus.Done, TodayStart.AddDays(-1));      // finished, so not overdue
        await AddTaskAsync(active, manager, TaskItemStatus.Backlog);
        await AddTaskAsync(planning, manager, TaskItemStatus.ToDo, TodayStart.AddDays(3));

        var stats = await _sut.GetStatsAsync(admin.Id, UserRole.Admin, Today);

        Assert.Equal(3, stats.TotalProjects);
        Assert.Equal(1, stats.ActiveProjects);
        Assert.Equal(2, stats.TotalTeams);
        Assert.Equal(5, stats.TotalTasks);
        Assert.Equal(1, stats.CompletedTasks);
        Assert.Equal(1, stats.OverdueTasks);

        var byStatus = stats.TasksByStatus.ToDictionary(s => s.Status, s => s.Count);
        Assert.Equal(5, byStatus.Count);
        Assert.Equal(1, byStatus["Backlog"]);
        Assert.Equal(2, byStatus["ToDo"]);
        Assert.Equal(1, byStatus["InProgress"]);
        Assert.Equal(0, byStatus["InReview"]);
        Assert.Equal(1, byStatus["Done"]);

        Assert.Equal(4, stats.TotalUsers);
        Assert.Equal(3, stats.ActiveUsers);
    }

    [Fact]
    public async Task GetStatsAsync_OverdueMeansDueBeforeToday_AndIgnoresFinishedTasks()
    {
        var admin = await AddUserAsync("Admin", UserRole.Admin);
        var team = await AddTeamAsync("Team", admin);
        var project = await AddProjectAsync("Project", team);

        await AddTaskAsync(project, admin, TaskItemStatus.ToDo, TodayStart.AddDays(-1));       // overdue
        await AddTaskAsync(project, admin, TaskItemStatus.InReview, TodayStart.AddDays(-30));  // overdue
        await AddTaskAsync(project, admin, TaskItemStatus.InProgress, TodayStart);             // due today: not overdue yet
        await AddTaskAsync(project, admin, TaskItemStatus.InProgress, TodayStart.AddHours(15)); // due this afternoon
        await AddTaskAsync(project, admin, TaskItemStatus.Done, TodayStart.AddDays(-1));       // finished
        await AddTaskAsync(project, admin, TaskItemStatus.ToDo);                               // no due date

        var stats = await _sut.GetStatsAsync(admin.Id, UserRole.Admin, Today);

        Assert.Equal(2, stats.OverdueTasks);
    }

    [Fact]
    public async Task GetStatsAsync_DueSoonCoversTodayThroughSevenDaysAhead()
    {
        var manager = await AddUserAsync("Manager", UserRole.ProjectManager);
        var dev = await AddUserAsync("Dev One");
        var team = await AddTeamAsync("Team", manager, dev);
        var project = await AddProjectAsync("Project", team);
        await AddProjectMemberAsync(project, dev);

        var dueToday = await AddTaskAsync(project, manager, TaskItemStatus.ToDo, TodayStart, dev);
        var dueInSeven = await AddTaskAsync(project, manager, TaskItemStatus.InProgress, TodayStart.AddDays(7), dev);
        await AddTaskAsync(project, manager, TaskItemStatus.ToDo, TodayStart.AddDays(8), dev);   // outside the window
        await AddTaskAsync(project, manager, TaskItemStatus.ToDo, TodayStart.AddDays(-1), dev);  // already overdue
        await AddTaskAsync(project, manager, TaskItemStatus.Done, TodayStart.AddDays(3), dev);   // finished
        var unassigned = await AddTaskAsync(project, manager, TaskItemStatus.ToDo, TodayStart.AddDays(2));

        var stats = await _sut.GetStatsAsync(dev.Id, UserRole.Developer, Today);

        Assert.Equal(2, stats.MyTasksDueSoon);
        Assert.Equal(4, stats.MyOpenTasks); // today, +7, +8 and yesterday; the finished one doesn't count
        Assert.Equal(
            new[] { dueToday.Id, unassigned.Id, dueInSeven.Id },
            stats.UpcomingTasks.Select(t => t.Id).ToArray());
        Assert.Equal("Dev One", stats.UpcomingTasks[0].AssigneeName);
        Assert.Null(stats.UpcomingTasks[1].AssigneeName);
    }

    [Fact]
    public async Task GetStatsAsync_AsDeveloper_OnlyCountsWhatTheyCanAccess()
    {
        var manager = await AddUserAsync("Manager", UserRole.ProjectManager);
        var dev = await AddUserAsync("Dev One");
        var teamOne = await AddTeamAsync("Team One", manager, dev);
        var teamTwo = await AddTeamAsync("Team Two", manager, dev);
        var teamThree = await AddTeamAsync("Team Three", manager);

        var onProject = await AddProjectAsync("On the project", teamOne);
        await AddProjectMemberAsync(onProject, dev);
        var teamOnly = await AddProjectAsync("Team only", teamTwo);
        var unrelated = await AddProjectAsync("Unrelated", teamThree);

        for (var i = 0; i < 2; i++) await AddTaskAsync(onProject, manager);
        for (var i = 0; i < 3; i++) await AddTaskAsync(teamOnly, manager);
        for (var i = 0; i < 4; i++) await AddTaskAsync(unrelated, manager);

        var asDev = await _sut.GetStatsAsync(dev.Id, UserRole.Developer, Today);

        Assert.Equal(2, asDev.TotalProjects); // the projects of the teams they belong to
        Assert.Equal(2, asDev.TotalTeams);
        Assert.Equal(2, asDev.TotalTasks);    // only the project they are a member of
        Assert.Null(asDev.TotalUsers);
        Assert.Null(asDev.ActiveUsers);

        // A team owner can open every project of their teams, whether or not they joined it.
        var asOwner = await _sut.GetStatsAsync(manager.Id, UserRole.ProjectManager, Today);
        Assert.Equal(3, asOwner.TotalProjects);
        Assert.Equal(9, asOwner.TotalTasks);
    }

    [Fact]
    public async Task GetStatsAsync_ProjectProgress_IsCalculatedFromTasksAndSkipsArchivedProjects()
    {
        var admin = await AddUserAsync("Admin", UserRole.Admin);
        var team = await AddTeamAsync("Team", admin);
        var dueSoon = await AddProjectAsync("Due soon", team, ProjectStatus.Active, TodayStart.AddDays(10));
        await AddProjectAsync("Open ended", team, ProjectStatus.Planning);
        var archived = await AddProjectAsync("Archived", team, ProjectStatus.Archived);

        await AddTaskAsync(dueSoon, admin, TaskItemStatus.Done);
        await AddTaskAsync(dueSoon, admin, TaskItemStatus.ToDo);
        await AddTaskAsync(dueSoon, admin, TaskItemStatus.InProgress);
        await AddTaskAsync(archived, admin, TaskItemStatus.Done);

        var stats = await _sut.GetStatsAsync(admin.Id, UserRole.Admin, Today);

        Assert.Equal(2, stats.ProjectProgress.Count);

        var first = stats.ProjectProgress[0];
        Assert.Equal("Due soon", first.Name);
        Assert.Equal(3, first.TotalTasks);
        Assert.Equal(1, first.CompletedTasks);
        Assert.Equal(33, first.ProgressPercent);

        var second = stats.ProjectProgress[1];
        Assert.Equal("Open ended", second.Name);
        Assert.Equal(0, second.TotalTasks);
        Assert.Equal(0, second.ProgressPercent);
    }

    [Fact]
    public async Task GetRecentActivityAsync_ReturnsNewestFirst_WithUserAndProjectNames()
    {
        var admin = await AddUserAsync("Admin", UserRole.Admin);
        var team = await AddTeamAsync("Team", admin);
        var project = await AddProjectAsync("Alpha", team);
        var start = new DateTime(2026, 10, 1, 9, 0, 0);

        await AddActivityAsync(admin, project.Id, "First", start);
        await AddActivityAsync(admin, project.Id, "Third", start.AddHours(2));
        await AddActivityAsync(admin, project.Id, "Second", start.AddHours(1));

        var result = await _sut.GetRecentActivityAsync(admin.Id, UserRole.Admin, 1, 20);

        Assert.Equal(new[] { "Third", "Second", "First" }, result.Items.Select(a => a.Action).ToArray());
        Assert.All(result.Items, a =>
        {
            Assert.Equal("Alpha", a.ProjectName);
            Assert.Equal("Admin", a.UserName);
        });
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task GetRecentActivityAsync_OnlyShowsProjectsTheUserCanOpen_PlusTheirOwnDeletedProjectEntries()
    {
        var manager = await AddUserAsync("Manager", UserRole.ProjectManager);
        var dev = await AddUserAsync("Dev One");
        var teamOne = await AddTeamAsync("Team One", manager, dev);
        var teamTwo = await AddTeamAsync("Team Two", manager, dev);
        var teamThree = await AddTeamAsync("Team Three", manager);

        var onProject = await AddProjectAsync("On the project", teamOne);
        await AddProjectMemberAsync(onProject, dev);
        var teamOnly = await AddProjectAsync("Team only", teamTwo);
        var unrelated = await AddProjectAsync("Unrelated", teamThree);

        var start = new DateTime(2026, 10, 1, 9, 0, 0);
        await AddActivityAsync(manager, onProject.Id, "On project", start);
        await AddActivityAsync(manager, teamOnly.Id, "Team only", start.AddMinutes(1));
        await AddActivityAsync(manager, unrelated.Id, "Unrelated", start.AddMinutes(2));
        await AddActivityAsync(dev, null, "Own deleted project", start.AddMinutes(3));
        await AddActivityAsync(manager, null, "Someone else's deleted project", start.AddMinutes(4));

        var asDev = await _sut.GetRecentActivityAsync(dev.Id, UserRole.Developer, 1, 20);
        Assert.Equal(new[] { "Own deleted project", "On project" }, asDev.Items.Select(a => a.Action).ToArray());

        // The manager owns all three teams, so sees all three projects plus their own deleted-project entry.
        var asOwner = await _sut.GetRecentActivityAsync(manager.Id, UserRole.ProjectManager, 1, 20);
        Assert.Equal(4, asOwner.TotalCount);
    }

    [Fact]
    public async Task GetRecentActivityAsync_PagesResults_AndClampsBadPagingValues()
    {
        var admin = await AddUserAsync("Admin", UserRole.Admin);
        var team = await AddTeamAsync("Team", admin);
        var project = await AddProjectAsync("Project", team);
        var start = new DateTime(2026, 10, 1, 9, 0, 0);
        for (var i = 0; i < 3; i++)
            await AddActivityAsync(admin, project.Id, $"Entry {i}", start.AddMinutes(i));

        var secondPage = await _sut.GetRecentActivityAsync(admin.Id, UserRole.Admin, 2, 2);
        Assert.Single(secondPage.Items);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(2, secondPage.TotalPages);

        var clamped = await _sut.GetRecentActivityAsync(admin.Id, UserRole.Admin, 0, 1000);
        Assert.Equal(1, clamped.CurrentPage);
        Assert.Equal(100, clamped.PageSize);
    }
}