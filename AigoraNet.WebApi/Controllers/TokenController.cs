using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("Auth/[controller]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class TokenController : DefaultController
{

}
