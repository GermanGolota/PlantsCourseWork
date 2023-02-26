﻿using Microsoft.Extensions.Options;

namespace Plants.Initializer;

internal class HostingContext : IHostingContext
{
    private readonly WebRootConfig _options;

    public HostingContext(IOptions<WebRootConfig> options)
    {
        _options = options.Value;
    }

    public string WebRootPath => _options.Path;

    public string WebRootUrlPath => $"/{Path.GetFileName(_options.Path)}";
}