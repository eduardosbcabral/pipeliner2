using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PipelineR
{
    public static class AutoInject
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        public static void SetupPipelineR(this IServiceCollection services)
        {
            var assemblies = GetAssemblies();

            var types = assemblies
                        .SelectMany(a => a.GetTypes())
                        .Select(a => a.GetTypeInfo());

            InjectContexts(services, types);
            InjectPipes(services, types);

            //Generate Docs
            services.AddSingleton(p => new DocumentationBuilder());
            ExecutePipelineStarting(services, types, typeof(PipelineStartingDiagram<>));
            ServiceProvider = services.BuildServiceProvider();

            LoadingDiagrams(types);

            var documentation = ServiceProvider.GetService<DocumentationBuilder>();
            documentation.Compile();

            ExecutePipelineStarting(services, types, typeof(PipelineStarting<>));

            ServiceProvider = services.BuildServiceProvider();
        }

        private static void ExecutePipelineStarting(IServiceCollection services, IEnumerable<TypeInfo> types, Type basePipelineStarting, bool isInsert = true)
        {
            Type baseType = typeof(IPipelineStarting<>);

            var contexts = types
                                .Where(a => a.IsClass && a.BaseType == typeof(BaseContext));

            foreach (var context in contexts)
            {
                Type[] typeArgs = { context.AsType() };
                var typeWithGeneric = baseType.MakeGenericType(typeArgs);

                Type impleBaseType = basePipelineStarting;
                var impleTypeWithGeneric = impleBaseType.MakeGenericType(typeArgs);

                if (isInsert)
                    services.AddScoped(typeWithGeneric, impleTypeWithGeneric);
                else
                    services.Remove(new ServiceDescriptor(typeWithGeneric, impleTypeWithGeneric, ServiceLifetime.Scoped));
            }
        }

        private static IEnumerable<Assembly> GetAssemblies()
        {
            var dependencies = DependencyContext.Default.RuntimeLibraries.Where(p =>
                p.Type.Equals("Project", StringComparison.CurrentCultureIgnoreCase));

            foreach (var library in dependencies)
            {
                var name = new AssemblyName(library.Name);
                var assembly = Assembly.Load(name);
                yield return assembly;
            }
        }

        private static void InjectContexts(IServiceCollection services, IEnumerable<TypeInfo> typeInfos)
        {
            var contexts = typeInfos
                                .Where(a => a.IsClass && a.BaseType == typeof(BaseContext));

            foreach (var context in contexts)
                services.AddScoped(context.AsType());
        }

        private static void InjectPipes(IServiceCollection services, IEnumerable<TypeInfo> types)
        {
            var searchingCondition = new[] { "RequestHandler", "ICondition", "IPipelineBuilder" };

            var pipes = types
                        .Where(a => a.IsClass &&
                                       !searchingCondition.Any(exclude => a.Name.Contains(exclude)) &&
                                       a.ImplementedInterfaces.Any(i =>
                                                             searchingCondition.Any(include => i.Name.Contains(include))));

            foreach (var pipe in pipes)
            {
                var interfaces = pipe.GetInterfaces()
                                .Where(a => !searchingCondition.Any(e => a.Name.Contains(e)));

                foreach (var i in interfaces)
                    services.AddTransient(i, pipe.AsType());
            }
        }

        /// <summary>
        /// Pega todos PipelineBuilder e seus métodos para criar os diagramas
        /// </summary>
        /// <param name="types"></param>
        private static void LoadingDiagrams(IEnumerable<TypeInfo> types)
        {
            var searchingCondition = new[] { "IPipelineBuilder" };

            var pipes = types
                        .Where(a => a.IsClass &&
                                       !searchingCondition.Any(exclude => a.Name.Contains(exclude)) &&
                                       a.ImplementedInterfaces.Any(i =>
                                                             searchingCondition.Any(include => i.Name.Contains(include))));

            foreach (var pipe in pipes)
            {
                var interfaces = pipe.GetInterfaces()
                                .Where(a => !searchingCondition.Any(e => a.Name.Contains(e)));

                foreach (var @interface in interfaces)
                {
                    var inst = ServiceProvider.GetService(@interface);
                    var methods = inst.GetType().GetMethods();

                    foreach (var method in methods.Where(m => m.ReturnType.Name.Contains("RequestHandlerResult")))
                    {
                        var parameter = method.GetParameters().FirstOrDefault();

                        //AutoFaker.Configure(b =>
                        //{
                        //    b.WithRepeatCount(1);
                        //    b.WithOverride(new StringGeneratorOverride());
                        //});

                        var instance = Activator.CreateInstance(parameter.ParameterType, null);

                        //var rr = parameter.ParameterType.GetClassStructure(new SnakeCaseNamingStrategy());
                        method.Invoke(inst, new object[] { instance });

                        //DrawDiagram.AddRequest(result);
                    }
                }
            }
        }
    }
}