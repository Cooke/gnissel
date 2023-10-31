namespace Cooke.Gnissel.Utils;

public static class CancellationTokenExtensions
{
    public static CancellationToken Combine(
        this CancellationToken cancellationToken1,
        CancellationToken cancellationToken2
    )
    {
        if (cancellationToken1 != default && cancellationToken2 != default)
        {
            return CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken1, cancellationToken2)
                .Token;
        }

        return cancellationToken1 != default ? cancellationToken1 : cancellationToken2;
    }
}
