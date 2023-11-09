namespace Sandstorm.Proxy.Helpers;

public static class ResponseHelper
{
    public static string NotFound()
    {
        return "{\"error\":{\"code\":404,\"error_ref\":14000,\"message\":\"The resource requested could not be found.\"}}";
    }
}
