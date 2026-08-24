namespace Application.Features.MyInfo;

public interface IMyInfoService
{
    Task<MyInfoAuthorizationRequest> CreateAuthorizationRequestAsync(
        string state,
        CancellationToken cancellationToken = default);
    Task<MyInfoPersonData> GetPersonDataAsync(
        string authCode,
        string codeVerifier,
        string nonce,
        string dpopPrivateKey,
        CancellationToken cancellationToken = default);
    bool IsConfigured { get; }
}
