# Belumi local run

## Backend

1. Install PostgreSQL and create database `belumi_db`.
2. Configure connection string with user-secrets or environment variable.

```powershell
cd BelumiApi/Belumi.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=belumi_db;Username=postgres;Password=12345"
dotnet user-secrets set "Gemini:ApiKey" "<your-key>"
```

3. Run API.

```powershell
dotnet run --project BelumiApi/Belumi.API/Belumi.API.csproj --urls https://localhost:7084
```

Swagger: `https://localhost:7084/swagger/index.html`

Admin demo:

- Email: `admin@belumi.com`
- Password: `belumi2026`

## Flutter

```powershell
cd belumi_app
flutter pub get
flutter run -d chrome --dart-define=BELUMI_API_BASE_URL=https://localhost:7084/api
```

For Android emulator, the app automatically maps `localhost` to `10.0.2.2`.

## Main API groups

- `POST /api/auth/firebase-login`
- `POST /api/admin/auth/login`
- `GET /api/admin/dashboard`
- `POST /api/skincare/analyze`
- `POST /api/ingredients/analyze-text`
- `POST /api/ingredients/analyze-image`
- `POST /api/makeup/consult`
- `GET /api/news`
- `GET /api/products`
- `GET /api/subscription/plans`
- `POST /api/payment/mock-checkout`
- `GET /api/ai-usage/{userId}`
