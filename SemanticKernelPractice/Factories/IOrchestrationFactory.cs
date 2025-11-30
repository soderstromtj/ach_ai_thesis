using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPractice.Factories
{
    public interface IOrchestrationFactory<TResult>
    {
        Task<TResult> ExecuteCoreAsync(string input, CancellationToken cancellationToken = default);
    }
}
