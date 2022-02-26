using System;
using System.Linq;
using System.Linq.Expressions;
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
    /// <exception cref="InvalidEndpointException">If the endpoint has more than 1 parameter (which must be of type
    /// <see cref="Request"/>) or returns a disallowed return type.
    /// </exception>
    public static void AddEndpoints(this Router router, object obj)
    {
        var endpointsType = obj.GetType();
        foreach(var method in endpointsType.GetMethods())
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
            
            var requestParam = Expression.Parameter(typeof(Request));
            var endpointsObj = Expression.Constant(obj, endpointsType);

            Expression callExpression;
            if (parameters.Count == 1)
            {
                if (!parameters[0].IsAssignableFrom(typeof(Request)))
                {
                    throw new InvalidEndpointException(method,
                        $"Endpoints can only take a {nameof(Request)} as a parameter");
                }
                // Call with the request parameter
                callExpression = Expression.Call(endpointsObj, method, requestParam);
            }
            else
            {
                // Call without the request parameter
                callExpression = Expression.Call(endpointsObj, method);
            }

            var returnType = method.ReturnType;
            Expression endpointExpression;

            if (returnType.IsAssignableTo(typeof(Task<HttpResponse>)))
            {
                // Delegate type already matches Handler, no conversion required
                endpointExpression = callExpression;
            }
            else if(returnType.IsAssignableTo(typeof(HttpResponse)))
            {
                // Method returns a response synchronously, we need to wrap it in a task
                var fromResultDelegate = (Func<HttpResponse, Task<HttpResponse>>) Task.FromResult;
                endpointExpression = Expression.Call(fromResultDelegate.Method, callExpression);
            }
            else if (returnType == typeof(void))
            {
                endpointExpression = Expression.Block(
                    callExpression, // Call the endpoint
                    Expression.Constant(Task.FromResult(Empty)) // Return an empty response
                );
            }   else if(returnType == typeof(Task)) {
                // Method returns a task but with no result - we need to wrap this to return an empty HttpResponse
                var toResponseTaskDelegate = (Func<Task, Task<HttpResponse>>) ToEmptyResponse;
                endpointExpression = Expression.Call(toResponseTaskDelegate.Method, callExpression);
            } else {
                throw new InvalidEndpointException(method,
                    $"Endpoint return types must be either {nameof(HttpResponse)}, {nameof(Task<HttpResponse>)}," +
                    $" void or {nameof(Task)}");
            }

            var handler = Expression.Lambda<Handler>(endpointExpression, requestParam).Compile();
            router.Route(endpointAttribute.Method, endpointAttribute.Path, handler);
        }
    }

    /// <summary>
    /// Awaits <paramref name="t"/>, then returns <see cref="Empty"/>.
    /// Used to wrap endpoints returning <see cref="Task"/>
    /// </summary>
    /// <param name="t">Task to await</param>
    /// <returns><see cref="Empty"/></returns>
    private static async Task<HttpResponse> ToEmptyResponse(Task t)
    {
        await t;
        return Empty;
    }
    
    /// <summary>
    /// The response emitted by endpoints returning <see cref="Void"/> or <see cref="Task"/>
    /// </summary>
    private static HttpResponse Empty { get; } = Responses.Ok();
}
