# SonarQube 코드 분석 실행 스크립트

# ⚠️ 사용법: ./run-sonar-analysis.sh YOUR_SONAR_TOKEN
# 토큰은 SonarQube 웹에서 생성 후 첫 번째 인자로 전달

# 기본값 설정
SONAR_TOKEN="$1"
SONAR_URL="http://localhost:9000"
PROJECT_KEY="StockTrading-Backend"

# 색상 설정
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${GREEN}🔍 SonarQube 코드 분석을 시작합니다...${NC}"

# 1. .NET SonarScanner 글로벌 도구 설치 확인
echo -e "${YELLOW}SonarScanner 설치 확인 중...${NC}"
if command -v dotnet sonarscanner >/dev/null 2>&1; then
    echo -e "${GREEN}✅ SonarScanner가 이미 설치되어 있습니다.${NC}"
else
    echo -e "${YELLOW}SonarScanner 설치 중...${NC}"
    dotnet tool install --global dotnet-sonarscanner
    
    if [ $? -ne 0 ]; then
        echo -e "${RED}❌ SonarScanner 설치에 실패했습니다.${NC}"
        exit 1
    fi
    echo -e "${GREEN}✅ SonarScanner 설치 완료${NC}"
fi

# 2. 토큰 확인
if [ -z "$SONAR_TOKEN" ]; then
    echo ""
    echo -e "${YELLOW}⚠️ SonarQube 토큰이 필요합니다!${NC}"
    echo -e "${CYAN}토큰 생성 방법:${NC}"
    echo -e "${NC}1. http://localhost:9000 접속${NC}"
    echo -e "${NC}2. 우상단 계정 → My Account → Security${NC}"
    echo -e "${NC}3. 'Generate Tokens' 섹션에서 새 토큰 생성${NC}"
    echo -e "${NC}4. 생성된 토큰을 복사${NC}"
    echo ""
    read -p "SonarQube 토큰을 입력하세요: " SONAR_TOKEN
    
    if [ -z "$SONAR_TOKEN" ]; then
        echo -e "${RED}❌ 토큰이 입력되지 않았습니다.${NC}"
        exit 1
    fi
fi

# 3. 기존 빌드 및 커버리지 결과 정리
echo -e "${YELLOW}기존 결과 정리 중...${NC}"
if [ -d "coverage" ]; then
    rm -rf coverage
fi

# 4. SonarQube 분석 시작
echo -e "${YELLOW}SonarQube 분석 시작...${NC}"
dotnet sonarscanner begin \
    /k:"$PROJECT_KEY" \
    /d:sonar.host.url="$SONAR_URL" \
    /d:sonar.token="$SONAR_TOKEN" \
    /d:sonar.cs.opencover.reportsPaths="coverage/**/coverage.opencover.xml" \
    /d:sonar.exclusions="**/bin/**,**/obj/**,**/Migrations/**" \
    /d:sonar.test.exclusions="**/*Test.cs,**/*Tests.cs"

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ SonarQube 분석 시작에 실패했습니다.${NC}"
    echo -e "${YELLOW}가능한 원인:${NC}"
    echo -e "${NC}1. 토큰이 유효하지 않음${NC}"
    echo -e "${NC}2. SonarQube 서버가 실행되지 않음${NC}"
    echo -e "${NC}3. 프로젝트 키가 이미 존재함${NC}"
    exit 1
fi

# 5. NuGet 패키지 복원
echo -e "${YELLOW}NuGet 패키지 복원 중...${NC}"
dotnet restore

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ NuGet 패키지 복원에 실패했습니다.${NC}"
    exit 1
fi

# 6. 프로젝트 빌드
echo -e "${YELLOW}프로젝트 빌드 중...${NC}"
dotnet build --configuration Release --no-restore

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ 프로젝트 빌드에 실패했습니다.${NC}"
    exit 1
fi

# 7. 테스트 실행 및 커버리지 수집
echo -e "${YELLOW}테스트 실행 및 커버리지 수집 중...${NC}"
dotnet test StockTrading.Tests \
    --configuration Release \
    --no-build \
    --collect:"XPlat Code Coverage" \
    --results-directory:"./coverage" \
    --logger:"console;verbosity=minimal" \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=opencover \
    /p:CoverletOutput="./coverage/" \
    /p:Exclude="[*.Tests]*,[*]*.Migrations.*"

if [ $? -ne 0 ]; then
    echo -e "${YELLOW}⚠️ 일부 테스트가 실패했지만 분석을 계속 진행합니다...${NC}"
fi

# 8. SonarQube 분석 완료
echo -e "${YELLOW}SonarQube 분석 완료 중...${NC}"
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ SonarQube 분석 완료에 실패했습니다.${NC}"
    exit 1
fi

# 9. 결과 안내
echo ""
echo -e "${GREEN}🎉 SonarQube 분석이 완료되었습니다!${NC}"
echo ""
echo -e "${CYAN}=== 결과 확인 ===${NC}"
echo -e "${NC}SonarQube 대시보드: $SONAR_URL/dashboard?id=$PROJECT_KEY${NC}"
echo ""

# 10. 대시보드 자동 열기
read -p "SonarQube 대시보드를 열까요? (Y/n): " open_dashboard
if [[ $open_dashboard == "" || $open_dashboard == "Y" || $open_dashboard == "y" ]]; then
    open "$SONAR_URL/dashboard?id=$PROJECT_KEY"
fi

echo -e "${GREEN}분석이 완료되었습니다. 대시보드에서 코드 품질 현황을 확인해보세요!${NC}"