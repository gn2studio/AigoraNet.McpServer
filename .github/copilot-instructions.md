# AI Coding Instructions for AigoraNet Solution

You are an expert .NET developer and architect. You are helping to build and maintain the **AigoraNet** solution. Follow these instructions strictly to ensure consistency with the project's architecture, tech stack, and business logic.

---

## 1. Project Overview & Core Logic
* **Project Name:** AigoraNet
* **Core Goal:** A service where users register an MCP (Model Context Protocol) server to their agent services using an issued token key.
* **Core Business Logic:** When a user's requirement contains specific **predefined keywords**, the system must retrieve and load the corresponding **predefined prompts** from the database.
* **Target Framework:** .NET 10 (C# 13+)

## 2. Solution Structure
* **AigoraNet.McpServer:** The core project. Implements MCP protocols and the prompt-loading engine based on keyword matching.
* **AigoraNet.WebApi:** Handles user registration, authentication, and token key issuance/management.
* **AigoraNet.Common:** Shared library containing common entities, DTOs, constants, and shared utilities used across the solution.

## 3. Tech Stack & Architecture
* **Architecture Pattern:** CQRS (Command Query Responsibility Segregation).
* **Messaging & Mediator:** **Wolverine** (for command bus, message handling, and decoupled processing).
* **Database:** MS SQL Server.
* **ORM:** Entity Framework Core (EF Core).
* **Logging:** Serilog (Structured logging).
* **Communication:** Model Context Protocol (MCP).

## 4. Coding Standards & Implementation Rules

### 4.1. CQRS with Wolverine
* Divide logic into **Commands** (state changes) and **Queries** (data retrieval).
* Use Wolverine's convention-based handlers (e.g., `public static void Handle(Command message)` or `public class CommandHandler`).
* Avoid traditional MediatR boilerplate; leverage Wolverine's simplified programming model and "outbox" pattern where necessary.

### 4.2. Entity Framework Core & MS SQL
* Use a Code-First approach.
* Keep domain entities clean. Use Fluent API for configurations in the `DbContext`.
* Leverage .NET 10 performance features (e.g., optimized LINQ queries, `asNoTracking` for queries).
* All DB-related projects should reference `AigoraNet.Common` for base classes or shared interfaces.

### 4.3. .NET 10 & C# 13 Features
* Use **Primary Constructors** for dependency injection.
* Use **Required Members** and **Raw String Literals** for cleaner DTOs and prompt templates.
* Apply the latest collection expressions and performance improvements.

### 4.4. Security & Business Logic
* Every request to the McpServer must validate the user's **Token Key**.
* The keyword matching logic should be efficient. Consider using compiled Regex or high-performance string searching when analyzing user requirements.
* Prompts are stored in MS SQL and should be cached if performance becomes a bottleneck.

### 4.5. Logging with Serilog
* Use structured logging.
* Always include relevant context (e.g., `UserId`, `TokenKey`, `MatchedKeyword`) in log messages.
* Log all critical path failures and business logic transitions.

### 4.6. Additional Coding Conventions (align with prior MediatR style applied to Wolverine)
* **Validation:** Perform manual validation (no FluentValidation dependency); reject empty/whitespace inputs before DB writes; surface clear error messages.
* **EF Query Patterns:** Prefer `AsNoTracking()` for reads; always filter `Condition.IsEnabled` and `Condition.Status == Active`; use `WhereIf` for optional filters; guard `Max`/`First` with existence checks.
* **Auditing:** On create, set `Condition = new AuditableEntity { CreatedBy = userId, RegistDate = DateTime.UtcNow }`; honor soft-delete/status fields.
* **Mapping/Text Normalization:** Map DTO ¡æ entity via mapper when available; normalize/URL-decode content before saving; set foreign keys only after validation.
* **Caching:** Use `IPromptCache` abstraction in shared handlers; keep handlers free from WebApi-specific dependencies; apply sliding TTLs for hit/miss caching.
* **DI Scope:** Use constructor/primary-constructor injection; resolve scoped services (e.g., `DefaultContext`) from the request scope, never from the root provider.
* **Logging:** Serilog structured logs with context placeholders; log exceptions with context; enqueue background jobs if the pattern exists.
* **File Organization:** For each command/query, keep related types (request/response/validator/handler) in a single file to avoid file sprawl and to keep related business logic visible together.

## 5. Instructions for AI Assistant
* **When generating code:** Ensure it fits within the CQRS structure using Wolverine.
* **When creating API endpoints:** Place them in `AigoraNet.WebApi` and ensure they follow RESTful conventions.
* **When modifying McpServer:** Focus on the keyword-to-prompt mapping logic and MCP protocol compliance.
* **Naming Convention:** Use PascalCase for classes/methods and _camelCase for private fields.
* **Async/Await:** Always use asynchronous programming for I/O bound operations (DB, Network).

---
*Reference: This project is a high-performance, AI-integrated middleware solution for modern agentic workflows.*