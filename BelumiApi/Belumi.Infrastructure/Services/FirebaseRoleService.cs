using System.Text.Json;
using Belumi.Core.Entities;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;

namespace Belumi.Infrastructure.Services;

public sealed class FirebaseRoleService(IConfiguration configuration, FirebaseAdminAppFactory firebaseAdminAppFactory)
{
    private readonly Lazy<FirestoreDb> _firestore = new(() => CreateFirestoreDb(configuration));

    public async Task<UserRole?> GetRoleAsync(FirebaseToken token, CancellationToken cancellationToken = default)
    {
        firebaseAdminAppFactory.GetOrCreate();

        var snapshot = await _firestore.Value
            .Collection("users")
            .Document(token.Uid)
            .GetSnapshotAsync(cancellationToken);

        if (!snapshot.Exists ||
            !snapshot.TryGetValue<string>("role", out var roleValue) ||
            string.IsNullOrWhiteSpace(roleValue))
        {
            return null;
        }

        return roleValue.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase)
            ? UserRole.Admin
            : UserRole.Customer;
    }

    private static FirestoreDb CreateFirestoreDb(IConfiguration configuration)
    {
        GoogleCredential credential;
        string? projectId = null;

        var firebaseCredentialsJson = configuration["FIREBASE_CREDENTIALS"];
        if (!string.IsNullOrWhiteSpace(firebaseCredentialsJson))
        {
            var cleanedJson = firebaseCredentialsJson.Trim('\'', '"');
            credential = GoogleCredential.FromJson(cleanedJson);
            
            using var document = JsonDocument.Parse(cleanedJson);
            if (document.RootElement.TryGetProperty("project_id", out var projIdElement))
            {
                projectId = projIdElement.GetString();
            }
        }
        else
        {
            var credentialPath = configuration["Firebase:ServiceAccountPath"]
                ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

            if (string.IsNullOrWhiteSpace(credentialPath))
            {
                throw new InvalidOperationException(
                    "Firebase service account is not configured. Set GOOGLE_APPLICATION_CREDENTIALS, FIREBASE_CREDENTIALS or Firebase:ServiceAccountPath.");
            }

            if (!File.Exists(credentialPath))
            {
                throw new InvalidOperationException($"Firebase service account file was not found: {credentialPath}");
            }

            credential = GoogleCredential.FromFile(credentialPath);
            
            using var stream = File.OpenRead(credentialPath);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.TryGetProperty("project_id", out var projIdElement))
            {
                projectId = projIdElement.GetString();
            }
        }

        projectId = configuration["Firebase:ProjectId"] ?? projectId;
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new InvalidOperationException("Firebase project_id was not found in service account config.");
        }

        return new FirestoreDbBuilder
        {
            ProjectId = projectId,
            Credential = credential
        }.Build();
    }

    private static string? ReadProjectId(string credentialPath)
    {
        using var stream = File.OpenRead(credentialPath);
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.TryGetProperty("project_id", out var projectId)
            ? projectId.GetString()
            : null;
    }
}
