using Stock.Api.Security;

namespace Stock.Tests;

[TestFixture]
public class PasswordHasherTests
{
    private const string Password = "Secreta123";

    [Test]
    public void Hash_NoGuardaLaPasswordEnTextoPlano()
    {
        // AC-07 (RF-07)
        var (hash, salt) = PasswordHasher.Hash(Password);

        Assert.That(hash, Does.Not.Contain(Password));
        Assert.That(salt, Does.Not.Contain(Password));
        Assert.That(Convert.FromBase64String(hash), Has.Length.EqualTo(32));
    }

    [Test]
    public void Hash_DosUsuariosConLaMismaPassword_ObtienenSaltsYHashesDistintos()
    {
        // AC-08 (RF-08)
        var primero = PasswordHasher.Hash(Password);
        var segundo = PasswordHasher.Hash(Password);

        Assert.That(primero.Salt, Is.Not.EqualTo(segundo.Salt));
        Assert.That(primero.Hash, Is.Not.EqualTo(segundo.Hash));
    }

    [Test]
    public void Verify_ConLaPasswordCorrecta_DevuelveTrue()
    {
        var (hash, salt) = PasswordHasher.Hash(Password);

        Assert.That(PasswordHasher.Verify(Password, hash, salt), Is.True);
    }

    [Test]
    public void Verify_ConLaPasswordIncorrecta_DevuelveFalse()
    {
        var (hash, salt) = PasswordHasher.Hash(Password);

        Assert.That(PasswordHasher.Verify("otraPassword1", hash, salt), Is.False);
    }

    [Test]
    public void Verify_ConElSaltDeOtroUsuario_DevuelveFalse()
    {
        var (hash, _) = PasswordHasher.Hash(Password);
        var (_, otroSalt) = PasswordHasher.Hash(Password);

        Assert.That(PasswordHasher.Verify(Password, hash, otroSalt), Is.False);
    }
}
