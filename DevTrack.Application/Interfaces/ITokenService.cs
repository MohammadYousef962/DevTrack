using DevTrack.Domain.Entities;

namespace DevTrack.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
}