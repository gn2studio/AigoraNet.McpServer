using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

[ApiController]
[Route("Public/[controller]")]
[ApiConventionType(typeof(DefaultApiConventions))]
public class BoardController : DefaultController
{

}
