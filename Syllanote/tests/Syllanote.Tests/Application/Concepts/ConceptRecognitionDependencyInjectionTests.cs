using Microsoft.Extensions.DependencyInjection;
using Syllanote.Application;
using Syllanote.Application.Notebooks.Concepts.FindConceptReferences;
using Syllanote.Application.Notebooks.Concepts.Recognition;

namespace Syllanote.Tests.Application.Concepts;

[TestClass]
public class ConceptRecognitionDependencyInjectionTests
{
    [TestMethod]
    public void AddApplication_RegistersRecognizeConceptsServiceAsScoped()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        using var provider = services.BuildServiceProvider();

        RecognizeConceptsService first;
        using (var firstScope = provider.CreateScope())
        {
            first = firstScope.ServiceProvider
                .GetRequiredService<RecognizeConceptsService>();
            var second = firstScope.ServiceProvider
                .GetRequiredService<RecognizeConceptsService>();

            Assert.AreSame(first, second);
        }

        using var secondScope = provider.CreateScope();
        var fromSecondScope = secondScope.ServiceProvider
            .GetRequiredService<RecognizeConceptsService>();

        Assert.AreNotSame(first, fromSecondScope);
    }

    [TestMethod]
    public void AddApplication_RegistersFindConceptReferencesServiceAsScoped()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var registration = services.Single(descriptor =>
            descriptor.ServiceType == typeof(FindConceptReferencesService));
        Assert.AreEqual(ServiceLifetime.Scoped, registration.Lifetime);
    }
}
