# Stock Trading Backend

한국투자증권 API를 활용한, Clean Architecture 패턴으로 구현된 주식 트레이딩 애플리케이션의 백엔드 프로젝트입니다. 실시간 주가 데이터 처리, 주식 주문, 계좌 잔고 조회 등의 기능을 제공합니다.

## 기술 스택

- **프레임워크**: ASP.NET Core 8.0
- **언어**: C# 12
- **아키텍처**: Clean Architecture
- **데이터베이스**: PostgreSQL 16
- **ORM**: Entity Framework Core 9.0
- **인증**: JWT, Google OAuth 2.0
- **실시간 통신**: SignalR
- **API 문서화**: Swagger / OpenAPI
- **테스트**: xUnit, Moq, FluentAssertions
- **로깅**: Serilog

## 프로젝트 구조

```
StockTrading/
├── StockTrading.Domain/               # 도메인 레이어
│   ├── Entities/                      # 비즈니스 엔티티
│   ├── Enums/                         # 열거형
│   ├── Exceptions/                    # 도메인 예외
│   └── Settings/                      # 설정 클래스
│
├── StockTrading.Application/          # 애플리케이션 레이어
│   ├── DTOs/                          # 데이터 전송 객체
│   ├── Repositories/                  # 레포지토리 인터페이스
│   └── Services/                      # 서비스 인터페이스
│
├── StockTrading.Infrastructure/       # 인프라스트럭처 레이어
│   ├── ExternalServices/              # 외부 서비스 통합
│   │   └── KoreaInvestment/           # 한국투자증권 API 연동
│   ├── Implementations/               # 서비스 구현체
│   ├── Interfaces/                    # 인프라스트럭처 인터페이스
│   ├── Migrations/                    # EF Core 마이그레이션
│   └── Repositories/                  # 레포지토리 구현체
│
├── StockTrading.API/                  # API 레이어
│   ├── Controllers/                   # API 컨트롤러
│   ├── DTOs/                          # API 데이터 전송 객체
│   ├── Middleware/                    # 커스텀 미들웨어
│   ├── Validator/                     # 입력값 검증
│   └── Program.cs                     # 애플리케이션 엔트리 포인트
│
└── StockTrading.Tests/                # 테스트 프로젝트
    ├── Unit/                          # 단위 테스트
    └── Integration/                   # 통합 테스트
```

## 아키텍처 개요

이 프로젝트는 다음과 같은 레이어를 포함하는 Clean Architecture 패턴을 따릅니다:

- **Domain Layer**: 비즈니스 엔티티, 예외, 인터페이스 정의
- **Application Layer**: 유스케이스, 비즈니스 로직, 서비스 인터페이스
- **Infrastructure Layer**: 데이터베이스 접근, 외부 API 통합(한국투자증권), 인증 등 구현
- **API Layer**: 컨트롤러, 미들웨어, 모델 바인딩, 실시간 허브

## 설치 및 실행 방법

### 사전 요구사항

- .NET 8.0 SDK
- Visual Studio 2022 또는 JetBrains Rider 2024.1 이상
- PostgreSQL 16
- Docker (선택 사항)

### 로컬 개발 환경 설정

#### 데이터베이스 설정

1. PostgreSQL 서버에 새 데이터베이스 생성:
```sql
CREATE DATABASE stock_trading;
