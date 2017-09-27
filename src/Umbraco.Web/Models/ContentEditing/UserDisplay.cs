﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;

namespace Umbraco.Web.Models.ContentEditing
{
    /// <summary>
    /// Represents a user that is being edited
    /// </summary>
    [DataContract(Name = "user", Namespace = "")]
    [ReadOnly(true)]
    public class UserDisplay : UserBasic
    {
        public UserDisplay()
        {
            AvailableCultures = new Dictionary<string, string>();
            StartContentNodes = new List<EntityBasic>();
            StartMediaNodes = new List<EntityBasic>();
        }
        
        /// <summary>
        /// Gets the available cultures (i.e. to populate a drop down)
        /// The key is the culture stored in the database, the value is the Name
        /// </summary>
        [DataMember(Name = "availableCultures")]
        public IDictionary<string, string> AvailableCultures { get; set; }
        
        [DataMember(Name = "startContentNodes")]
        public IEnumerable<EntityBasic> StartContentNodes { get; set; }

        [DataMember(Name = "startMediaNodes")]
        public IEnumerable<EntityBasic> StartMediaNodes { get; set; }

        /// <summary>
        /// If the password is reset on save, this value will be populated
        /// </summary>
        [DataMember(Name = "resetPasswordValue")]
        [ReadOnly(true)]
        public string ResetPasswordValue { get; set; }
        
    }
}