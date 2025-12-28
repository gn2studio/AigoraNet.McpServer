# AigoraNet

## 프로젝트 소개
- **목적**: 발급된 토큰 키로 MCP(Model Context Protocol) 서버를 에이전트 서비스에 등록하고, 요구사항 내 키워드에 따라 사전 정의된 프롬프트를 DB에서 조회·적용하는 미들웨어.
- **주요 구성요소**:
  - `AigoraNet.McpServer`: MCP 프로토콜 구현, 키워드→프롬프트 매핑 엔진, 캐시.
  - `AigoraNet.WebApi`: 사용자 등록/인증, 토큰 발급·관리, 관리/공개 API.
  - `AigoraNet.Common`: 공통 엔터티, CQRS 핸들러, 설정, 유틸.
- **기술스택**: .NET 10 (C# 13), Razor Pages + WebApi, CQRS + Wolverine, EF Core(Code-First) + MS SQL, Serilog, Azure Blob(파일), Redis(캐시), MCP.

## 폴더 구조(요약)
- `AigoraNet.Common/` 공통 엔터티, CQRS 핸들러, 설정
- `AigoraNet.WebApi/` 인증·토큰·관리/공개/개인 API, Razor Pages, 미들웨어
- `AigoraNet.McpServer/` MCP 서버 패키지 소스
- `AigoraNet.WebApi.Tests/` WebApi 단위/통합 테스트

## 실행/빌드/테스트
```bash
# 전체 빌드
Dotnet build

# WebApi 테스트 실행
Dotnet test AigoraNet.WebApi.Tests/AigoraNet.WebApi.Tests.csproj

# MCP 서버 로컬 실행 예시
Dotnet run --project AigoraNet.McpServer
```

## API 엔드포인트 프리픽스 및 권한 원칙
- `auth/*` : 인증/토큰/회원가입(공개 등록 포함)
- `public/*` : 로그인 없이 열람 가능한 공개 리소스 (예: 공지/보드 조회, 프롬프트 매칭)
- `private/*` : 로그인 사용자 전용 (본인 정보, 파일, 댓글 등)
- `system/*` : 관리자 전용 (회원/프롬프트/로그 등)
- 각 액션에 `[Authorize(Roles = ...)]` 혹은 `[AllowAnonymous]`로 세분화

## 코딩 컨벤션 (요약)
- **CQRS + Wolverine**: Command/Query 분리, 컨벤션 핸들러 사용.
- **검증**: FluentValidation 미사용, 수동 검증으로 공백/필수값 체크 후 조기 반환.
- **EF Core**: `AsNoTracking()` 조회, `Condition.IsEnabled && Status==Active` 필터, `WhereIf` 옵션 필터, `Max` 사용 시 존재 검증.
- **감사/상태**: 생성 시 `Condition = new AuditableEntity { CreatedBy = userId, RegistDate = UtcNow }`, 소프트 삭제/비활성화 준수.
- **매핑/정규화**: DTO→엔터티 매핑, 콘텐츠 URL 디코딩/정규화 후 저장, FK는 검증 후 설정.
- **캐시**: `IPromptCache` 기반, 핸들러는 WebApi 의존성 제거, TTL 적용.
- **로깅**: Serilog 구조화 로그, UserId/TokenKey/MatchedKeyword 등 컨텍스트 포함.
- **DI**: 생성자/primary ctor, DbContext는 scoped, 루트 서비스 해제 금지.
- **파일**: FileMaster는 로그성(Insert/비활성화), Azure Blob 업로드/삭제 연계.
- **파일 배치**: Command/Query/Handler를 한 파일에 모아 관리.

## 기여 방법
1. 포크 또는 브랜치 생성 후 작업.
2. 변경 시 관련 테스트 추가/수정, `dotnet test` 통과 확인.
3. PR 템플릿에 목적/변경사항/테스트 결과 기재.
4. 코딩 컨벤션 및 엔드포인트 권한 규칙 준수.

## 문의
- 버그/요구사항: 이슈 등록
- 보안/토큰 관련: 담당 관리자에게 직접 문의