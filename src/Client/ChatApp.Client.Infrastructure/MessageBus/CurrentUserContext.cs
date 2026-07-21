namespace ChatApp.Client.Infrastructure.MessageBus;

/// <summary>
/// Контекст текущего пользователя для фильтрации событий
/// Singleton для хранения информации о текущем пользователе
/// </summary>
public class CurrentUserContext
{
    private static CurrentUserContext? _instance;
    private static readonly object _lock = new object();

    public Guid UserId { get; private set; }
    public string Username { get; private set; } = string.Empty;

    private CurrentUserContext() { }

    public static CurrentUserContext Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new CurrentUserContext();
                    }
                }
            }
            return _instance;
        }
    }

    public void SetCurrentUser(Guid userId, string username)
    {
        UserId = userId;
        Username = username;
    }

    public bool IsCurrentUser(Guid userId)
    {
        return UserId != Guid.Empty && UserId == userId;
    }

    public void Clear()
    {
        UserId = Guid.Empty;
        Username = string.Empty;
    }
}
