using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;

namespace Belumi.Infrastructure.Services;

public sealed class FirebaseAdminAppFactory(IConfiguration configuration)
{
    private static readonly object Gate = new();

    public FirebaseApp GetOrCreate()
    {
        if (FirebaseApp.DefaultInstance is not null)
        {
            return FirebaseApp.DefaultInstance;
        }

        lock (Gate)
        {
            if (FirebaseApp.DefaultInstance is not null)
            {
                return FirebaseApp.DefaultInstance;
            }

            GoogleCredential credential;
            var firebaseCredentialsJson = configuration["FIREBASE_CREDENTIALS"];

            if (!string.IsNullOrWhiteSpace(firebaseCredentialsJson))
            {
                var cleanedJson = firebaseCredentialsJson.Trim('\'', '"');
                credential = GoogleCredential.FromJson(cleanedJson);
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
            }

            return FirebaseApp.Create(new AppOptions
            {
                Credential = credential
            });
        }
    }
}
