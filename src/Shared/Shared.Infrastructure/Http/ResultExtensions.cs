using Microsoft.AspNetCore.Http;
using Shared.Domain.Primitives;

namespace Shared.Infrastructure.Http;

/// <summary>
/// Tradução central de <see cref="Result"/>/<see cref="Error"/> para respostas HTTP no padrão
/// ProblemDetails (RFC 9457). Os endpoints chamam <c>result.ToOk()</c> em vez de montar
/// `Results.Ok/BadRequest/...` à mão — o status vem do <see cref="ErrorType"/> (ou, p/ os erros
/// legados com Type=Failure, é inferido pelo sufixo do <see cref="Error.Code"/>).
/// </summary>
public static class ResultExtensions
{
    /// <summary>200 com o valor no sucesso; ProblemDetails no erro.</summary>
    public static IResult ToOk<T>(this Result<T> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();

    /// <summary>200 sem corpo no sucesso; ProblemDetails no erro. Para commands sem retorno.</summary>
    public static IResult ToOk(this Result result) =>
        result.IsSuccess ? TypedResults.Ok() : result.Error.ToProblem();

    /// <summary>204 no sucesso; ProblemDetails no erro. Para deletes/updates sem corpo.</summary>
    public static IResult ToNoContent(this Result result) =>
        result.IsSuccess ? TypedResults.NoContent() : result.Error.ToProblem();

    /// <summary>201 + Location no sucesso; ProblemDetails no erro.</summary>
    public static IResult ToCreated<T>(this Result<T> result, string location) =>
        result.IsSuccess ? TypedResults.Created(location, result.Value) : result.Error.ToProblem();

    /// <summary>Converte um <see cref="Error"/> no ProblemDetails correspondente.</summary>
    public static IResult ToProblem(this Error error)
    {
        var status = StatusFor(error);

        if (status == StatusCodes.Status400BadRequest && IsValidation(error))
        {
            var messages = error.Description.Split("; ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    [error.Field ?? "request"] = messages.Length > 0 ? messages : [error.Description],
                },
                detail: error.Description,
                extensions: new Dictionary<string, object?> { ["code"] = error.Code });
        }

        return TypedResults.Problem(
            detail: error.Description,
            statusCode: status,
            title: TitleFor(status),
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    private static bool IsValidation(Error error) =>
        error.Type == ErrorType.Validation || error.Code.StartsWith("Validation.", StringComparison.Ordinal);

    private static int StatusFor(Error error) => error.Type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        _ => StatusFromCode(error.Code),
    };

    private static int StatusFromCode(string code)
    {
        if (code.StartsWith("Validation.", StringComparison.Ordinal))
            return StatusCodes.Status400BadRequest;
        if (code.EndsWith("NotFound", StringComparison.Ordinal))
            return StatusCodes.Status404NotFound;
        if (code.EndsWith("Forbidden", StringComparison.Ordinal))
            return StatusCodes.Status403Forbidden;
        if (code.EndsWith("Unauthorized", StringComparison.Ordinal))
            return StatusCodes.Status401Unauthorized;
        if (code.EndsWith("Conflict", StringComparison.Ordinal)
            || code.EndsWith("AlreadyExists", StringComparison.Ordinal)
            || code.EndsWith("AlreadyInUse", StringComparison.Ordinal))
            return StatusCodes.Status409Conflict;
        return StatusCodes.Status400BadRequest;
    }

    private static string TitleFor(int status) => status switch
    {
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Bad Request",
    };
}

/// <summary>
/// Atalhos p/ produzir ProblemDetails direto do endpoint quando NÃO há um <see cref="Result"/> em
/// mãos: falha de guard antes do handler, checagens de parâmetro, recurso ausente numa leitura
/// direta. Mantém o corpo de erro consistente com <see cref="ResultExtensions"/>.
/// </summary>
public static class Problems
{
    public static IResult Unauthorized(
        string code = "Auth.Unauthorized",
        string detail = "Autenticação necessária ou credenciais inválidas.") =>
        Error.Unauthorized(code, detail).ToProblem();

    public static IResult Forbidden(string detail = "Você não tem acesso a este recurso.") =>
        Error.Forbidden("Auth.Forbidden", detail).ToProblem();

    public static IResult NotFound(string code, string detail) =>
        new Error(code, detail, ErrorType.NotFound).ToProblem();

    public static IResult Validation(string code, string detail) =>
        Error.Validation(code, detail).ToProblem();

    public static IResult Conflict(string code, string detail) =>
        Error.Conflict(code, detail).ToProblem();

    public static IResult UnprocessableEntity(string code, string detail) =>
        TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status422UnprocessableEntity,
            title: "Unprocessable Entity",
            extensions: new Dictionary<string, object?> { ["code"] = code });

    public static IResult ServiceUnavailable(string code, string detail, string? title = null) =>
        TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status503ServiceUnavailable,
            title: title ?? "Service Unavailable",
            extensions: new Dictionary<string, object?> { ["code"] = code });

    public static IResult InternalServerError(
        string code = "Server.UnexpectedError",
        string detail = "Ocorreu um erro inesperado. Tente novamente mais tarde.") =>
        TypedResults.Problem(
            detail: detail,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Internal Server Error",
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
