﻿using System.ComponentModel.DataAnnotations;

namespace Plants.Initializer;

[ConfigSection(Section)]
internal class WebRootConfig
{
    const string Section = "WebRoot";
    [Required]
    public string Path { get; set; } = null!;
}