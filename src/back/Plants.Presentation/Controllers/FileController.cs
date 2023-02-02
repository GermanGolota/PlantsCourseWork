﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Plants.Application.Contracts;

namespace Plants.Presentation;

[ApiController]
[Route("file")]
[ApiVersion("1")]
[ApiExplorerSettings(GroupName = "v1")]
public class FileController : ControllerBase
{
    private readonly IFileService _file;

    public FileController(IFileService file)
    {
        _file = file;
    }

    [HttpGet("plant/{id}")]
    public async Task<FileResult> Load([FromRoute] long id)
    {
        var bytes = await _file.LoadPlantImage(id);
        return File(bytes, "application/octet-stream");
    }

    [HttpGet("instruction/{id}")]
    public async Task<FileResult> LoadInstruction([FromRoute] long id)
    {
        var bytes = await _file.LoadInstructionCoverImage(id);
        return File(bytes, "application/octet-stream");
    }
}

[ApiController]
[Route("v2/file")]
[ApiVersion("2")]
[ApiExplorerSettings(GroupName = "v2")]
public class FileControllerV2 : ControllerBase
{
    private readonly IFileProvider _provider;
    private readonly IProjectionQueryService<PlantInstruction> _instructionQuery;
    private readonly IProjectionQueryService<PlantInfo> _infoQuery;

    public FileControllerV2(IFileProvider provider,
        IProjectionQueryService<PlantInstruction> instructionQuery,
        IProjectionQueryService<PlantInfo> infoQuery)
    {
        _provider = provider;
        _instructionQuery = instructionQuery;
        _infoQuery = infoQuery;
    }

    [HttpGet("plant/{id}")]
    public async Task<ActionResult> Load([FromRoute] long id, CancellationToken token)
    {
        var plantInfo = await _infoQuery.GetByIdAsync(PlantInfo.InfoId, token);
        var path = plantInfo.PlantImagePaths[id];
        var info = _provider.GetFileInfo(path);
        if (info.Exists)
        {
            var streamFunc = info.CreateReadStream;
            var bytes = await streamFunc.ReadAllBytesAsync();
            return File(bytes, "application/octet-stream");
        }
        else
        {
            return NotFound();
        }
    }

    [HttpGet("instruction/{id}")]
    public async Task<ActionResult> LoadInstruction([FromRoute] Guid id, CancellationToken token)
    {
        var instruction = await _instructionQuery.GetByIdAsync(id, token);
        var path = instruction.CoverUrl;
        var info = _provider.GetFileInfo(path);
        if (info.Exists)
        {
            var streamFunc = info.CreateReadStream;
            var bytes = await streamFunc.ReadAllBytesAsync();
            return File(bytes, "application/octet-stream");
        }
        else
        {
            return NotFound();
        }
    }
}
