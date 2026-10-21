using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoomPilot
{
    public sealed partial class AlexaClient
    {
        public async Task<List<AlexaEndpoint>> ListDevicesAsync()
        {
            const string query = "query RoomPilotEndpoints { endpoints { items { friendlyName features { name operations { name } } legacyAppliance { entityId applianceId friendlyName manufacturerName actions } } } }";
            var root = await SendAuthenticatedAsync("/nexus/v1/graphql", "POST", new { query = query }).ConfigureAwait(false);
            if (At(root, "data", "endpoints", "items") == null)
                throw new UserError(L10n.Text("error.listDevices"));
            return ParseEndpoints(At(root, "data", "endpoints", "items"));
        }

        public static List<AlexaEndpoint> ParseEndpoints(object items)
        {
            var found = new List<AlexaEndpoint>();
            foreach (var item in Json.Items(items))
            {
                var legacy = Json.Get(item, "legacyAppliance") as IDictionary<string, object>;
                if (legacy == null)
                    continue;
                bool power = Json.Items(Json.Get(item, "features")).Any(f =>
                {
                    string name = Json.String(f, "name");
                    if (name == "Alexa.PowerController")
                        return true;
                    var operations = Json.Items(Json.Get(f, "operations")).Select(o => Json.String(o, "name")).ToList();
                    return name == "power" && operations.Contains("turnOn") && operations.Contains("turnOff");
                });
                var actions = Json.Get(legacy, "actions") as System.Collections.IEnumerable;
                if (actions != null)
                    foreach (var a in actions)
                        if (String.Equals(Convert.ToString(a), "turnOn", StringComparison.OrdinalIgnoreCase))
                            power = true;
                // phoenix/state with entityType APPLIANCE addresses applianceId, not entityId.
                string id = Json.String(legacy, "applianceId");
                if (!power || String.IsNullOrWhiteSpace(id) || found.Any(d => d.Id == id))
                    continue;
                string friendly = Json.String(item, "friendlyName");
                if (String.IsNullOrWhiteSpace(friendly))
                    friendly = Json.String(legacy, "friendlyName");
                found.Add(new AlexaEndpoint { Id = id, Name = String.IsNullOrWhiteSpace(friendly) ? L10n.Text("default.unnamed") : friendly.Trim(), Manufacturer = Json.String(legacy, "manufacturerName") });
            }

            // Keep duplicate names distinguishable in the mapping screen.
            foreach (var group in found.GroupBy(d => d.Name).Where(g => g.Count() > 1))
            {
                int i = 0;
                foreach (var device in group)
                    device.Name += " (" + (++i) + ")";
            }

            return found.OrderBy(d => d.Name).ToList();
        }
    }
}
