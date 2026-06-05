using System.Text.Json;
using System.Text.Json.Nodes;
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
                var fixedJson = FixPrivateKeyInJson(cleanedJson);
                credential = GoogleCredential.FromJson(fixedJson);
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

    /// <summary>
    /// AWS Elastic Beanstalk strips the \n escape sequences from environment variables,
    /// so the private_key field in the Firebase credentials JSON loses all its line breaks.
    /// This method parses the JSON, fixes the private_key PEM format, and reconstructs the JSON.
    /// </summary>
    public static string FixPrivateKeyInJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("private_key", out var pkElement))
                return json; // No private_key field, nothing to fix

            var pk = pkElement.GetString();
            if (pk == null) return json;

            // If the key already has proper newlines, it's fine
            if (pk.Contains('\n')) return json;

            const string beginMarker = "-----BEGIN PRIVATE KEY-----";
            const string endMarker = "-----END PRIVATE KEY-----";

            int beginIdx = pk.IndexOf(beginMarker, StringComparison.Ordinal);
            int endIdx = pk.IndexOf(endMarker, StringComparison.Ordinal);

            if (beginIdx < 0 || endIdx < 0)
                return json; // Markers not found, can't fix

            // Extract the base64 body between the PEM markers
            var body = pk.Substring(beginIdx + beginMarker.Length, endIdx - beginIdx - beginMarker.Length).Trim();

            // Reconstruct proper PEM format with newlines
            var fixedPk = beginMarker + "\n" + body + "\n" + endMarker + "\n";

            // Reconstruct the JSON with the fixed private_key
            var jsonNode = JsonNode.Parse(json);
            jsonNode!["private_key"] = fixedPk;
            return jsonNode.ToJsonString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FIREBASE_FIX] Failed to fix private key: {ex.Message}");
            return json; // If anything fails, return the original
        }
    }
}
