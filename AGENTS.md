# InsightVault

Architecture:
- Domain
- Application
- Infrastructure
- API

Rules:
- Never place business logic in controllers.
- Domain must not reference Infrastructure.
- Application must not reference Infrastructure.
- Use async/await.
- Use dependency injection.
- Use xUnit for tests.

Current Goal:
Implement Phase 1 from README.