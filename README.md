# AigoraNet

## 프로젝트 개요
- 프레임워크: .NET 10 (C# 13)
- UI/API: Razor Pages + WebApi (사용자 가입/인증, 토큰 발급)
- 아키텍처: CQRS + Wolverine 기반 메시징
- 데이터베이스/ORM: MS SQL Server + Entity Framework Core (Code-First)
- 로깅: Serilog(구조적 로깅)
- 핵심 도메인: MCP 서버 등록·토큰 발급, 요구사항 내 키워드 매칭 → 사전 정의 프롬프트 로딩
- 공통 라이브러리: `AigoraNet.Common` (엔터티, 구성, 유틸 공유)
- 핵심 프로젝트: `AigoraNet.McpServer` (MCP 프로토콜 처리, 키워드-프롬프트 매핑)

## 우선 작업 로드맵
1) **도메인/DB 모델링**
   - 사용자/토큰 엔터티, 키워드-프롬프트 매핑 엔터티 설계
   - Fluent API 구성 및 초기 마이그레이션 생성
2) **CQRS 설계(Wolverine)**
   - 명령: 회원가입, 토큰 발급/갱신/폐기, 키워드-프롬프트 등록/수정
   - 조회: 프롬프트 조회(키워드 매칭), 토큰 검증용 조회
   - Wolverine 핸들러/파이프라인 정의
3) **인증·토큰 검증**
   - WebApi/MCP 요청에 토큰 검증 미들웨어 적용
   - 토큰 상태(유효/만료/폐기) 조회 로직 연계
4) **비즈니스 로직**
   - 요구사항 문자열 → 키워드 매칭 → 프롬프트 조회 구현
   - 성능 고려: 컴파일된 Regex 또는 고성능 문자열 검색, 캐싱 전략 검토
5) **로깅·관찰성**
   - Serilog 구조적 로그에 UserId/TokenKey/MatchedKeyword 등 핵심 컨텍스트 포함
   - 주요 실패/분기 로깅 및 대시보드 지표 정의
6) **API/Razor Pages 설정**
   - Scalar UI, 파이프라인 구성
   - 예외 처리 필터/미들웨어 정리
7) **성능·캐싱 검토**
   - 프롬프트 조회 캐시(우선순위/TTL/무효화) 검토 및 PoC