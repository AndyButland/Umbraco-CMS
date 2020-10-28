﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Umbraco.Tests.Common.Builders.Interfaces;
using Umbraco.Web.Models.ContentEditing;

namespace Umbraco.Tests.Common.Builders
{
    public class ContentVariantSaveBuilder<TParent> : ChildBuilderBase<TParent, ContentVariantSave>,
        IWithNameBuilder,
        IWithCultureInfoBuilder
    {
        private List<ContentPropertyBasicBuilder<ContentVariantSaveBuilder<TParent>>> _propertyBuilders = new List<ContentPropertyBasicBuilder<ContentVariantSaveBuilder<TParent>>>();


        private string _name;
        private CultureInfo _cultureInfo;
        private bool? _save = null;
        private bool? _publish = null;

        public ContentVariantSaveBuilder(TParent parentBuilder) : base(parentBuilder)
        {
        }

        public ContentPropertyBasicBuilder<ContentVariantSaveBuilder<TParent>> AddProperty()
        {
            var builder = new ContentPropertyBasicBuilder<ContentVariantSaveBuilder<TParent>>(this);
            _propertyBuilders.Add(builder);
            return builder;
        }

        public override ContentVariantSave Build()
        {
            var name = _name ?? null;
            var culture = _cultureInfo?.Name ?? null;
            var save = _save ?? true;
            var publish = _publish ?? true;
            var properties = _propertyBuilders.Select(x => x.Build());

            return new ContentVariantSave()
            {
                Name = name,
                Culture = culture,
                Save = save,
                Publish = publish,
                Properties = properties
            };
        }

        string IWithNameBuilder.Name
        {
            get => _name;
            set => _name = value;
        }

        CultureInfo IWithCultureInfoBuilder.CultureInfo
        {
            get => _cultureInfo;
            set => _cultureInfo = value;
        }
    }
}
