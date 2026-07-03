using GestaoDePedidos.Common.Responses;
using GestaoDePedidos.Common.Security;
using GestaoDePedidos.Common.Swagger;
using GestaoDePedidos.Data;
using GestaoDePedidos.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var (statusCode, body) = ApiErrorResponseFactory.FromModelState(context.ModelState);
            return new ObjectResult(body) { StatusCode = statusCode };
        };
    });
builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.InferSecuritySchemes();
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gestão de Pedidos API",
        Version = "v1",
        Description = """
            API de gestão de pedidos: cadastro de clientes e produtos, e criação/acompanhamento de pedidos com baixa de estoque.

            Toda resposta de erro segue o mesmo formato (`ApiErrorResponse`): `status` (o código HTTP), `title` (categoria do erro),
            `detail` (mensagem explicando a causa) e `errors` (lista de erros por campo, presente apenas quando a causa é validação
            de campos da requisição; `null` nos demais casos, como regras de negócio ou autenticação/autorização).
            """
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    options.SchemaFilter<SchemaExamplesFilter>();
    options.OperationFilter<AuthorizeOperationFilter>();
    options.OperationFilter<ErrorResponseExamplesFilter>();
});

var jwtSettings = JwtSettings.FromConfiguration(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };

        options.Events = new JwtBearerEvents
        {
            // Sem HandleResponse() o handler padrão só define o status/WWW-Authenticate e não escreve corpo.
            OnChallenge = context =>
            {
                context.HandleResponse();

                var body = new ApiErrorResponse
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Não autenticado",
                    Detail = "Token de acesso ausente, inválido ou expirado."
                };

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return context.Response.WriteAsJsonAsync(body);
            },
            OnForbidden = context =>
            {
                var body = new ApiErrorResponse
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Acesso negado",
                    Detail = "Você não tem permissão para acessar este recurso."
                };

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return context.Response.WriteAsJsonAsync(body);
            }
        };
    });

builder.Services.AddAuthorization();

new RegisterServices(builder.Services);


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gestão de Pedidos API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await DbSeeder.SeedAdminAsync(app.Services);

app.Run();
