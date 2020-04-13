using Castle.DynamicProxy;

namespace Polly.Interception
{
    public interface IReadOnlyPolicyRegistry
    {
        bool TryGetAsyncPolicy(IInvocation invocation, out IAsyncPolicy policy);
        bool TryGetSyncPolicy(IInvocation invocation, out ISyncPolicy policy);
    }
}