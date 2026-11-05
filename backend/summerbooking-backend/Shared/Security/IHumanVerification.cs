namespace Shared.Security;

/// <summary>Verifica anti-robot dei form pubblici: il token prodotto dal widget del frontend.</summary>
public interface IHumanVerification
{
	Task<bool> VerifyAsync(string? token, string? clientIp, CancellationToken ct = default);
}

/// <summary>Sfida anti-robot da risolvere nel browser prima di inviare un form pubblico.</summary>
public interface IHumanChallengeIssuer
{
	HumanChallenge Create();
}

/// <summary>
/// Sfida di prova di lavoro: il browser cerca il numero (da 0 a <paramref name="MaxNumber"/>) per cui
/// SHA-256(<paramref name="Salt"/> + numero) vale <paramref name="Challenge"/>, e lo rimanda insieme alla sfida firmata.
/// Stesso formato del widget open source ALTCHA.
/// </summary>
public sealed record HumanChallenge(string Algorithm, string Challenge, long MaxNumber, string Salt, string Signature);
