using System;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Polly.Interception
{
    public class AsyncPolicyInterceptor : IAsyncInterceptor
    {
        private readonly AsyncPolicy _asyncPolicy;

        public AsyncPolicyInterceptor(AsyncPolicy asyncPolicy)
        {
            _asyncPolicy = asyncPolicy ?? throw new ArgumentNullException(nameof(asyncPolicy));
        }
        
        
        public void InterceptSynchronous(IInvocation invocation)
        {
            var capture = invocation.CaptureProceedInfo();

            invocation.ReturnValue = _asyncPolicy
                .ExecuteAsync(() =>
                {
                    capture.Invoke();
                    return Task.FromResult(invocation.ReturnValue);
                })
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        

        public void InterceptAsynchronous(IInvocation invocation)
        {
            var capture = invocation.CaptureProceedInfo();

            invocation.ReturnValue = _asyncPolicy
                .ExecuteAsync(() =>
                {
                    capture.Invoke();
                    return (Task)invocation.ReturnValue;
                });
        }

        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            var capture = invocation.CaptureProceedInfo();

            invocation.ReturnValue = _asyncPolicy
                .ExecuteAsync(() =>
                {
                    capture.Invoke();
                    return (Task<TResult>) invocation.ReturnValue;
                });
        }
    }
}