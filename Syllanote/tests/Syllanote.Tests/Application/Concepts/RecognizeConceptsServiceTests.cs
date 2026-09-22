using Syllanote.Application.Notebooks.Concepts.Recognition;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Concepts;

[TestClass]
public class RecognizeConceptsServiceTests
{
    private readonly RecognizeConceptsService _service = new();

    [TestMethod]
    public void Recognize_WithEmptyContent_ReturnsNoMatches()
    {
        var matches = _service.Recognize(
            string.Empty,
            [CreateConcept("Singleton")]);

        Assert.AreEqual(0, matches.Count);
    }

    [TestMethod]
    public void Recognize_WithNoConcepts_ReturnsNoMatches()
    {
        var matches = _service.Recognize("Singleton is useful.", []);

        Assert.AreEqual(0, matches.Count);
    }

    [TestMethod]
    public void Recognize_WithExactName_ReturnsConceptAndTextRange()
    {
        var concept = CreateConcept("Dependency Injection");
        const string content = "Using Dependency Injection helps testing.";

        var match = _service.Recognize(content, [concept]).Single();

        Assert.AreEqual(concept.Id, match.ConceptId);
        Assert.AreEqual(concept.Name, match.ConceptName);
        Assert.AreEqual(6, match.StartIndex);
        Assert.AreEqual("Dependency Injection".Length, match.Length);
        Assert.AreEqual(
            "Dependency Injection",
            content.Substring(match.StartIndex, match.Length));
    }

    [TestMethod]
    public void Recognize_IsCaseInsensitive()
    {
        var concept = CreateConcept("Singleton");

        var match = _service.Recognize(
            "A singleton is useful.",
            [concept]).Single();

        Assert.AreEqual(concept.Id, match.ConceptId);
        Assert.AreEqual("singleton", "A singleton is useful."
            .Substring(match.StartIndex, match.Length));
    }

    [TestMethod]
    public void Recognize_RequiresBoundariesAroundConceptName()
    {
        var concept = CreateConcept("Singleton");
        const string content =
            "SingletonPattern preSingleton Singleton_value Singleton1 Singleton";

        var match = _service.Recognize(content, [concept]).Single();

        Assert.AreEqual(content.LastIndexOf("Singleton"), match.StartIndex);
    }

    [TestMethod]
    public void Recognize_AllowsPunctuationAndWhitespaceAsBoundaries()
    {
        var concept = CreateConcept("Singleton");
        const string content = "(Singleton),\nsingleton!";

        var matches = _service.Recognize(content, [concept]);

        Assert.AreEqual(2, matches.Count);
        Assert.AreEqual("Singleton", content.Substring(
            matches[0].StartIndex, matches[0].Length));
        Assert.AreEqual("singleton", content.Substring(
            matches[1].StartIndex, matches[1].Length));
    }

    [TestMethod]
    public void Recognize_MatchesMultiWordConceptWithExactInternalWhitespace()
    {
        var concept = CreateConcept("Dependency Injection");
        const string content =
            "Dependency Injection; Dependency  Injection; Dependency\tInjection";

        var match = _service.Recognize(content, [concept]).Single();

        Assert.AreEqual(0, match.StartIndex);
        Assert.AreEqual(concept.Name.Length, match.Length);
    }

    [TestMethod]
    public void Recognize_MatchesNamesContainingSymbolsAndNumbers()
    {
        var cSharp = CreateConcept("C#");
        var dotNet = CreateConcept(".NET");
        var f1Score = CreateConcept("F1 Score");
        const string content = "C#, .NET, and F1 Score.";

        var matches = _service.Recognize(
            content,
            [f1Score, dotNet, cSharp]);

        Assert.AreEqual(3, matches.Count);
        CollectionAssert.AreEqual(
            new[] { cSharp.Id, dotNet.Id, f1Score.Id },
            matches.Select(match => match.ConceptId).ToArray());
    }

    [TestMethod]
    public void Recognize_DoesNotMatchSymbolNamesInsideLargerTokens()
    {
        var cSharp = CreateConcept("C#");
        var dotNet = CreateConcept(".NET");
        var f1Score = CreateConcept("F1 Score");

        var matches = _service.Recognize(
            "C#8 ASP.NET F1 Scores",
            [cSharp, dotNet, f1Score]);

        Assert.AreEqual(0, matches.Count);
    }

