using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RoomPilot
{
    public sealed class AlexaDiagnostic
    {
        public string Version { get; set; }
        public string Step { get; set; }
        public int HttpStatus { get; set; }
        public string ErrorCode { get; set; }
        public DateTime TimeUtc { get; set; }

        public static string StepFor(string path)
        {
            if (path == "/auth/register")
                return "register";
            if (path == "/ap/exchangetoken/cookies")
                return "session";
            if (path == "/nexus/v1/graphql")
                return "devices";
            if (path.StartsWith("/api/phoenix/state", StringComparison.Ordinal))
                return "power";
            return "csrf";
        }

        public static string Label(string step)
        {
            return step == "register" ? L10n.Text("step.register") : step == "session" ? L10n.Text("step.session") : step == "devices" ? L10n.Text("step.devices") : step == "power" ? L10n.Text("step.power") : L10n.Text("step.csrf");
        }

        // Only fixed, recognized error identifiers leave the response parser. No raw messages,
        // headers, request IDs, URLs with queries, cookie values or auth codes are recorded.
        static readonly HashSet<string> Codes = new HashSet<string>(new[] { "InvalidToken", "InvalidValue", "InvalidRequest", "InvalidParameterValue", "MissingParameter", "InvalidClient", "InvalidScope", "InvalidGrant", "InvalidDomain", "UnrecognizedDeviceType", "RegistrationError", "InvalidCookie", "InvalidCookies", "InvalidCode", "InvalidCodeVerifier", "InvalidClientId", "invalid_request", "invalid_grant", "invalid_client", "invalid_scope", "invalid_token", "unauthorized_client", "access_denied", "unsupported_grant_type", "InternalError" }, StringComparer.Ordinal);
        public static string ParseCode(string body)
        {
            if (String.IsNullOrEmpty(body) || body.Length > 2097152)
                return "";
            try
            {
                var root = Json.Object(Json.Serializer().DeserializeObject(body));
                object error = Json.Get(root, "error");
                var response = Json.Get(root, "response") as IDictionary<string, object>;
                if (error == null && response != null)
                    error = Json.Get(response, "error");
                var detail = error as IDictionary<string, object>;
                string code = error as string;
                if (detail != null)
                    code = Json.String(detail, "code");
                if (String.IsNullOrEmpty(code))
                    code = Json.String(root, "code");
                return code != null && Codes.Contains(code) ? code : "";
            }
            catch
            {
                return "";
            }
        }

        public static AlexaDiagnostic Create(string path, int status, string body)
        {
            return new AlexaDiagnostic
            {
                Version = typeof(AlexaDiagnostic).Assembly.GetName().Version.ToString(3),
                Step = StepFor(path),
                HttpStatus = status,
                ErrorCode = ParseCode(body),
                TimeUtc = DateTime.UtcNow
            };
        }

        public string UserMessage()
        {
            string result = L10n.Format("diagnostic.refused", Label(Step), HttpStatus, String.IsNullOrEmpty(ErrorCode) ? "" : " · " + ErrorCode);
            if (ErrorCode == "InvalidCode" || ErrorCode == "InvalidToken" || ErrorCode == "invalid_grant" || ErrorCode == "invalid_token")
                result += L10n.Text("diagnostic.retry");
            else if (Step == "session")
                result += L10n.Text("diagnostic.region");
            else
                result += L10n.Text("diagnostic.details");
            return result;
        }

        public void Save(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "amazon-status.json"), Json.Serializer().Serialize(this), new UTF8Encoding(false));
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (System.Security.SecurityException)
            {
            }
        }
    }
}
