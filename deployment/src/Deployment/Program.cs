using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Deployment
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new DeploymentStack(app, "DeploymentStack");
            app.Synth();
        }
    }
}
