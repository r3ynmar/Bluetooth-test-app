using Microsoft.Extensions.DependencyInjection;
using Shiny;
using System;
using System.Collections.Generic;
using System.Text;

namespace BluetoothSampleApp
{
    public class ShinyBluetoothStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.UseBleClient();
            services.UseBleHosting();
        }
    }
}
