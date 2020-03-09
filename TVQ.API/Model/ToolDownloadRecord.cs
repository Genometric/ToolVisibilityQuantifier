﻿using System;

namespace Genometric.TVQ.API.Model
{
    public class ToolDownloadRecord : BaseModel
    {
        public int ToolID { set; get; }

        public int Count { set; get; }

        public DateTime Date { set; get; }

        public virtual Tool Tool { set; get; }

        public ToolDownloadRecord() { }
    }
}
