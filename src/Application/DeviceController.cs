using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RoomPilot
{
    public sealed class DeviceController : IDisposable
    {
        public AppConfig Config { get; private set; }
        public Dictionary<string, DeviceState> States { get; private set; }

        public event Action Changed;
        public event Action<string> Notice;
        readonly IDeviceService service;
        readonly SemaphoreSlim operation = new SemaphoreSlim(1, 1);
        public bool Busy { get; private set; }

        public DeviceController(AppConfig config, IDeviceService deviceService)
        {
            Config = config;
            service = deviceService;
            States = config.Devices.ToDictionary(device => device.Key, device => new DeviceState());
            foreach (var device in config.Devices)
            {
                if (String.IsNullOrEmpty(device.DeviceId))
                    States[device.Key].Message = L10n.Text("state.selectDevice");
            }
        }

        void NotifyChanged()
        {
            if (Changed != null)
                Changed();
        }

        public bool IsConfigured(DeviceConfig device)
        {
            return service != null && service.IsConnected && !String.IsNullOrWhiteSpace(device.DeviceId);
        }

        async Task ReadDeviceAsync(DeviceConfig device)
        {
            var state = States[device.Key];
            state.Power = null;
            state.CanControl = false;
            state.Online = false;
            state.Busy = true;
            NotifyChanged();
            try
            {
                if (!IsConfigured(device))
                    throw new UserError(L10n.Text("error.pending"));
                state.Power = await service.ReadPowerAsync(device.DeviceId).ConfigureAwait(false);
                state.Online = true;
                if (!state.Power.HasValue)
                    throw new UserError(L10n.Text("error.unknownState"));
                state.CanControl = true;
                state.Message = state.Power.Value ? L10n.Text("state.on") : L10n.Text("state.off");
                state.CheckedAt = DateTime.Now;
            }
            catch (Exception error)
            {
                state.Message = error is UserError ? error.Message : L10n.Text("error.connection");
            }
            finally
            {
                state.Busy = false;
                NotifyChanged();
            }
        }

        public async Task RefreshAsync()
        {
            if (!await operation.WaitAsync(0).ConfigureAwait(false))
                return;
            Busy = true;
            NotifyChanged();
            try
            {
                foreach (var device in Config.Devices)
                    await ReadDeviceAsync(device).ConfigureAwait(false);
            }
            finally
            {
                Busy = false;
                operation.Release();
                NotifyChanged();
            }
        }

        public async Task<bool> ExecuteAsync(string key, PowerAction action)
        {
            if (!await operation.WaitAsync(0).ConfigureAwait(false))
            {
                if (Notice != null)
                    Notice(L10n.Text("error.busy"));
                return false;
            }

            Busy = true;
            NotifyChanged();
            var device = Config.Devices.FirstOrDefault(x => x.Key == key);
            try
            {
                if (!Enum.IsDefined(typeof(PowerAction), action))
                    throw new UserError(L10n.Text("error.powerAction"));
                if (device == null)
                    throw new UserError(L10n.Text("error.deviceMissing"));
                // Read the current device state, including changes made in Alexa or at the wall.
                await ReadDeviceAsync(device).ConfigureAwait(false);
                var state = States[key];
                if (!state.CanControl || !state.Power.HasValue)
                    throw new UserError(state.Message);
                bool next = action == PowerAction.Toggle ? !state.Power.Value : action == PowerAction.On;
                state.Busy = true;
                NotifyChanged();
                try
                {
                    await service.SetPowerAsync(device.DeviceId, next).ConfigureAwait(false);
                    state.Power = null;
                    state.CanControl = false;
                    state.Message = L10n.Text("state.sending");
                    NotifyChanged();
                    bool confirmed = false;
                    for (int i = 0; i < 3; i++)
                    {
                        await Task.Delay(600).ConfigureAwait(false);
                        await ReadDeviceAsync(device).ConfigureAwait(false);
                        if (state.Power == next)
                        {
                            confirmed = true;
                            break;
                        }
                    }

                    if (!confirmed)
                        throw new UserError(L10n.Text("error.confirmation"));
                    if (Notice != null)
                        Notice(L10n.Format(next ? "notice.powerOn" : "notice.powerOff", device.Name));
                    return true;
                }
                catch
                {
                    state.Power = null;
                    state.CanControl = false;
                    state.Message = L10n.Text("state.commandUnconfirmed");
                    throw;
                }
            }
            catch (Exception error)
            {
                if (Notice != null)
                    Notice(error is UserError ? error.Message : L10n.Text("error.command"));
                return false;
            }
            finally
            {
                if (device != null)
                    States[key].Busy = false;
                Busy = false;
                operation.Release();
                NotifyChanged();
            }
        }

        public void Dispose()
        {
            if (service != null)
                service.Dispose();
            operation.Dispose();
        }
    }
}
