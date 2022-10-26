using System;
using System.Collections.Generic;

using Xabbo.Interceptor;
using Xabbo.Core;
using Xabbo.Core.Tasks;

namespace Xabbo.Scripter.Scripting;

public partial class G
{
    /// <summary>
    /// Gets the user's data.
    /// </summary>
    public IUserData UserData => _profileManager.UserData ?? throw new Exception("The user's data has not yet been loaded.");

    /// <summary>
    /// Gets the user's ID.
    /// </summary>
    public long UserId => UserData.Id;

    /// <summary>
    /// Gets the user's name.
    /// </summary>
    public string UserName => UserData.Name;

    /// <summary>
    /// Gets the user's gender.
    /// </summary>
    public Gender UserGender => UserData.Gender;

    /// <summary>
    /// Gets the user's figure.
    /// </summary>
    public string UserFigure => UserData.Figure;

    /// <summary>
    /// Gets the user's motto.
    /// </summary>
    public string UserMotto => UserData.Motto;

    /// <summary>
    /// Gets whether the user's name can be changed.
    /// </summary>
    public bool UserNameChangeable => UserData.IsNameChangeable;

    /// <summary>
    /// Gets the user's achievements.
    /// </summary>
    public IAchievements UserAchievements => _profileManager.Achievements ?? throw new Exception("The user's achievements have not yet been loaded.");

    /// <summary>
    /// Gets the user's current credits.
    /// </summary>
    public int UserCredits => _profileManager.Credits ?? throw new Exception("User's credits have not yet been loaded.");

    /// <summary>
    /// Gets the user's activity points.
    /// </summary>
    public ActivityPoints UserPoints => _profileManager.Points ?? throw new Exception("User's points have not yet been loaded.");

    /// <summary>
    /// Gets the user's current diamonds.
    /// </summary>
    public int UserDiamonds => UserPoints[ActivityPointType.Diamond];

    /// <summary>
    /// Gets the user's current duckets.
    /// </summary>
    public int UserDuckets => UserPoints[ActivityPointType.Ducket];

    /// <summary>
    /// Sets the user's motto.
    /// </summary>
    /// <param name="motto">The new motto.</param>
    public void SetUserMotto(string motto)
    {
        ArgumentNullException.ThrowIfNull(motto);
        Interceptor.Send(Out.ChangeAvatarMotto, motto);
    }

    /// <summary>
    /// Sets the user's figure.
    /// </summary>
    /// <param name="figureString">The figure string.</param>
    /// <param name="gender">The gender of the figure.</param>
    public void SetUserFigure(string figureString, Gender gender)
    {
        ArgumentNullException.ThrowIfNull(figureString);
        Interceptor.Send(Out.UpdateAvatar, gender.ToShortString(), figureString);
    }

    /// <summary>
    /// Sets the user's figure, inferring the gender from the figure string.
    /// </summary>
    /// <param name="figureString">The figure string.</param>
    public void SetUserFigure(string figureString)
    {
        ArgumentNullException.ThrowIfNull(figureString);
        var figure = Figure.Parse(figureString);
        if (figure.Gender == Gender.Unisex)
            throw new Exception($"Unable to detect gender for figure string: {figureString}");
        SetUserFigure(figure.GetFigureString(), figure.Gender);
    }

    /// <summary>
    /// Gets the user's badges.
    /// </summary>
    /// <param name="timeout">The time to wait for a response from the server.</param>
    public List<Badge> GetUserBadges(int timeout = DEFAULT_TIMEOUT)
        => new GetBadgesTask(Interceptor).Execute(timeout, Ct);

    /// <summary>
    /// Gets the list of groups the user belongs to.
    /// </summary>
    /// <param name="timeout">The time to wait for a response from the server.</param>
    public List<GroupInfo> GetUserGroups(int timeout = DEFAULT_TIMEOUT)
    {
        var receiveTask = ReceiveAsync(In.GuildMemberships, timeout);
        Interceptor.Send(Out.GetGuildMemberships);
        var packet = receiveTask.GetAwaiter().GetResult();

        var list = new List<GroupInfo>();
        int n = packet.ReadInt();
        for (int i = 0; i < n; i++)
            list.Add(GroupInfo.Parse(packet));

        return list;
    }

    /// <summary>
    /// Gets the user's achievements.
    /// </summary>
    /// <param name="timeout">The time to wait for a response from the server.</param>
    public IAchievements GetUserAchievements(int timeout = DEFAULT_TIMEOUT)
        => new GetAchievementsTask(Interceptor).Execute(timeout, Ct);

    /// <summary>
    /// Gets the user's rooms.
    /// </summary>
    /// <param name="timeout">The time to wait for a response from the server.</param>
    public IEnumerable<IRoomInfo> GetUserRooms(int timeout = DEFAULT_TIMEOUT)
        => SearchNav("my", "", timeout);
}
