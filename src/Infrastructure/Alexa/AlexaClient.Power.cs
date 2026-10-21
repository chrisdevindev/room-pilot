using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoomPilot
{
    public sealed partial class AlexaClient
    {
        static void CheckErrors(IDictionary<string, object> root)
        {
            var errors = Json.Items(Json.Get(root, "errors")).ToList();
            if (errors.Count == 0)
                return;
            if (errors.Any(e => Json.String(e, "code") == "ENDPOINT_UNREACHABLE"))
                throw new UserError(L10n.Text("error.offline"));
            if (errors.Any(e => Json.String(e, "code") == "TargetApplianceNotFoundException"))
                throw new UserError(L10n.Text("error.deviceIdentity"));
            throw new UserError(L10n.Text("error.alexaRequest"));
        }

        public async Task<bool> ReadPowerAsync(string id)
        {
            var response = await SendAuthenticatedAsync("/api/phoenix/state", "POST", new { stateRequests = new[] { new { entityId = id, entityType = "APPLIANCE" } } }).ConfigureAwait(false);
            return ParsePower(response, id);
        }

        public static bool ParsePower(IDictionary<string, object> response, string id)
        {
            CheckErrors(response);
            foreach (var device in Json.Items(Json.Get(response, "deviceStates")))
            {
                if (Convert.ToString(At(device, "entity", "entityId")) != id)
                    continue;
                var states = Json.Get(device, "capabilityStates") as System.Collections.IEnumerable;
                if (states == null)
                    continue;
                foreach (var raw in states)
                {
                    object decoded = raw;
                    if (raw is string)
                    {
                        try
                        {
                            decoded = Json.Serializer().DeserializeObject((string)raw);
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    var state = decoded as IDictionary<string, object>;
                    if (state == null)
                        continue;
                    if (Json.String(state, "namespace") == "Alexa.PowerController" && Json.String(state, "name") == "powerState")
                    {
                        string value = Json.String(state, "value");
                        if (value == "ON")
                            return true;
                        if (value == "OFF")
                            return false;
                    }
                }
            }

            throw new UserError(L10n.Text("error.alexaPower"));
        }

        public async Task SetPowerAsync(string id, bool power)
        {
            var response = await SendAuthenticatedAsync("/api/phoenix/state", "PUT", new { controlRequests = new[] { new { entityId = id, entityType = "APPLIANCE", parameters = new { action = power ? "turnOn" : "turnOff" } } } }).ConfigureAwait(false);
            CheckErrors(response);
            if (!Json.Items(Json.Get(response, "controlResponses")).Any(r =>
            {
                string direct = Json.String(r, "entityId"), nested = Convert.ToString(At(r, "entity", "entityId"));
                if (!String.IsNullOrEmpty(direct) && !String.IsNullOrEmpty(nested) && direct != nested)
                    return false;
                return (String.IsNullOrEmpty(direct) ? nested : direct) == id && Json.String(r, "code") == "SUCCESS";
            }))
                throw new UserError(L10n.Text("error.alexaCommand"));
        }
    }
}
