using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RoomPilot.Tests
{
    static partial class TestRunner
    {
        static int count;
        static void Check(bool condition, string name)
        {
            if (!condition)
                throw new Exception("FALHOU: " + name);
            count++;
            Console.WriteLine("OK " + name);
        }

        static AppConfig Credentials()
        {
            var config = new AppConfig();
            foreach (var d in config.Devices)
                d.DeviceId = d.Key + "-test";
            return config;
        }

        static object Decode(string json)
        {
            return Json.Serializer().DeserializeObject(json);
        }

        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                if (args.Length == 2 && args[0] == "--icon")
                {
                    using (var icon = Theme.CreateIcon())
                    using (var stream = File.Create(args[1]))
                        icon.Save(stream);
                    return 0;
                }

                SynchronizationContext.SetSynchronizationContext(new System.Windows.Forms.WindowsFormsSynchronizationContext());
                L10n.SetLanguage(L10n.Portuguese);
                var tests = Run();
                while (!tests.IsCompleted)
                {
                    System.Windows.Forms.Application.DoEvents();
                    Thread.Sleep(10);
                }

                tests.GetAwaiter().GetResult();
                Console.WriteLine(count + " verificações aprovadas.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        static async Task Run()
        {
            TestConfiguration();
            TestPrivateStorage();
            TestLocalization();
            await TestController();
            TestDynamicForms();
            SynchronizationContext.SetSynchronizationContext(null);
            await TestAlexa();
        }

        static System.Collections.Generic.IEnumerable<System.Windows.Forms.Control> Descendants(System.Windows.Forms.Control parent)
        {
            foreach (System.Windows.Forms.Control child in parent.Controls)
            {
                yield return child;
                foreach (var nested in Descendants(child))
                    yield return nested;
            }
        }
    }
}