    [TestMethod]
    public void Recognize_MatchesDanishLettersCaseInsensitively()
    {
        var concepts = new[]
        {
            CreateConcept("Årsag"),
            CreateConcept("Ændring"),
            CreateConcept("Øvelse")
        };

        var matches = _service.Recognize(
            "årsag, ændring og øvelse",
            concepts);

        Assert.AreEqual(3, matches.Count);
        CollectionAssert.AreEqual(
            concepts.Select(concept => concept.Id).ToArray(),
            matches.Select(match => match.ConceptId).ToArray());
    }

    [TestMethod]
    public void Recognize_DoesNotTreatAlternativeSpellingAsSameName()
    {
        var matches = _service.Recognize(
            "aarsag",
            [CreateConcept("Årsag")]);

        Assert.AreEqual(0, matches.Count);
    }

    [TestMethod]
    public void Recognize_ReturnsEveryRepeatedOccurrence()
    {
        var concept = CreateConcept("Singleton");
        const string content = "Singleton, singleton, SINGLETON.";

        var matches = _service.Recognize(content, [concept]);

        Assert.AreEqual(3, matches.Count);
        Assert.IsTrue(matches.All(match => match.ConceptId == concept.Id));
        CollectionAssert.AreEqual(
            new[] { 0, 11, 22 },
            matches.Select(match => match.StartIndex).ToArray());
    }

    [TestMethod]
    public void Recognize_WhenMatchesStartTogether_PrefersLongestName()
    {
        var dependency = CreateConcept("Dependency");
        var dependencyInjection = CreateConcept("Dependency Injection");

        var match = _service.Recognize(
            "Dependency Injection is useful.",
            [dependency, dependencyInjection]).Single();

        Assert.AreEqual(dependencyInjection.Id, match.ConceptId);
    }

    [TestMethod]
    public void Recognize_WhenMatchesPartiallyOverlap_PrefersEarliestStart()
    {
        var dataScience = CreateConcept("Data Science");
        var scienceModel = CreateConcept("Science Model");

        var match = _service.Recognize(
            "Data Science Model",
            [scienceModel, dataScience]).Single();

        Assert.AreEqual(dataScience.Id, match.ConceptId);
        Assert.AreEqual(0, match.StartIndex);
    }

    [TestMethod]
    public void Recognize_IsIndependentOfConceptInputOrder()
    {
        var dependency = CreateConcept("Dependency");
        var dependencyInjection = CreateConcept("Dependency Injection");
        var singleton = CreateConcept("Singleton");
        const string content = "Dependency Injection and Singleton";

        var first = _service.Recognize(
            content,
            [dependency, singleton, dependencyInjection]);
        var second = _service.Recognize(
            content,
            [singleton, dependencyInjection, dependency]);

        CollectionAssert.AreEqual(first.ToArray(), second.ToArray());
    }

    [TestMethod]
    public void Recognize_ReturnsMatchesOrderedByStartIndex()
    {
        var singleton = CreateConcept("Singleton");
        var polymorphism = CreateConcept("Polymorphism");

        var matches = _service.Recognize(
            "Polymorphism before Singleton.",
            [singleton, polymorphism]);

        CollectionAssert.AreEqual(
            new[] { polymorphism.Id, singleton.Id },
            matches.Select(match => match.ConceptId).ToArray());
        Assert.IsTrue(matches[0].StartIndex < matches[1].StartIndex);
    }

    [TestMethod]
    public void Recognize_WhenConceptListChanges_UsesCurrentNameAndMembership()
    {
        var concept = CreateConcept("Singleton");
        const string content = "Singleton and Polymorphism";

        var original = _service.Recognize(content, [concept]).Single();

        concept.Rename("Polymorphism");
        var renamed = _service.Recognize(content, [concept]).Single();
        var deleted = _service.Recognize(content, []);

        Assert.AreEqual(concept.Id, original.ConceptId);
        Assert.AreEqual(0, original.StartIndex);
        Assert.AreEqual(concept.Id, renamed.ConceptId);
        Assert.AreEqual("Polymorphism", renamed.ConceptName);
        Assert.AreEqual(14, renamed.StartIndex);
        Assert.AreEqual(0, deleted.Count);
    }

    [TestMethod]
    public void Recognize_WithNullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(() =>
            _service.Recognize(null!, []));
        Assert.ThrowsException<ArgumentNullException>(() =>
            _service.Recognize(string.Empty, null!));
    }

    private static Concept CreateConcept(string name)
    {
        return new Concept(Guid.NewGuid(), name, "Definition");
    }
}
