﻿using System;

using MediaBrowser.Model.Dto;

namespace MediaBrowser.Plugins.DVBViewer.Services.Entities
{
    public class StreamingDetails
    {
        public String Id { get; set; }
        public String StreamIdentifier { get; set; }
        public MediaSourceInfo SourceInfo { get; set; }
    }
}
