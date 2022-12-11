namespace OmniSharp.Models;

public interface IAggregateResponse<T> where T : IAggregateResponse<T>
{
    T Merge(T response);
}

public interface IAggregateResponse
{
    IAggregateResponse Merge(IAggregateResponse response);
}
