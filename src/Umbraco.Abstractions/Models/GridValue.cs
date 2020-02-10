using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Umbraco.Core.Models
{
    // TODO: Make a property value converter for this!

    /// <summary>
    /// A model representing the value saved for the grid
    /// </summary>
    public class GridValue
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "sections")]
        public IEnumerable<GridSection> Sections { get; set; }

        public class GridSection
        {
            [DataMember(Name = "grid")]
            public string Grid { get; set; } // TODO: what is this?

            [DataMember(Name = "rows")]
            public IEnumerable<GridRow> Rows { get; set; }
        }

        public class GridRow
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "id")]
            public Guid Id { get; set; }

            [DataMember(Name = "areas")]
            public IEnumerable<GridArea> Areas { get; set; }

            [DataMember(Name = "styles")]
            public JToken Styles { get; set; }

            [DataMember(Name = "config")]
            public JToken Config { get; set; }
        }

        public class GridArea
        {
            [DataMember(Name = "grid")]
            public string Grid { get; set; } // TODO: what is this?

            [DataMember(Name = "controls")]
            public IEnumerable<GridControl> Controls { get; set; }

            [DataMember(Name = "styles")]
            public JToken Styles { get; set; }

            [DataMember(Name = "config")]
            public JToken Config { get; set; }
        }

        public class GridControl
        {
            [DataMember(Name = "value")]
            public JToken Value { get; set; }

            [DataMember(Name = "editor")]
            public GridEditor Editor { get; set; }

            [DataMember(Name = "styles")]
            public JToken Styles { get; set; }

            [DataMember(Name = "config")]
            public JToken Config { get; set; }
        }

        public class GridEditor
        {
            [DataMember(Name = "alias")]
            public string Alias { get; set; }

            [DataMember(Name = "view")]
            public string View { get; set; }
        }
    }
}
