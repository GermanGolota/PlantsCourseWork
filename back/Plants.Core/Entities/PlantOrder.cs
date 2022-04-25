﻿using System;
using System.Collections.Generic;

#nullable disable

namespace Plants.Core.Entities
{
    public partial class PlantOrder
    {
        public int PostId { get; set; }
        public int CustomerId { get; set; }

        public virtual Person Customer { get; set; }
        public virtual PlantPost Post { get; set; }
        public virtual PlantShipment PlantShipment { get; set; }
    }
}