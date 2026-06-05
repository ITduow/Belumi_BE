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
                var escapedJson = EscapeJsonStringNewlines(cleanedJson);
                credential = GoogleCredential.FromJson(escapedJson);
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

    public static string EscapeJsonStringNewlines(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;

        var sb = new System.Text.StringBuilder(json.Length);
        bool inString = false;
        bool escaped = false;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '"' && !escaped)
            {
                inString = !inString;
                sb.Append(c);
            }
            else if (inString)
            {
                if (c == '\r')
                {
                    if (i + 1 < json.Length && json[i + 1] == '\n')
                    {
                        i++;
                    }
                    sb.Append("\\n");
                }
                else if (c == '\n')
                {
                    sb.Append("\\n");
                }
                else
                {
                    if (c == '\\')
                    {
                        escaped = !escaped;
                    }
                    else
                    {
                        escaped = false;
                    }
                    sb.Append(c);
                }
            }
            else
            {
                sb.Append(c);
                escaped = false;
            }
        }

        return sb.ToString();
    }
}
