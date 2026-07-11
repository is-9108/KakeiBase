using NetArchTest.Rules;

namespace KakeiBase.ArchitectureTests;

public class LayerDependencyTests
{
    private const string RootNamespace = "KakeiBase.WebApi";
    private const string DomainNamespace = "KakeiBase.WebApi.Domain";
    private const string ApplicationNamespace = "KakeiBase.WebApi.Application";
    private const string InfrastructureNamespace = "KakeiBase.WebApi.Infrastructure";
    private const string EndpointsNamespace = "KakeiBase.WebApi.Endpoints";

    private static readonly Types AllTypes =
        Types.InAssembly(typeof(Program).Assembly);

    [Fact]
    public void Domain_Should_Not_DependOn_Application()
    {
        var result = AllTypes
            .That().ResideInNamespace(DomainNamespace)
            .ShouldNot().HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain層がApplication層に依存しています: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_Should_Not_DependOn_Infrastructure()
    {
        var result = AllTypes
            .That().ResideInNamespace(DomainNamespace)
            .ShouldNot().HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain層がInfrastructure層に依存しています: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_Should_Not_DependOn_Endpoints()
    {
        var result = AllTypes
            .That().ResideInNamespace(DomainNamespace)
            .ShouldNot().HaveDependencyOn(EndpointsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Domain層がEndpoints層に依存しています: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Should_Not_DependOn_Infrastructure()
    {
        var result = AllTypes
            .That().ResideInNamespace(ApplicationNamespace)
            .ShouldNot().HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application層がInfrastructure層に依存しています: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Should_Not_DependOn_Endpoints()
    {
        var result = AllTypes
            .That().ResideInNamespace(ApplicationNamespace)
            .ShouldNot().HaveDependencyOn(EndpointsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Application層がEndpoints層に依存しています: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_Should_Not_DependOn_Endpoints()
    {
        var result = AllTypes
            .That().ResideInNamespace(InfrastructureNamespace)
            .ShouldNot().HaveDependencyOn(EndpointsNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"Infrastructure層がEndpoints層に依存しています: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
