﻿using System;
using System.Collections.Generic;

#nullable disable

namespace Plants.Core.Entities
{
    public partial class PlantsV
    {
        public int? Id { get; set; }
        public string PlantName { get; set; }
        public string Description { get; set; }
        public bool? Ismine { get; set; }
    }
}