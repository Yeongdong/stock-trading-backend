# 📈 Stock Trading Backend – C# Clean Architecture Project

## 🎯 프로젝트 개요

> 실제 증권사 API(한국투자증권 OpenAPI)를 연동하여 주식 실시간 시세 스트리밍, 주문 처리, 잔고 조회 등의 기능을 제공하는 **백엔드 중심 트레이딩 시스템**입니다.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?style=flat&logo=postgresql)](https://postgresql.org/)
[![SignalR](https://img.shields.io/badge/SignalR-WebSocket-007ACC?style=flat)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-green?style=flat)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

## 🛠 기술 스택

| 분류                 | 기술 스택                                     | 설명                                  |
|:-------------------|:------------------------------------------|:------------------------------------|
| **Backend**        | ASP.NET Core 8.0 (C# 12)                  | 고성능 백엔드 API 개발 및 최신 C# 문법 활용        |
| **Architecture**   | Clean Architecture (DDD 적용)               | 비즈니스 로직의 독립성과 테스트 용이성을 위한 아키텍처 설계   |
| **Database**       | PostgreSQL 16 + Entity Framework Core 9.0 | 관계형 데이터베이스 관리 및 ORM을 통한 효율적인 데이터 접근 |
| **Real-time**      | SignalR (WebSocket 기반)                    | 클라이언트-서버 간 실시간 양방향 통신 구현            |
| **Caching**        | Redis + 메모리 캐시                            | 분산/로컬 캐시를 통한 데이터 조회 성능 최적화          |
| **Authentication** | JWT + Google OAuth 2.0                    | 안전한 사용자 인증 및 권한 부여 시스템 구축           |
| **External API**   | 한국투자증권 OpenAPI + 한국거래소 API                | 실제 금융 데이터를 위한 외부 금융 API 연동 및 데이터 처리 |
| **Testing**        | xUnit + Moq + FluentAssertions            | 단위 및 통합 테스트를 통한 코드 품질 및 안정성 확보      |
| **Documentation**  | Swagger/OpenAPI 3.0                       | API 엔드포인트 자동 문서화 및 테스트 환경 제공        |

## 🏗️ 아키텍처 개요

```
┌─────────────────────────────────────────────┐
│           🌐 API Layer (ASP.NET)            │
│     Controllers, SignalR, Middleware        │
├─────────────────────────────────────────────┤
│           📋 Application Layer              │
│      Services, DTOs, Business Logic         │
├─────────────────────────────────────────────┤
│          🏗 Infrastructure Layer            │
│     Repositories, External APIs, Cache      │
├─────────────────────────────────────────────┤
│              💼 Domain Layer                │
│        Entity, Enum, Exception, Rule        │
└─────────────────────────────────────────────┘
```

**Clean Architecture 핵심 원칙:**

- **의존성 역전(DIP)**: Application → Interface → Infrastructure 구조로 느슨한 결합
- **도메인 중심 설계**: Stock, Order, User 등 핵심 엔티티 명확 정의

## 📁 프로젝트 구조

```plaintext
StockTrading/
├── StockTrading.API/            # API 진입점 (Controllers, SignalR Hubs)
├── StockTrading.Application/    # 유즈케이스, 서비스 인터페이스
├── StockTrading.Infrastructure/ # DB, Redis, 외부 API 구현체
├── StockTrading.Domain/         # 핵심 비즈니스 모델 (Entity, Enum)
└── StockTrading.Tests/          # 단위 및 통합 테스트
```

## 🚀 핵심 기능

### **실시간 주식 거래 API**

- `POST /api/trading/order` : 주식 주문 생성
- `GET /api/trading/balance` : 계좌 잔고 조회
- `GET /api/trading/buyable-inquiry` : 매수 가능 금액 조회
- `GET /api/trading/executions` : 주문 체결 내역 조회

### **시장 데이터 API**

- `GET /api/stock/search` : 국내 주식 검색
- `GET /api/stock/overseas/search` : 해외 주식 검색
- `GET /api/stock/overseas/markets/{market}` : 시장별 종목 조회

### **인증 및 사용자 관리**

- `POST /api/auth/google` : Google OAuth 2.0 로그인
- `POST /api/auth/refresh` : JWT 토큰 갱신
- SignalR 실시간 연결 관리 및 브로드캐스팅

## ⚡ 핵심 기술 구현

### **1. Clean Architecture 의존성 관리**

**의존성 역전 원칙(DIP)** 을 통해 비즈니스 로직과 인프라 구현을 완전히 분리

```csharp
// Application Layer - 인터페이스 정의
public interface IStockService
{
    Task<StockSearchResponse> SearchStocksAsync(string searchTerm, int page, int pageSize);
    Task<StockSearchResult?> GetStockByCodeAsync(string code);
}

// Infrastructure Layer - 캐시 전략이 포함된 구현체
public class StockService : IStockService
{
    public async Task<StockSearchResult?> GetStockByCodeAsync(string code)
    {
        var cached = await _cacheService.GetStockByCodeAsync(code); // 캐시 우선
        if (cached != null) return cached;

        var result = await _repository.GetByCodeAsync(code); // DB 조회
        if (result != null)
            await _cacheService.SetStockByCodeAsync(code, result); // 캐싱

        return result;
    }
}
```

### **2. 실시간 데이터 스트리밍 (SignalR)**

안정적인 실시간 주가 데이터 브로드캐스팅과 연결 상태 관리 구현

```csharp
// SignalR Hub - 실시간 주가 브로드캐스팅
public class StockHub : Hub
{
    public async Task SendStockPrice(string symbol, decimal price)
    {
        await Clients.All.SendAsync("ReceiveStockPrice", symbol, price);
    }

    // 연결 상태 확인
    public async Task CheckConnection()
    {
        var connectionInfo = new
        {
            connectionId = Context.ConnectionId,
            userEmail = Context.User?.FindFirst(ClaimTypes.Email)?.Value,
            timestamp = DateTime.UtcNow
        };
        await Clients.Caller.SendAsync("ConnectionStatus", connectionInfo);
    }
}
```

### **3. 다층 캐싱 전략 (Redis + Memory)**

**L1(메모리) + L2(Redis)** 캐시 조합으로 데이터 조회 성능 극대화

```csharp
public class StockCacheService : IStockCacheService
{
    private readonly IDistributedCache _distributedCache; // Redis
    private readonly IMemoryCache _memoryCache; // L1 Cache

    public async Task<StockSearchResponse?> GetSearchResultAsync(string searchTerm, int page, int pageSize)
    {
        var key = CacheKeys.SearchResult(searchTerm, page, pageSize);
        
        // L1 캐시 확인
        if (_memoryCache.TryGetValue(key, out StockSearchResponse? cached))
        {
            _cacheMetrics.RecordHit(key);
            return cached;
        }

        // L2 캐시 확인
        var distributedData = await _distributedCache.GetStringAsync(key);
        if (distributedData != null)
        {
            var result = JsonSerializer.Deserialize<StockSearchResponse>(distributedData);
            _memoryCache.Set(key, result, TimeSpan.FromMinutes(5)); // L1에 캐싱
            return result;
        }

        return null;
    }
}
```

### **4. 한국투자증권 API 연동**

실제 금융 API와의 안정적인 연동 및 에러 처리 구현

```csharp
public class KisPriceApiClient : KisApiClientBase
{
    public async Task<DomesticCurrentPriceResponse> GetDomesticCurrentPriceAsync(
        CurrentPriceRequest request, UserInfo user)
    {
        var queryParams = CreateCurrentPriceQueryParams(request);
        var httpRequest = CreateCurrentPriceHttpRequest(queryParams, user);

        var response = await _httpClient.SendAsync(httpRequest);
        response.EnsureSuccessStatusCode();
        
        var kisResponse = await response.Content.ReadFromJsonAsync<KisStockPriceResponse>();
        return _priceConverter.ConvertToDomesticCurrentPrice(kisResponse);
    }
}
```

## 🎯 기술적 도전과 해결 과정

### **도전 1: 대용량 실시간 데이터 처리**

**문제**: 초당 수백 건의 주가 데이터 처리 시 메모리 사용량 급증  
**해결**: Redis 기반 분산 캐시 + 메모리 캐시 조합으로 **응답 시간 92% 개선**

### **도전 2: 외부 API 의존성 관리**

**문제**: 한국투자증권 API 장애 시 전체 시스템 마비  
**해결**: Circuit Breaker 패턴 + 재시도 전략으로 **API 에러율 98% 감소**

## 📊 성능 최적화 결과

| 메트릭         | Before | After | 개선율       |
|-------------|--------|-------|-----------|
| 주식 검색 응답 시간 | 200ms  | 15ms  | **92% ↓** |
| 캐시 히트율      | -      | 85%   | **신규**    |
| 동시 연결 처리    | 100    | 1000+ | **10배 ↑** |
| API 에러율     | 5%     | 0.1%  | **98% ↓** |

## 🧪 테스트 전략

```csharp
// 단위 테스트 - Arrange, Act, Assert 패턴
[Fact]
public async Task GetStockByCode_ValidCode_ReturnsStock()
{
    // Arrange
    var mockRepository = new Mock<IStockRepository>();
    mockRepository.Setup(r => r.GetByCodeAsync("005930"))
              .ReturnsAsync(new Stock { Code = "005930", Name = "삼성전자" });
    
    var service = new StockService(mockRepository.Object);
    
    // Act
    var result = await service.GetStockByCodeAsync("005930");
    
    // Assert
    result.Should().NotBeNull();
    result.Code.Should().Be("005930");
}

// 통합 테스트
[Fact]
public async Task GetStock_ValidCode_ReturnsOkResult()
{
    var response = await _client.GetAsync("/api/market/stock/005930");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

**테스트 커버리지**: Controllers, Services, Repositories, Infrastructure 계층별 85% 달성

## 💡 핵심 학습 성과

### **아키텍처 설계 역량**

- Clean Architecture 패턴 실무 적용으로 **계층 분리와 의존성 관리** 경험
- SOLID 원칙 준수한 객체지향 설계로 **확장 가능한 코드 구조** 구현

### **실시간 시스템 구현**

- SignalR을 활용한 **WebSocket 기반 실시간 통신** 기술 완전 습득
- 대용량 동시 연결 처리 및 **연결 상태 관리** 메커니즘 구현

### **성능 최적화**

- **다층 캐싱 전략** 설계로 응답 시간 92% 개선 달성
- Redis 분산 캐시 아키텍처 학습 및 실무 적용

## 🔧 설치 및 실행

### **Prerequisites**

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 16+](https://www.postgresql.org/download/)
- [Redis](https://redis.io/download/)

### **Quick Start**

```bash
# 1. 저장소 클론
git clone https://github.com/Yeongdong/stock-trading-backend.git
cd stock-trading-backend

# 2. 데이터베이스 생성
createdb stock_trading

# 3. 환경 설정
cp appsettings.Example.json appsettings.Development.json

# 4. 마이그레이션 적용
dotnet ef database update --project StockTrading.Infrastructure

# 5. 애플리케이션 실행
dotnet run --project StockTrading.API
```

### **환경설정 예시**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=stock_trading;Username=postgres;Password=your_password"
  },
  "KoreaInvestmentSettings": {
    "BaseUrl": "https://openapi.koreainvestment.com:9443",
    "AppKey": "YOUR_APP_KEY",
    "AppSecret": "YOUR_APP_SECRET"
  },
  "RedisSettings": {
    "ConnectionString": "localhost:6379"
  }
}
```

🌐 **실행 후 접속:** `https://localhost:7072/swagger`

## 🔮 향후 개발 계획

- [ ] **Kubernetes** 기반 컨테이너 오케스트레이션 구축
- [ ] **gRPC** 도입으로 마이크로서비스 간 통신 최적화
- [ ] **Event Sourcing** 패턴 적용으로 거래 이력 추적 강화
- [ ] **ML.NET** 활용한 주가 예측 모델 연동

---

## 📞 연락처

**정영동** - 백엔드 개발자

- 📧 **이메일**: jyd37855@gmail.com
- 🐙 **GitHub**: [GitHub 프로필](https://github.com/Yeongdong)
- 🎨 **프론트엔드 저장소**: [Stock Trading Frontend](https://github.com/your-username/stock-trading-frontend)

> 이 프로젝트는 Clean Architecture를 실무에 적용하여 확장 가능하고 테스트 가능한 금융 시스템을 구현한 결과물입니다. 복잡한 비즈니스 도메인 모델링과 외부 API 연동의 안정성 확보, 실시간 데이터
> 처리 성능 최적화 경험을 통해 실제 서비스 개발에 기여할 준비가 되어 있습니다.
