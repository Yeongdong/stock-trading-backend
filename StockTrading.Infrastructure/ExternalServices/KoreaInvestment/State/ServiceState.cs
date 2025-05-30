using StockTrading.Application.DTOs.Users;

namespace StockTrading.Infrastructure.ExternalServices.KoreaInvestment.State;

/// <summary>
/// 실시간 서비스 상태 관리
/// </summary>
public class ServiceState
{
    private bool _isStarted;
    private UserInfo? _currentUser;
    private readonly object _lockObject = new();

    /// <summary>
    /// 서비스 시작 여부
    /// </summary>
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

    /// <summary>
    /// 현재 사용자 정보
    /// </summary>
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

    /// <summary>
    /// 서비스 시작
    /// </summary>
    public void Start(UserInfo user)
    {
        ArgumentNullException.ThrowIfNull(user);

        lock (_lockObject)
        {
            _currentUser = user;
            _isStarted = true;
        }
    }

    /// <summary>
    /// 서비스 중지
    /// </summary>
    public void Stop()
    {
        lock (_lockObject)
        {
            _isStarted = false;
            _currentUser = null;
        }
    }

    /// <summary>
    /// 서비스가 시작되었는지 확인
    /// </summary>
    public void EnsureStarted()
    {
        if (!IsStarted)
            throw new InvalidOperationException("서비스를 먼저 시작하세요");
    }

    /// <summary>
    /// 사용자 정보가 있는지 확인
    /// </summary>
    public void EnsureUserExists()
    {
        if (CurrentUser == null)
            throw new InvalidOperationException("사용자 정보가 없습니다. 서비스를 다시 시작해주세요.");
    }
}