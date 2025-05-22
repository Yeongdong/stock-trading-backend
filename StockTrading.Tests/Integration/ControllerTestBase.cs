using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace StockTrading.Tests.Integration;

/// <summary>
/// 컨트롤러별 통합테스트의 기본 베이스 클래스
/// AuthenticatedTestBase를 확장하여 컨트롤러 테스트에 특화된 기능 제공
/// </summary>
/// <typeparam name="TController">테스트할 컨트롤러 타입</typeparam>
public abstract class ControllerTestBase<TController> : AuthenticatedTestBase
    where TController : ControllerBase
{
    /// <summary>
    /// 테스트 대상 컨트롤러의 기본 경로
    /// </summary>
    protected string BaseRoute { get; private set; }

    /// <summary>
    /// 컨트롤러 타입 정보
    /// </summary>
    protected Type ControllerType { get; private set; }

    /// <summary>
    /// 컨트롤러명 (Controller 접미사 제거된 형태)
    /// </summary>
    protected string ControllerName { get; private set; }

    /// <summary>
    /// JSON 직렬화 옵션
    /// </summary>
    protected JsonSerializerOptions JsonOptions { get; private set; }

    protected ControllerTestBase(IntegrationTestWebApplicationFactory factory) 
        : base(factory)
    {
        InitializeControllerInfo();
        InitializeJsonOptions();
    }

    #region 초기화

    /// <summary>
    /// 컨트롤러 정보 초기화
    /// </summary>
    private void InitializeControllerInfo()
    {
        ControllerType = typeof(TController);
        ControllerName = ExtractControllerName(ControllerType);
        BaseRoute = DetermineBaseRoute();

        Logger.LogDebug("컨트롤러 테스트 초기화: {ControllerName} -> {BaseRoute}", 
            ControllerName, BaseRoute);
    }

    /// <summary>
    /// JSON 직렬화 옵션 초기화
    /// </summary>
    private void InitializeJsonOptions()
    {
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// 컨트롤러명 추출 (Controller 접미사 제거)
    /// </summary>
    private static string ExtractControllerName(Type controllerType)
    {
        var typeName = controllerType.Name;
        return typeName.EndsWith("Controller") 
            ? typeName[..^"Controller".Length] 
            : typeName;
    }

    /// <summary>
    /// 기본 라우트 결정
    /// Route 어트리뷰트가 있으면 사용하고, 없으면 컨트롤러명 기반으로 생성
    /// </summary>
    private string DetermineBaseRoute()
    {
        // Route 어트리뷰트 확인
        var routeAttribute = ControllerType.GetCustomAttribute<RouteAttribute>();
        if (routeAttribute != null)
        {
            var route = routeAttribute.Template;
            // [controller] 토큰 치환
            route = route.Replace("[controller]", ControllerName, StringComparison.OrdinalIgnoreCase);
            return EnsureRouteFormat(route);
        }

        // 기본 API 컨트롤러 경로 생성
        return $"/api/{ControllerName}";
    }

    /// <summary>
    /// 라우트 형식 보장 (앞에 /가 있도록)
    /// </summary>
    private static string EnsureRouteFormat(string route)
    {
        return route.StartsWith('/') ? route : $"/{route}";
    }

    #endregion

    #region HTTP 요청 헬퍼 메서드들

    /// <summary>
    /// GET 요청 (상대 경로)
    /// </summary>
    protected virtual async Task<HttpResponseMessage> GetAsync(string relativePath = "")
    {
        var fullPath = BuildFullPath(relativePath);
        return await AuthenticatedGetAsync(fullPath);
    }

    /// <summary>
    /// GET 요청 후 타입 변환
    /// </summary>
    protected virtual async Task<T> GetAsync<T>(string relativePath = "")
    {
        var response = await GetAsync(relativePath);
        await HttpClientHelper.EnsureSuccessStatusCodeAsync(response);
        return await HttpClientHelper.DeserializeResponseAsync<T>(response);
    }

    /// <summary>
    /// POST 요청 (JSON)
    /// </summary>
    protected virtual async Task<HttpResponseMessage> PostAsync<T>(T content, string relativePath = "")
    {
        var fullPath = BuildFullPath(relativePath);
        return await AuthenticatedPostJsonAsync(fullPath, content);
    }

    /// <summary>
    /// POST 요청 후 타입 변환
    /// </summary>
    protected virtual async Task<TResponse> PostAsync<TRequest, TResponse>(TRequest content, string relativePath = "")
    {
        var response = await PostAsync(content, relativePath);
        await HttpClientHelper.EnsureSuccessStatusCodeAsync(response);
        return await HttpClientHelper.DeserializeResponseAsync<TResponse>(response);
    }

    /// <summary>
    /// PUT 요청 (JSON)
    /// </summary>
    protected virtual async Task<HttpResponseMessage> PutAsync<T>(T content, string relativePath = "")
    {
        var fullPath = BuildFullPath(relativePath);
        return await AuthenticatedPutJsonAsync(fullPath, content);
    }

    /// <summary>
    /// PUT 요청 후 타입 변환
    /// </summary>
    protected virtual async Task<TResponse> PutAsync<TRequest, TResponse>(TRequest content, string relativePath = "")
    {
        var response = await PutAsync(content, relativePath);
        await HttpClientHelper.EnsureSuccessStatusCodeAsync(response);
        return await HttpClientHelper.DeserializeResponseAsync<TResponse>(response);
    }

    /// <summary>
    /// DELETE 요청
    /// </summary>
    protected virtual async Task<HttpResponseMessage> DeleteAsync(string relativePath = "")
    {
        var fullPath = BuildFullPath(relativePath);
        return await AuthenticatedDeleteAsync(fullPath);
    }

    /// <summary>
    /// DELETE 요청 후 성공 확인
    /// </summary>
    protected virtual async Task DeleteWithSuccessCheckAsync(string relativePath = "")
    {
        var response = await DeleteAsync(relativePath);
        await HttpClientHelper.EnsureSuccessStatusCodeAsync(response);
    }

    #endregion

    #region 경로 빌더 메서드들

    /// <summary>
    /// 전체 경로 빌드
    /// </summary>
    protected virtual string BuildFullPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return BaseRoute;

        relativePath = relativePath.TrimStart('/');
        return $"{BaseRoute}/{relativePath}";
    }

    /// <summary>
    /// ID 기반 경로 빌드
    /// </summary>
    protected virtual string BuildPathWithId(object id)
    {
        return BuildFullPath(id.ToString());
    }

    /// <summary>
    /// 쿼리 파라미터와 함께 경로 빌드
    /// </summary>
    protected virtual string BuildPathWithQuery(string relativePath, object queryParameters)
    {
        var fullPath = BuildFullPath(relativePath);
        var queryString = BuildQueryString(queryParameters);
        
        return string.IsNullOrEmpty(queryString) 
            ? fullPath 
            : $"{fullPath}?{queryString}";
    }

    /// <summary>
    /// 쿼리 스트링 빌드 (익명 객체에서)
    /// </summary>
    protected virtual string BuildQueryString(object parameters)
    {
        if (parameters == null) return string.Empty;

        var properties = parameters.GetType().GetProperties()
            .Where(p => p.GetValue(parameters) != null)
            .Select(p => $"{p.Name}={Uri.EscapeDataString(p.GetValue(parameters).ToString())}")
            .ToArray();

        return string.Join("&", properties);
    }

    #endregion

    #region 응답 검증 헬퍼들

    /// <summary>
    /// 성공 응답 검증
    /// </summary>
    protected virtual async Task AssertSuccessResponseAsync(HttpResponseMessage response)
    {
        var content = await HttpClientHelper.ReadResponseContentAsync(response);
        
        Assert.True(response.IsSuccessStatusCode, 
            $"응답이 성공 상태가 아님: {response.StatusCode} {response.ReasonPhrase}\n응답 내용: {content}");
    }

    /// <summary>
    /// 특정 상태 코드 검증
    /// </summary>
    protected virtual async Task AssertStatusCodeAsync(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        var content = await HttpClientHelper.ReadResponseContentAsync(response);
        
        Assert.Equal(expectedStatusCode, response.StatusCode);
        
        Logger.LogDebug("상태 코드 검증 성공: {StatusCode}", expectedStatusCode);
    }

    /// <summary>
    /// 에러 응답 검증
    /// </summary>
    protected virtual async Task AssertErrorResponseAsync(HttpResponseMessage response, HttpStatusCode expectedStatusCode, string expectedErrorMessage = null)
    {
        await AssertStatusCodeAsync(response, expectedStatusCode);

        if (!string.IsNullOrEmpty(expectedErrorMessage))
        {
            var content = await HttpClientHelper.ReadResponseContentAsync(response);
            Assert.Contains(expectedErrorMessage, content);
        }
    }

    /// <summary>
    /// 인증 실패 응답 검증
    /// </summary>
    protected virtual async Task AssertUnauthorizedAsync(HttpResponseMessage response)
    {
        await AssertStatusCodeAsync(response, HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// 권한 없음 응답 검증
    /// </summary>
    protected virtual async Task AssertForbiddenAsync(HttpResponseMessage response)
    {
        await AssertStatusCodeAsync(response, HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// 찾을 수 없음 응답 검증
    /// </summary>
    protected virtual async Task AssertNotFoundAsync(HttpResponseMessage response)
    {
        await AssertStatusCodeAsync(response, HttpStatusCode.NotFound);
    }

    /// <summary>
    /// 잘못된 요청 응답 검증
    /// </summary>
    protected virtual async Task AssertBadRequestAsync(HttpResponseMessage response, string expectedErrorMessage = null)
    {
        await AssertErrorResponseAsync(response, HttpStatusCode.BadRequest, expectedErrorMessage);
    }

    #endregion

    #region 컨트롤러별 특화 기능

    /// <summary>
    /// 컨트롤러 인스턴스 가져오기 (DI 컨테이너에서)
    /// </summary>
    protected virtual TController GetControllerInstance()
    {
        return GetService<TController>();
    }

    /// <summary>
    /// 컨트롤러 액션 메서드 정보 가져오기
    /// </summary>
    protected virtual MethodInfo GetActionMethod(string actionName)
    {
        var method = ControllerType.GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);
        
        if (method == null)
            throw new ArgumentException($"액션 메서드 '{actionName}'을 찾을 수 없습니다.", nameof(actionName));

        return method;
    }

    /// <summary>
    /// 컨트롤러의 모든 액션 메서드 가져오기
    /// </summary>
    protected virtual IEnumerable<MethodInfo> GetAllActionMethods()
    {
        return ControllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType == ControllerType)
            .Where(m => m.IsPublic && !m.IsSpecialName);
    }

    /// <summary>
    /// 특정 HTTP 메서드의 액션들 가져오기
    /// </summary>
    protected virtual IEnumerable<MethodInfo> GetActionsByHttpMethod(string httpMethod)
    {
        return GetAllActionMethods()
            .Where(m => HasHttpMethodAttribute(m, httpMethod));
    }

    /// <summary>
    /// 메서드가 특정 HTTP 메서드 어트리뷰트를 가지고 있는지 확인
    /// </summary>
    private static bool HasHttpMethodAttribute(MethodInfo method, string httpMethod)
    {
        return httpMethod.ToUpperInvariant() switch
        {
            "GET" => method.GetCustomAttribute<HttpGetAttribute>() != null,
            "POST" => method.GetCustomAttribute<HttpPostAttribute>() != null,
            "PUT" => method.GetCustomAttribute<HttpPutAttribute>() != null,
            "DELETE" => method.GetCustomAttribute<HttpDeleteAttribute>() != null,
            "PATCH" => method.GetCustomAttribute<HttpPatchAttribute>() != null,
            _ => false
        };
    }

    #endregion

    #region 테스트 시나리오 헬퍼들

    /// <summary>
    /// CRUD 시나리오 테스트 헬퍼
    /// </summary>
    protected virtual async Task RunCrudScenarioAsync<TEntity, TCreateDto, TUpdateDto>(
        TCreateDto createDto,
        TUpdateDto updateDto,
        Expression<Func<TEntity, bool>> verificationSelector = null)
        where TEntity : class
    {
        Logger.LogInformation("CRUD 시나리오 테스트 시작: {EntityType}", typeof(TEntity).Name);

        // Create
        Logger.LogDebug("CREATE 테스트");
        var createResponse = await PostAsync(createDto);
        await AssertSuccessResponseAsync(createResponse);
        var created = await HttpClientHelper.DeserializeResponseAsync<TEntity>(createResponse);

        // Read
        Logger.LogDebug("READ 테스트");
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty != null)
        {
            var id = idProperty.GetValue(created);
            var readResponse = await GetAsync(id.ToString());
            await AssertSuccessResponseAsync(readResponse);
        }

        // Update
        Logger.LogDebug("UPDATE 테스트");
        if (idProperty != null)
        {
            var id = idProperty.GetValue(created);
            var updateResponse = await PutAsync(updateDto, id.ToString());
            await AssertSuccessResponseAsync(updateResponse);
        }

        // Delete
        Logger.LogDebug("DELETE 테스트");
        if (idProperty != null)
        {
            var id = idProperty.GetValue(created);
            await DeleteWithSuccessCheckAsync(id.ToString());

            // 삭제 확인
            var deleteVerifyResponse = await GetAsync(id.ToString());
            await AssertNotFoundAsync(deleteVerifyResponse);
        }

        Logger.LogInformation("CRUD 시나리오 테스트 완료: {EntityType}", typeof(TEntity).Name);
    }

    /// <summary>
    /// 인증 시나리오 테스트 헬퍼
    /// </summary>
    protected virtual async Task RunAuthenticationScenarioAsync(string relativePath = "")
    {
        Logger.LogInformation("인증 시나리오 테스트 시작");

        // 인증된 요청은 성공
        Logger.LogDebug("인증된 요청 테스트");
        var authenticatedResponse = await GetAsync(relativePath);
        await AssertSuccessResponseAsync(authenticatedResponse);

        // 익명 요청은 실패
        Logger.LogDebug("익명 요청 테스트");
        await SwitchToAnonymousAsync();
        var anonymousResponse = await GetAsync(relativePath);
        await AssertUnauthorizedAsync(anonymousResponse);

        // 만료된 토큰 요청은 실패
        Logger.LogDebug("만료된 토큰 요청 테스트");
        await SwitchToExpiredTokenAsync();
        var expiredResponse = await GetAsync(relativePath);
        await AssertUnauthorizedAsync(expiredResponse);

        // 인증 복구
        await SetupDefaultAuthenticationAsync();

        Logger.LogInformation("인증 시나리오 테스트 완료");
    }

    /// <summary>
    /// 권한 시나리오 테스트 헬퍼
    /// </summary>
    protected virtual async Task RunAuthorizationScenarioAsync(string relativePath, string requiredRole)
    {
        Logger.LogInformation("권한 시나리오 테스트 시작: {Role}", requiredRole);

        // 올바른 역할로는 성공
        Logger.LogDebug("올바른 역할 요청 테스트: {Role}", requiredRole);
        await SwitchToRoleAsync(requiredRole);
        var authorizedResponse = await GetAsync(relativePath);
        await AssertSuccessResponseAsync(authorizedResponse);

        // 잘못된 역할로는 실패
        Logger.LogDebug("잘못된 역할 요청 테스트");
        await SwitchToRoleAsync("User"); // 일반 사용자로 변경
        var unauthorizedResponse = await GetAsync(relativePath);
        await AssertForbiddenAsync(unauthorizedResponse);

        Logger.LogInformation("권한 시나리오 테스트 완료");
    }

    #endregion

    #region 디버깅 헬퍼

    /// <summary>
    /// 컨트롤러 정보 출력 (디버깅용)
    /// </summary>
    protected virtual void LogControllerInfo()
    {
        Logger.LogDebug("=== 컨트롤러 정보 ===");
        Logger.LogDebug("컨트롤러 타입: {ControllerType}", ControllerType.Name);
        Logger.LogDebug("컨트롤러명: {ControllerName}", ControllerName);
        Logger.LogDebug("기본 라우트: {BaseRoute}", BaseRoute);
        
        var actions = GetAllActionMethods().Select(m => m.Name).ToArray();
        Logger.LogDebug("액션 메서드들: {Actions}", string.Join(", ", actions));
        
        Logger.LogDebug("===================");
    }

    /// <summary>
    /// 라우트 정보 출력 (디버깅용)
    /// </summary>
    protected virtual void LogRouteInfo(string relativePath = "")
    {
        var fullPath = BuildFullPath(relativePath);
        Logger.LogDebug("요청 경로: {FullPath} (기본: {BaseRoute}, 상대: {RelativePath})", 
            fullPath, BaseRoute, relativePath);
    }

    #endregion

    #region 테스트 환경 초기화 오버라이드

    /// <summary>
    /// 컨트롤러 테스트 환경 초기화
    /// </summary>
    protected override async Task OnTestEnvironmentInitializedAsync()
    {
        await base.OnTestEnvironmentInitializedAsync();
        
        Logger.LogInformation("컨트롤러 테스트 환경 초기화 완료: {ControllerName}", ControllerName);
        
        // 디버그 모드에서 컨트롤러 정보 출력
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            LogControllerInfo();
        }
    }

    #endregion
}