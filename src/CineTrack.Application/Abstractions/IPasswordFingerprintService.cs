namespace CineTrack.Application.Abstractions;

public interface IPasswordFingerprintService
{
    string CreateFingerprint(string password);
}
