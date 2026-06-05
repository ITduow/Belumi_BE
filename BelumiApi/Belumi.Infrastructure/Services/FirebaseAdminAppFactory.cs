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
                return json;

            var pk = pkElement.GetString();
            if (pk == null) return json;

            // If the key already has proper newlines, it's fine
            if (pk.Contains('\n')) return json;

            const string beginMarker = "-----BEGIN PRIVATE KEY-----";
            const string endMarker = "-----END PRIVATE KEY-----";

            int beginIdx = pk.IndexOf(beginMarker, StringComparison.Ordinal);
            int endIdx = pk.IndexOf(endMarker, StringComparison.Ordinal);

            if (beginIdx < 0 || endIdx < 0)
                return json;

            // Extract the body between markers
            var body = pk.Substring(beginIdx + beginMarker.Length, endIdx - beginIdx - beginMarker.Length);

            // AWS EB strips backslash from \n, leaving stray 'n' chars:
            //   -----BEGIN PRIVATE KEY-----nMIIEvg...64chars...nMIIE...n-----END PRIVATE KEY-----n
            // Strip leading 'n' (was \n after BEGIN marker)
            if (body.StartsWith("n")) body = body.Substring(1);
            // Strip trailing 'n' (was \n before END marker)  
            if (body.EndsWith("n")) body = body.Substring(0, body.Length - 1);

            // Now body = <64 base64>n<64 base64>n...<last line>
            // The stray 'n' appears every 64 chars of base64.
            // Remove them by processing in 65-char chunks (64 base64 + 1 stray n)
            var cleanBase64 = new System.Text.StringBuilder(body.Length);
            int linePos = 0;
            for (int i = 0; i < body.Length; i++)
            {
                if (linePos == 64 && body[i] == 'n')
                {
                    // This 'n' is a stray separator — skip it
                    linePos = 0;
                    continue;
                }
                cleanBase64.Append(body[i]);
                linePos++;
            }

            // Reconstruct proper PEM format
            var fixedPk = beginMarker + "\n" + cleanBase64 + "\n" + endMarker + "\n";

            // Reconstruct the JSON with the fixed private_key
            var jsonNode = JsonNode.Parse(json);
            jsonNode!["private_key"] = fixedPk;
            return jsonNode.ToJsonString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FIREBASE_FIX] Failed to fix private key: {ex.Message}");
            return json;
        }
    }
}
