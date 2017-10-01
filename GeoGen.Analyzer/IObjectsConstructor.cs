﻿using GeoGen.Analyzer.Geometry;
using GeoGen.Core.Configurations;

namespace GeoGen.Analyzer
{
    /// <summary>
    /// Represents a constructor of geometrical objects. It's 
    /// the key connector between configuration objects and geometrical
    /// objects.
    /// </summary>
    internal interface IObjectsConstructor
    {
        /// <summary>
        /// Constructs a geometrical object from a given configuration object.
        /// </summary>
        /// <param name="configurationObject">The configuration object.</param>
        /// <returns>The geometrical object.</returns>
        GeometricalObject Construct(ConfigurationObject configurationObject);
    }
}