using StockTrading.Application.Features.Users.DTOs;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.RealTime.State;

public class ServiceState
{
    private bool _isStarted;
    private UserInfo? _currentUser;
    private readonly object _lockObject = new();

    public bool IsStarted
    {
        get
        {
            lock (_lockObject)
            {
                return _isStarted;
            }
        }
        private set
        {
            lock (_lockObject)
            {
                _isStarted = value;
            }
        }
    }

    public UserInfo? CurrentUser
    {
        get
        {
            lock (_lockObject)
            {
                return _currentUser;
            }
        }
        private set
        {
            lock (_lockObject)
            {
                _currentUser = value;
            }
        }
    }

    public void Start(UserInfo user)
    {
        ArgumentNullException.ThrowIfNull(user);

        lock (_lockObject)
        {
            _currentUser = user;
            _isStarted = true;
        }
    }

    public void Stop()
    {
        lock (_lockObject)
        {
            _isStarted = false;
            _currentUser = null;
        }
    }

    public void EnsureStarted()
    {
        if (!IsStarted)
            throw new InvalidOperationException("서비스를 먼저 시작하세요");
    }

    public void EnsureUserExists()
    {
        if (CurrentUser == null)
            throw new InvalidOperationException("사용자 정보가 없습니다. 서비스를 다시 시작해주세요.");
    }
}