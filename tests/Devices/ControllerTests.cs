using System.Threading.Tasks;

namespace RoomPilot.Tests
{
    static partial class TestRunner
    {
        static async Task TestController()
        {
            var config = Credentials();
            using (var fake = new FakeService())
            using (var controller = new DeviceController(config, fake))
            {
                await controller.RefreshAsync();
                Check(controller.States["lamparina"].Power == false, "consulta usa o estado retornado pelo serviço");
                fake.Power = true;
                Check(await controller.ExecuteAsync("lamparina", PowerAction.Toggle), "alternância confirma o estado depois do comando");
                Check(!fake.LastTarget && !fake.Power, "alternância considera mudanças feitas fora do programa");
                fake.Online = false;
                Check(!await controller.ExecuteAsync("lamparina", PowerAction.On) && fake.Commands == 1, "aparelho offline não recebe comandos");
                Check(controller.States["lamparina"].Power == null, "offline não aparece como desligado");
            }

            using (var fake = new FakeService
            {
                UnknownPower = true
            }

            )
            using (var controller = new DeviceController(config, fake))
            {
                Check(!await controller.ExecuteAsync("tomada", PowerAction.Toggle) && fake.Commands == 0, "estado desconhecido bloqueia a alternância");
            }

            using (var controller = new DeviceController(new AppConfig(), null))
            {
                Check(!await controller.ExecuteAsync("tomada", PowerAction.On), "conexão pendente não simula controle");
            }

            using (var fake = new FakeService
            {
                IgnoreCommands = true
            }

            )
            using (var controller = new DeviceController(config, fake))
            {
                Check(!await controller.ExecuteAsync("tomada", PowerAction.On) && fake.Commands == 1, "mudança não confirmada não repete o comando automaticamente");
                Check(controller.States["tomada"].Power == null && !controller.States["tomada"].CanControl, "falha de confirmação deixa o estado desconhecido");
            }

            using (var fake = new FakeService())
            using (var controller = new DeviceController(config, fake))
            {
                Check(!await controller.ExecuteAsync("missing", PowerAction.On) && fake.Commands == 0, "identificador inexistente não aciona outro aparelho");
                Check(!await controller.ExecuteAsync("tomada", (PowerAction)99) && fake.Commands == 0, "ação inválida não aciona aparelhos");
                var extra = DeviceCatalog.Add(config, "Novo controle");
                extra.DeviceId = "extra-test";
            }

            using (var fake = new FakeService())
            using (var controller = new DeviceController(config, fake))
            {
                string key = config.Devices[3].Key;
                Check(await controller.ExecuteAsync(key, PowerAction.On) && fake.LastDevice == "extra-test", "novo aparelho usa o mesmo fluxo de comando dos controles originais");
            }

            using (var fake = new FakeService
            {
                HoldRead = new TaskCompletionSource<bool?>()
            }

            )
            using (var controller = new DeviceController(config, fake))
            {
                var pending = controller.ExecuteAsync("tomada", PowerAction.On);
                Check(!await controller.ExecuteAsync("fita", PowerAction.Off) && fake.Commands == 0, "comandos concorrentes são bloqueados durante uma consulta");
                var held = fake.HoldRead;
                fake.HoldRead = null;
                held.SetResult(false);
                Check(await pending && fake.Commands == 1, "comando pendente conclui uma única vez após a consulta");
            }
        }

        sealed class FakeService : IDeviceService
        {
            public bool Power, LastTarget, UnknownPower, IgnoreCommands;
            public bool Online = true;
            public int Commands;
            public string LastDevice;
            public TaskCompletionSource<bool?> HoldRead;
            public bool IsConnected
            {
                get
                {
                    return true;
                }
            }

            public Task<bool?> ReadPowerAsync(string id)
            {
                if (HoldRead != null)
                    return HoldRead.Task;
                if (!Online)
                    throw new UserError("Offline");
                return Task.FromResult(UnknownPower ? (bool? )null : Power);
            }

            public Task SetPowerAsync(string id, bool power)
            {
                Commands++;
                LastTarget = power;
                LastDevice = id;
                if (!IgnoreCommands)
                    Power = power;
                return Task.FromResult(0);
            }

            public void Dispose()
            {
            }
        }
    }
}
