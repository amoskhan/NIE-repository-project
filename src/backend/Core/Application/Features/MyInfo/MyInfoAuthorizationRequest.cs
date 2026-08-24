namespace Application.Features.MyInfo;

public record MyInfoAuthorizationRequest(
    string AuthorizeUrl,
    string CodeVerifier,
    string Nonce,
    string DpopPrivateKey);
