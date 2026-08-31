using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ComplianceService.Infrastructure.Authorization;

public static class HospitalIdLocator
{
    public static bool TryGet(ActionExecutingContext context, out Guid hospitalId)
    {
        hospitalId = Guid.Empty;

        if (TryParseRouteOrQuery(context, out hospitalId))
        {
            return hospitalId != Guid.Empty;
        }

        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var argument) || argument is null)
            {
                continue;
            }

            if (IsHospitalIdName(parameter.Name) && argument is Guid guidArg && guidArg != Guid.Empty)
            {
                hospitalId = guidArg;
                return true;
            }

            var property = argument.GetType().GetProperty("HospitalId", BindingFlags.Public | BindingFlags.Instance);
            if (property is null)
            {
                continue;
            }

            if (property.PropertyType == typeof(Guid))
            {
                var value = (Guid)property.GetValue(argument)!;
                if (value != Guid.Empty)
                {
                    hospitalId = value;
                    return true;
                }
            }
            else if (property.PropertyType == typeof(Guid?))
            {
                var value = (Guid?)property.GetValue(argument);
                if (value is { } id && id != Guid.Empty)
                {
                    hospitalId = id;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryParseRouteOrQuery(ActionExecutingContext context, out Guid hospitalId)
    {
        hospitalId = Guid.Empty;

        foreach (var key in context.RouteData.Values.Keys)
        {
            if (!IsHospitalIdName(key))
            {
                continue;
            }

            if (Guid.TryParse(context.RouteData.Values[key]?.ToString(), out var routeId) && routeId != Guid.Empty)
            {
                hospitalId = routeId;
                return true;
            }
        }

        foreach (var query in context.HttpContext.Request.Query)
        {
            if (!IsHospitalIdName(query.Key))
            {
                continue;
            }

            if (Guid.TryParse(query.Value.ToString(), out var queryId) && queryId != Guid.Empty)
            {
                hospitalId = queryId;
                return true;
            }
        }

        return false;
    }

    public static bool IsHospitalIdName(string name)
    {
        var normalized = name.Replace("-", string.Empty).Replace("_", string.Empty);
        return string.Equals(normalized, "hospitalid", StringComparison.OrdinalIgnoreCase);
    }
}
