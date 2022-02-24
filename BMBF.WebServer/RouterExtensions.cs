using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BMBF.WebServer.Attributes;
using Hydra;

namespace BMBF.WebServer;


public static class RouterExtensions
{
    /// <summary>
    /// Adds methods annotated with <see cref="EndpointAttribute"/> subclasses as routes in <see cref="Router" />
    ///
    /// Methods annotated with <see cref="EndpointAttribute"/> must have at most one argument
    /// (of type <see cref="Request"/>). The return types permitted are
    /// <see cref="HttpResponse"/>, <see cref="Task{HttpResponse}"/>, or <see cref="Void"/>.
    /// If <see cref="Void"/> is returned, then it will be converted into a 200 OK response, with an empty body.
    /// </summary>
    /// <param name="router">Router to add the endpoints to</param>
    /// <param name="obj">Object instance to add the endpoints from</param>
    /// <exception cref="InvalidEndpointException"></exception>
    public static void AddEndpoints(this Router router, object obj)
    {
        foreach(var method in obj.GetType().GetMethods())
        {
            object[] attributes = method.GetCustomAttributes(true);
            var endpointAttributes = attributes
                .OfType<EndpointAttribute>()
                .ToArray();

            // Skip - method is not declared as an endpoint
            if (endpointAttributes.Length == 0)
            {
                continue;
            }

            // We cannot have 1 method declared as multiple endpoints!
            if (endpointAttributes.Length > 1)
            {
                throw new InvalidEndpointException(method,
                    $"{endpointAttributes.Length} endpoint attributes detected - only 0 or 1 are allowed");
            }

            var endpointAttribute = endpointAttributes[0];
            
            var parameters = method.GetParameters().Select(p => p.ParameterType).ToList();
            if (parameters.Count > 1)
            {
                throw new InvalidEndpointException(method, "Endpoint can have a maximum of 1 parameter");
            }

            bool needPassRequest = false;
            if (parameters.Count == 1)
            {
                needPassRequest = true;
                if (!parameters[0].IsAssignableFrom(typeof(Request)))
                {
                    throw new InvalidEndpointException(method,
                        $"Endpoints can only take a {nameof(Request)} as a parameter");
                }
            }

            var returnType = method.ReturnType;

            bool returnsResponse;
            bool returnsTask;
            if (returnType.IsAssignableTo(typeof(HttpResponse)))
            {
                returnsResponse = true;
                returnsTask = false;
            }
            else if(returnType.IsAssignableTo(typeof(Task<HttpResponse>)))
            {
                returnsResponse = true;
                returnsTask = true;
            }
            else if (returnType == typeof(void))
            {
                returnsResponse = false;
                returnsTask = false;
            }   else if(returnType.IsAssignableTo(typeof(Task))) {
                returnsResponse = false;
                returnsTask = true;
            } else {
                throw new InvalidEndpointException(method,
                    $"Endpoint return types must be either {nameof(HttpResponse)}, {nameof(Task<HttpResponse>)}," +
                    $" void or {nameof(Task)}");
            }
            
            router.Route(endpointAttribute.Method, endpointAttribute.Path, async req =>
            {
                object? result;
                try
                {
                    // Invoke the endpoint method reflectively
                    // TODO: Perhaps use Expression<T> or convert to delegate in an attempt to make this faster
                    result = method.Invoke(
                        obj,
                        needPassRequest
                            ? new object?[] { req } // Pass the request if required
                            : Array.Empty<object?>());
                }
                catch (TargetInvocationException ex)
                {
                    // Prefer throwing the underlying exception from the endpoint
                    if (ex.InnerException != null)
                    {
                        throw ex.InnerException;
                    }
                    throw;
                }
                

                // If this endpoint returns void, return OK now
                if (!returnsResponse && !returnsTask)
                {
                    return Responses.Ok();
                }
                
                if (result == null)
                {
                    throw new NullReferenceException($"Null return from endpoint {endpointAttribute.Path}");
                }

                // If the endpoint returns a task but with no response, we will await it, then return OK
                if (!returnsResponse)
                {
                    await (Task) result;
                    return Responses.Ok();
                }

                // Await the endpoint if necessary, otherwise return the result
                if (returnsTask)
                {
                    return await (Task<HttpResponse>) result;
                }
                return (HttpResponse) result;
            });
        }
    }
}
