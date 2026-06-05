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

            // === DIAGNOSTIC: log the parsed private_key value ===
            Console.WriteLine($"[PK_DEBUG] private_key length: {pk.Length}");
            Console.WriteLine($"[PK_DEBUG] first 80 chars: {pk[..Math.Min(80, pk.Length)]}");
            Console.WriteLine($"[PK_DEBUG] last 80 chars: {pk[Math.Max(0, pk.Length - 80)..]}");
            Console.WriteLine($"[PK_DEBUG] contains real newline (\\n): {pk.Contains('\n')}");
            Console.WriteLine($"[PK_DEBUG] contains literal backslash-n: {pk.Contains("\\n")}");
            Console.WriteLine($"[PK_DEBUG] starts with BEGIN marker: {pk.StartsWith("-----BEGIN")}");
            Console.WriteLine($"[PK_DEBUG] ends with END marker: {pk.TrimEnd().EndsWith("-----END PRIVATE KEY-----")}");
            // === END DIAGNOSTIC ===

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

            Console.WriteLine($"[PK_DEBUG] FIXED private_key length: {fixedPk.Length}");
            Console.WriteLine($"[PK_DEBUG] FIXED first 80: {fixedPk[..Math.Min(80, fixedPk.Length)]}");

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
