using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GearFlow.Api.OpenApi;

/// <summary>
/// Declara o esquema de segurança Bearer (JWT) no documento OpenAPI. Sem isto, o Scalar não mostra o
/// botão "Authorize" e as chamadas a endpoints protegidos vão sem o header Authorization.
/// Fluxo: faça login em <c>/api/identity/auth/login</c>, copie o <c>accessToken</c> e cole no Scalar.
/// </summary>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // transformer OpenAPI (setup) — fora da meta de cobertura
internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeId = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[SchemeId] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT de staff. Faça login em POST /api/identity/auth/login e cole aqui só o accessToken (sem 'Bearer ')."
        };

        // Requisito global de segurança → o Scalar pré-seleciona o Bearer e aplica em todas as chamadas.
        var requirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeId, document)] = new List<string>()
        };
        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(requirement);

        return Task.CompletedTask;
    }
}
