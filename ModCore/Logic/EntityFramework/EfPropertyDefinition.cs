﻿using System;
using System.Reflection;

namespace ModCore.Logic.EntityFramework
{
    public sealed class EfPropertyDefinition
    {
        public PropertyInfo Property { get; internal set; }
        public Attribute Source { get; internal set; }
    }
}