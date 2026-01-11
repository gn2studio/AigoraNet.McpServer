using GN2.Common.Library.Abstracts;
using GN2.Core;
using Microsoft.AspNetCore.Mvc;

namespace AigoraNet.WebApi.Controllers;

public class DefaultController : ControllerBase
{
    protected IObjectLinker _linker;

    protected IActionBridge _bridge;

    public DefaultController(IActionBridge bridge, IObjectLinker linker)
    {
        _bridge = bridge;
        _linker = linker;
    }

    public  IActionResult ApiResult<T>(ReturnValues<T> result)
    {
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result.Message);
        }
    }

    public IActionResult ApiResult(ReturnValue result)
    {
        if (result.Success)
        {
            return Ok(result);
        }
        else
        {
            return BadRequest(result.Message);
        }
    }
}
