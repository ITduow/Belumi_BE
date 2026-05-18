# Belumi API Clean Architecture

## Structure

```text
BelumiApi/
├── Belumi.API/              # Presentation layer: controllers, auth/cors/swagger, HTTP concerns only
├── Belumi.Application/      # Application layer: use-case contracts and app service interfaces
├── Belumi.Core/             # Domain layer: entities, enums, domain DTO contracts shared by use cases
├── Belumi.Infrastructure/   # Infrastructure layer: EF Core, PostgreSQL, external/mock AI/payment implementations
└── Belumi.Tests/            # Unit tests
```

## Dependency Rule

```text
API -> Application -> Core
API -> Infrastructure
Infrastructure -> Application -> Core
```

`Belumi.API` injects application interfaces such as `ICatalogService`, `IAiBeautyService`, `IContentService`, `IPaymentService`, and `IUserInteractionService`.
`Belumi.Infrastructure` implements those interfaces using `BelumiDbContext`, PostgreSQL, mock Gemini/OCR logic, and VietQR URL generation.

Controllers should not contain EF queries or business logic. Add new behavior by:

1. Defining an interface/use case in `Belumi.Application/Abstractions`.
2. Implementing it in `Belumi.Infrastructure/Services`.
3. Registering it in `Belumi.Infrastructure/DependencyInjection.cs`.
4. Calling the interface from an API controller.
