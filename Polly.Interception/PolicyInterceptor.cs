using System;
using System.Linq;
using System.Threading.Tasks;
using Castle.DynamicProxy;
namespace Polly.Interception
{
    public class PolicyInterceptor : IAsyncInterceptor
    {
        private readonly IReadOnlyPolicyRegistry _policyRegistry;

        public PolicyInterceptor(IReadOnlyPolicyRegistry policyRegistry)
        {
            _policyRegistry = policyRegistry ?? throw new ArgumentNullException(nameof(policyRegistry));
        }


        public void InterceptSynchronous(IInvocation invocation)
        {
            if(!_policyRegistry.TryGetSyncPolicy(invocation, out var syncPolicy))
            {
                invocation.Proceed();
                return;
            }

            var capture = invocation.CaptureProceedInfo();

            invocation.ReturnValue = syncPolicy
                .Execute(ctx =>
                    {
                        capture.Invoke();
                        return invocation.ReturnValue;
                    },
                    new Context(CreateOperationKey(invocation)));
        }
        

        public void InterceptAsynchronous(IInvocation invocation)
        {
            if(!_policyRegistry.TryGetAsyncPolicy(invocation, out var asyncPolicy))
            {
                invocation.Proceed();
                return;
            }
            
            var capture = invocation.CaptureProceedInfo();

            invocation.ReturnValue = asyncPolicy
                .ExecuteAsync(ctx =>
                    {
                        capture.Invoke();
                        return (Task) invocation.ReturnValue;
                    },
                    new Context(CreateOperationKey(invocation)));
        }

        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            if(!_policyRegistry.TryGetAsyncPolicy(invocation, out var asyncPolicy))
            {
                invocation.Proceed();
                return;
            }

            var capture = invocation.CaptureProceedInfo();

            invocation.ReturnValue = asyncPolicy
                .ExecuteAsync(ctx =>
                    {
                        capture.Invoke();
                        return (Task<TResult>) invocation.ReturnValue;
                    },
                    new Context(CreateOperationKey(invocation)));
        }
        

        private static string CreateOperationKey(IInvocation invocation)
        {
            var type = invocation.TargetType;
            var methodInfo = invocation.MethodInvocationTarget;
            var fullMethodName = $"{type.Namespace}.{type.Name}.{methodInfo.Name}";
            var arguments = string.Join("_", invocation.Arguments.Select(a => a.ToString()));
            var operationKey = $"{methodInfo.ReturnType} | {fullMethodName} | {arguments}";
            return operationKey;
        }
    }
}