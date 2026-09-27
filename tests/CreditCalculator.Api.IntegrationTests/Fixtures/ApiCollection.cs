namespace CreditCalculator.Api.IntegrationTests.Fixtures;

// Один контейнер PostgreSQL на все тестовые классы: запуск контейнера — самая дорогая часть тестов.
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
