namespace OzonOrdersWeb.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            //try
            //{
                await _next(context);
            //}
            //catch (Exception ex)
            //{

            //    context.Items["ErrorMessage"] = $"Произошла ошибка: {ex.Message}";

            //    context.Response.Redirect("/Home/Error");
            //}
        }
    }

    public static class ErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
