namespace Iago.Runner
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;
  using Microsoft.Framework.Logging;

  public delegate Type[] GetTypesWithTests(params string[] args);

  public class ApplicationHost
  {

    public HostedConfiguration Configuration {get;}
    private GetTypesWithTests getTypesWithTests;
    private readonly ILogger logger;

    public ApplicationHost(
      HostedConfiguration hostedConfiguration,
      ILogger logger = null)
    {
      Configuration = hostedConfiguration;
      this.logger = logger ?? new SimpleConsoleLogger();
    }

    public void Run()
    {
      var hostedAssembly = Assembly.Load(Configuration.HostedApplicationName);
      logger.WriteWarning(
        $"scanning assembly [{hostedAssembly.GetName().Name}]");

      var specTypes = new List<Type>();
      hostedAssembly.ExportedTypes
          .Where(type => type.Name.ToLower().EndsWith("specs"))
          .Where(type => type.IsClass)
          .ToList()
          .ForEach(type=> specTypes.Add(type));

      logger.WriteWarning("found specs");



        foreach(var spec in specTypes)
        {
          using(logger.BeginScope("running " + spec.Name + ""))
          {
            var instance = Activator.CreateInstance(spec);

            var specifyField = spec
                .GetFields(
                  BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(x=>x.FieldType == typeof(Specify));
            if(specifyField != null)
            {
              var specify = (specifyField.GetValue(instance) as Specify)?.Invoke();
              logger.WriteInformation("=> " + specify);
            }
            var run = spec.GetMethod("Run");
            if(run != null)
            {
              try
              {
                run.Invoke(instance,null);
              } catch(Exception ex)
              {
                logger.WriteError(ex.InnerException.Message);
              }

            }
          }
        }

    }
  }

}
