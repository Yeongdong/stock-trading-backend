# SonarQube 설치 및 설정 스크립트

echo "🚀 SonarQube 설치를 시작합니다..."

# 색상 설정
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# 1. Docker가 설치되어 있는지 확인
echo -e "${YELLOW}Docker 설치 확인 중...${NC}"
if command -v docker >/dev/null 2>&1; then
    echo -e "${GREEN}✅ Docker가 설치되어 있습니다.${NC}"
else
    echo -e "${RED}❌ Docker가 설치되지 않았습니다.${NC}"
    echo -e "${BLUE}Docker Desktop for Mac 다운로드: https://www.docker.com/products/docker-desktop${NC}"
    exit 1
fi

# 2. Docker가 실행 중인지 확인
if ! docker info >/dev/null 2>&1; then
    echo -e "${RED}❌ Docker가 실행되지 않고 있습니다. Docker Desktop을 시작해주세요.${NC}"
    exit 1
fi

# 3. 기존 SonarQube 컨테이너 정리 (있다면)
echo -e "${YELLOW}기존 SonarQube 컨테이너 확인 중...${NC}"
if docker ps -a | grep -q sonarqube; then
    echo -e "${YELLOW}기존 SonarQube 컨테이너를 정리합니다...${NC}"
    docker compose -f docker-compose.sonar.yml down
fi

# 4. SonarQube 컨테이너 실행
echo -e "${YELLOW}SonarQube 컨테이너 시작 중...${NC}"
docker compose -f docker-compose.sonar.yml up -d

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ SonarQube 컨테이너 시작에 실패했습니다.${NC}"
    exit 1
fi

# 5. SonarQube가 시작될 때까지 대기
echo -e "${YELLOW}SonarQube 시작 대기 중... (약 2-3분 소요)${NC}"
max_attempts=30
attempt=0

while [ $attempt -lt $max_attempts ]; do
    sleep 10
    attempt=$((attempt + 1))
    echo -e "${YELLOW}시도 $attempt/$max_attempts ...${NC}"
    
    if curl -s "http://localhost:9000/api/system/status" | grep -q '"status":"UP"'; then
        echo -e "${GREEN}✅ SonarQube가 성공적으로 시작되었습니다!${NC}"
        break
    fi
    
    if [ $attempt -eq $max_attempts ]; then
        echo -e "${RED}❌ SonarQube 시작에 시간이 너무 오래 걸립니다. 로그를 확인해주세요.${NC}"
        echo -e "${BLUE}로그 확인 명령어: docker-compose -f docker-compose.sonar.yml logs sonarqube${NC}"
        exit 1
    fi
done

# 6. 초기 설정 안내
echo ""
echo -e "${GREEN}🎉 SonarQube 설치가 완료되었습니다!${NC}"
echo ""
echo -e "${CYAN}=== 접속 정보 ===${NC}"
echo -e "${NC}URL: http://localhost:9000${NC}"
echo -e "${NC}초기 로그인:${NC}"
echo -e "${NC}  아이디: admin${NC}"
echo -e "${NC}  비밀번호: admin${NC}"
echo ""
echo -e "${CYAN}=== 다음 단계 ===${NC}"
echo -e "${NC}1. 브라우저에서 http://localhost:9000 접속${NC}"
echo -e "${NC}2. admin/admin으로 로그인${NC}"
echo -e "${NC}3. 새 비밀번호 설정${NC}"
echo -e "${NC}4. 프로젝트 생성 및 토큰 발급${NC}"
echo ""

# 7. 브라우저 자동 열기
read -p "브라우저를 자동으로 열까요? (Y/n): " open_browser
if [[ $open_browser == "" || $open_browser == "Y" || $open_browser == "y" ]]; then
    open "http://localhost:9000"
fi

echo -e "${GREEN}설치가 완료되었습니다. 다음 단계로 프로젝트 설정을 진행해주세요.${NC}"