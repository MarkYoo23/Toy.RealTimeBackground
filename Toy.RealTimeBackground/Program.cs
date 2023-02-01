using Microsoft.Extensions.Hosting.WindowsServices;
using System.Diagnostics;

namespace Toy.RealTimeBackground.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var options = new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = WindowsServiceHelpers.IsWindowsService()
                                           ? AppContext.BaseDirectory : default
            };

            var builder = WebApplication.CreateBuilder(options);
            builder.Host.UseWindowsService();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseAuthorization();
            app.MapControllers();

            Thread t1 = new Thread(async() =>
            {
                var currentProcess = Process.GetCurrentProcess();
                currentProcess.PriorityBoostEnabled = true;
                currentProcess.PriorityClass = ProcessPriorityClass.RealTime;

                var currentProcessThread = currentProcess.Threads;
                var cpuNumber = 1;

                foreach (ProcessThread thread in currentProcessThread)
                {
                    if (thread.Id == Environment.CurrentManagedThreadId)
                    {
                        thread.IdealProcessor = cpuNumber;
                        thread.ProcessorAffinity = new IntPtr(1 << (cpuNumber));
                        break;
                    }

                    thread.PriorityLevel = ThreadPriorityLevel.Highest;
                }

                Console.WriteLine("thread 1 inner : "+Environment.CurrentManagedThreadId);

                while (true)
                {
                    Console.WriteLine("Hello World!");
                    await Task.Delay(1000);
                }
            });
            t1.Priority = ThreadPriority.Highest;            
            t1.Start();

            Console.WriteLine("thread 1 outer : " + t1.ManagedThreadId);
            Console.WriteLine("main thread : " + Environment.CurrentManagedThreadId);

            await app.RunAsync();
        }
    }
}
